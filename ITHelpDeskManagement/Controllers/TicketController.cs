
using ITHelpDeskManagement.Data;
using ITHelpDeskManagement.Hubs;
using ITHelpDeskManagement.Models;
using ITHelpDeskManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ITHelpDeskManagement.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TicketHub> _hubContext;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<TicketHub> hubContext,
            ILogger<TicketController> logger)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _logger = logger;
        }

        // Display all tickets
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Index(
            string? searchString,
            string? status,
            string? priority,
            int page = 1)
        {
            int pageSize = 10;

            var currentUserId = _userManager.GetUserId(User);

            var tickets = _context.Tickets.AsQueryable();

            // Employees can see only tickets they created.
            // Admins can see all tickets.
            if (!User.IsInRole("Admin"))
            {
                tickets = tickets.Where(t =>
                    t.CreatedByUserId == currentUserId);
            }

            // Search by title or description
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                tickets = tickets.Where(t =>
                    t.Title.Contains(searchString) ||
                    t.Description.Contains(searchString));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                tickets = tickets.Where(t => t.Status == status);
            }

            // Filter by priority
            if (!string.IsNullOrWhiteSpace(priority))
            {
                tickets = tickets.Where(t => t.Priority == priority);
            }

            // Count tickets after applying filters
            int totalTickets = await tickets.CountAsync();

            // Pagination
            var ticketList = await tickets
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(
                totalTickets / (double)pageSize);

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            return View(ticketList);
        }

        // Display the ticket creation form
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Create()
        {
            return View();
        }

        // Save the submitted ticket
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket)
        {
            _logger.LogInformation("CREATE TICKET POST ACTION STARTED");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var item in error.Value.Errors)
                    {
                        _logger.LogWarning(
                            "Validation error in {Field}: {Error}",
                            error.Key,
                            item.ErrorMessage
                        );
                    }
                }

                return View(ticket);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            ticket.CreatedByUserId = currentUser.Id;
            ticket.CreatedByEmail = currentUser.Email;

            // Save ticket.
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ticket created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[] { "Open", "In Progress", "Resolved" };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid ticket status.");
            }

            ticket.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,User,SupportStaff")]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.AssignedToUser)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Get logged-in user's ID.
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");

            bool isCreator = ticket.CreatedByUserId == currentUserId;

            bool isAssignedStaff =
                User.IsInRole("SupportStaff") &&
                ticket.AssignedToUserId == currentUserId;

            // Check whether the user can access this ticket.
            if (!isAdmin && !isCreator && !isAssignedStaff)
            {
                return Forbid();
            }

            // Find comments hidden by the currently logged-in user.
            var hiddenCommentIds = await _context.CommentHiddenForUsers
                .Where(h => h.UserId == currentUserId)
                .Select(h => h.CommentId)
                .ToListAsync();

            // Hide only those comments from this user's view.
            ticket.Comments = ticket.Comments
                .Where(c => !hiddenCommentIds.Contains(c.CommentId))
                .ToList();

            // Mark this user's unread notifications for this ticket as read.
            var unreadNotifications = await _context.Notifications
                .Where(n =>
                    n.UserId == currentUserId &&
                    n.TicketId == id &&
                    !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            if (unreadNotifications.Any())
            {
                await _context.SaveChangesAsync();
            }

            // Load ticket status history.
            var history = await _context.TicketStatusHistories
                .Where(h => h.TicketId == id)
                .OrderByDescending(h => h.UpdatedAt)
                .ToListAsync();

            ViewBag.StatusHistory = history;

            return View(ticket);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Employees can edit only their own Open tickets.
            if (!User.IsInRole("Admin"))
            {
                var currentUserId = _userManager.GetUserId(User);

                if (ticket.CreatedByUserId != currentUserId)
                {
                    return Forbid();
                }

                if (ticket.Status != "Open")
                {
                    TempData["Error"] = "You can edit only Open tickets.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            return View(ticket);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("TicketId,Title,Description,Priority")] Ticket input)
        {
            if (id != input.TicketId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Employees can edit only their own Open tickets.
            if (!User.IsInRole("Admin"))
            {
                var currentUserId = _userManager.GetUserId(User);

                if (ticket.CreatedByUserId != currentUserId)
                {
                    return Forbid();
                }

                if (ticket.Status != "Open")
                {
                    return Forbid();
                }
            }

            ticket.Title = input.Title;
            ticket.Description = input.Description;
            ticket.Priority = input.Priority;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Ticket updated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var currentUserId = _userManager.GetUserId(User);

                if (ticket.CreatedByUserId != currentUserId)
                {
                    return Forbid();
                }

                if (ticket.Status != "Open")
                {
                    TempData["Error"] = "You can delete only Open tickets.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            return View(ticket);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var currentUserId = _userManager.GetUserId(User);

                if (ticket.CreatedByUserId != currentUserId)
                {
                    return Forbid();
                }

                if (ticket.Status != "Open")
                {
                    return Forbid();
                }
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ticket deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SupportStaff")]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (User.IsInRole("SupportStaff"))
            {
                var currentUserId = _userManager.GetUserId(User);

                if (ticket.AssignedToUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SupportStaff")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status,
            string? remarks)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // SupportStaff can update only assigned tickets.
            if (User.IsInRole("SupportStaff"))
            {
                var currentUserId = _userManager.GetUserId(User);

                if (ticket.AssignedToUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            var allowedStatuses = new[]
            {
                "Open",
                "In Progress",
                "Resolved"
            };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid ticket status.");
            }

            if (remarks != null && remarks.Length > 500)
            {
                return BadRequest("Remarks cannot exceed 500 characters.");
            }

            string oldStatus = ticket.Status;

            ticket.Status = status;

            if (oldStatus != status)
            {
                // 1. Save status history
                var history = new TicketStatusHistory
                {
                    TicketId = ticket.TicketId,
                    OldStatus = oldStatus,
                    NewStatus = status,
                    Remarks = remarks,
                    UpdatedBy = User.Identity?.Name ?? "Unknown",
                    UpdatedAt = DateTime.Now
                };

                _context.TicketStatusHistories.Add(history);

                // 2. Create notification for ticket creator
                if (!string.IsNullOrEmpty(ticket.CreatedByUserId))
                {
                    var notification = new Notification
                    {
                        UserId = ticket.CreatedByUserId,
                        TicketId = ticket.TicketId,
                        Message = $"Your ticket '{ticket.Title}' status has changed from {oldStatus} to {status}.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                }
            }

            // Save history, notification and ticket status.
            await _context.SaveChangesAsync();

            // Notify the Employee who created this ticket.
            if (oldStatus != status &&
                !string.IsNullOrEmpty(ticket.CreatedByUserId))
            {
                await _hubContext.Clients
                    .User(ticket.CreatedByUserId)
                    .SendAsync(
                        "TicketStatusUpdated",
                        ticket.TicketId,
                        ticket.Status
                    );
            }

            // Notify the assigned SupportStaff member.
            if (oldStatus != status &&
                !string.IsNullOrEmpty(ticket.AssignedToUserId) &&
                ticket.AssignedToUserId != ticket.CreatedByUserId)
            {
                await _hubContext.Clients
                    .User(ticket.AssignedToUserId)
                    .SendAsync(
                        "TicketStatusUpdated",
                        ticket.TicketId,
                        ticket.Status
                    );
            }

            TempData["Success"] = "Ticket status updated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> History(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            var history = await _context.TicketStatusHistories
                .Where(h => h.TicketId == id)
                .OrderByDescending(h => h.UpdatedAt)
                .ToListAsync();

            ViewBag.Ticket = ticket;

            return View(history);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Assign(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            var staff = await _userManager.GetUsersInRoleAsync("SupportStaff");

            ViewBag.Staff = staff;

            return View(ticket);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, string? assignedTo)
        {
            // 1. Find the ticket
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // 2. Convert an empty selection into NULL (unassigned).
            string? newAssignedTo = string.IsNullOrWhiteSpace(assignedTo)
                ? null
                : assignedTo;

            // 3. Validate the selected Support Staff member.
            if (newAssignedTo != null)
            {
                var staff = await _userManager.GetUsersInRoleAsync("SupportStaff");

                if (!staff.Any(u => u.Id == newAssignedTo))
                {
                    TempData["Error"] = "Invalid Support Staff member.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // 4. Remember the previous assigned user.
            string? previousAssignedTo = ticket.AssignedToUserId;

            // 5. Save the assignment in the new primary column.
            ticket.AssignedToUserId = newAssignedTo;

            // Keep the old property synchronized for existing views/code.
            ticket.AssignedTo = newAssignedTo;

            // 6. Save ticket assignment.
            await _context.SaveChangesAsync();

            // 7. Create notification only when assignment changes to a user.
            if (newAssignedTo != null &&
                newAssignedTo != previousAssignedTo)
            {
                var notification = new Notification
                {
                    UserId = newAssignedTo,
                    TicketId = ticket.TicketId,
                    Message = $"A new ticket has been assigned to you: {ticket.Title}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);

                // Save notification in SQL Server.
                await _context.SaveChangesAsync();

                // 8. Send notification immediately using SignalR.
                await _hubContext.Clients
                    .User(newAssignedTo)
                    .SendAsync(
                        "ReceiveNotification",
                        notification.NotificationId,
                        notification.Message,
                        notification.TicketId,
                        notification.CreatedAt
                    );
            }

            TempData["Success"] = "Ticket assignment updated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTickets()
        {
            var tickets = await _context.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var users = await _userManager.Users.ToListAsync();

            var userEmails = users.ToDictionary(
                u => u.Id,
                u => u.Email ?? u.UserName ?? "Staff"
            );

            ViewBag.UserEmails = userEmails;

            return View("AssignTickets", tickets);
        }

        [Authorize(Roles = "SupportStaff")]
        public async Task<IActionResult> MyAssignedTickets()
        {
            var userId = _userManager.GetUserId(User);

            var tickets = await _context.Tickets
                .Where(t => t.AssignedToUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(
            int ticketId,
            string commentText)
        {
            // 1. Find ticket
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                return NotFound();
            }

            // 2. Get logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            // 3. Check authorization
            bool isAdmin = User.IsInRole("Admin");

            bool isCreator =
                ticket.CreatedByUserId == currentUser.Id;

            bool isAssignedStaff =
                User.IsInRole("SupportStaff") &&
                ticket.AssignedToUserId == currentUser.Id;

            if (!isAdmin && !isCreator && !isAssignedStaff)
            {
                return Forbid();
            }

            // 4. Validate comment
            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["Error"] = "Comment cannot be empty.";

                return RedirectToAction(nameof(Details),
                    new { id = ticketId });
            }

            if (commentText.Length > 2000)
            {
                TempData["Error"] =
                    "Comment cannot exceed 2000 characters.";

                return RedirectToAction(nameof(Details),
                    new { id = ticketId });
            }

            // 5. Create comment
            var comment = new TicketComment
            {
                TicketId = ticketId,
                UserId = currentUser.Id,
                CommentText = commentText.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.ticketcomment.Add(comment);

            // 6. Prepare notification recipients
            var notifications = new List<Notification>();

            // CASE 1: SupportStaff comments
            // Notify all Admin users.
            if (User.IsInRole("SupportStaff"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                foreach (var admin in admins)
                {
                    // Don't notify the same user.
                    if (admin.Id == currentUser.Id)
                        continue;

                    notifications.Add(new Notification
                    {
                        UserId = admin.Id,
                        TicketId = ticket.TicketId,
                        Message = $"{currentUser.Email} commented on ticket '{ticket.Title}'.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // CASE 2: Admin comments
            // Notify the person who created the ticket.
            else if (isAdmin &&
                     !string.IsNullOrEmpty(ticket.CreatedByUserId) &&
                     ticket.CreatedByUserId != currentUser.Id)
            {
                notifications.Add(new Notification
                {
                    UserId = ticket.CreatedByUserId,
                    TicketId = ticket.TicketId,
                    Message = $"Admin replied to your ticket '{ticket.Title}'.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            // 7. Save comments and notifications
            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
            }

            await _context.SaveChangesAsync();

            // 8. Send real-time SignalR notifications
            foreach (var notification in notifications)
            {
                await _hubContext.Clients
                    .User(notification.UserId)
                    .SendAsync(
                        "ReceiveNotification",
                        notification.NotificationId,
                        notification.Message,
                        notification.TicketId,
                        notification.CreatedAt
                    );
            }

            TempData["Success"] = "Comment added successfully.";

            return RedirectToAction(nameof(Details),
                new { id = ticketId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,User,SupportStaff")]
        public async Task<IActionResult> Comments()
        {
            // 1. Get the logged-in user's ID.
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Unauthorized();
            }

            // 2. Check the user's role.
            bool isAdmin = User.IsInRole("Admin");
            bool isSupportStaff = User.IsInRole("SupportStaff");

            // 3. Find comments hidden by this user.
            var hiddenCommentIds = await _context.CommentHiddenForUsers
                .Where(h => h.UserId == currentUserId)
                .Select(h => h.CommentId)
                .ToListAsync();

            // 4. Get comments according to the user's permissions.
            var comments = await _context.ticketcomment
                .Include(c => c.Ticket)
                .Include(c => c.User)
                .Where(c =>
                    !hiddenCommentIds.Contains(c.CommentId) &&
                    (
                        // Admin can see every comment.
                        isAdmin ||

                        // Ticket creator can see comments on their tickets.
                        c.Ticket!.CreatedByUserId == currentUserId ||

                        // SupportStaff can see comments on assigned tickets.
                        (isSupportStaff &&
                         c.Ticket!.AssignedToUserId == currentUserId)
                    ))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // 5. Convert database comments into ViewModel objects.
            var commentList = new List<AddCommentHistoryViewModel>();

            foreach (var comment in comments)
            {
                var roles = await _userManager.GetRolesAsync(comment.User!);

                string authorRole = roles.Contains("Admin")
                    ? "Admin"
                    : roles.Contains("SupportStaff")
                        ? "SupportStaff"
                        : "User";

                commentList.Add(new AddCommentHistoryViewModel
                {
                    CommentId = comment.CommentId,
                    TicketId = comment.TicketId,
                    TicketTitle = comment.Ticket?.Title ?? "Unknown Ticket",
                    AuthorEmail = comment.User?.Email ?? "Unknown",
                    AuthorRole = authorRole,
                    TicketCreatorEmail =
                        comment.Ticket?.CreatedByEmail ?? "Unknown",
                    CommentText = comment.CommentText,
                    CommentDate = comment.CreatedAt
                });
            }

            // 6. Send the comments to the Comments view.
            return View(commentList);
        }
    }
}

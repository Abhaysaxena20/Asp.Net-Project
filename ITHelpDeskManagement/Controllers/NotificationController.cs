
using ITHelpDeskManagement.Data;
using ITHelpDeskManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITHelpDeskManagement.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Get notifications of the logged-in user
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.TicketId,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Json(notifications);
        }

        // Get unread notification count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(count);
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == id &&
                    n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}

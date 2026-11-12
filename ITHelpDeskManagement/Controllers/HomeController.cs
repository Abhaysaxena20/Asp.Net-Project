
using Microsoft.AspNetCore.Authorization;
using ITHelpDeskManagement.Data;
using ITHelpDeskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITHelpDeskManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Start with all tickets
            var tickets = _context.Tickets.AsQueryable();

            // If the logged-in user is not Admin,
            // show only tickets created by that user.
            if (!User.IsInRole("Admin"))
            {
                var currentUserId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                tickets = tickets.Where(t =>
                    t.CreatedByUserId == currentUserId);
            }

            // Calculate dashboard statistics
            var dashboard = new DashboardViewModel
            {
                TotalTickets = await tickets.CountAsync(),

                OpenTickets = await tickets
                    .CountAsync(t => t.Status == "Open"),

                InProgressTickets = await tickets
                    .CountAsync(t => t.Status == "In Progress"),

                ResolvedTickets = await tickets
                    .CountAsync(t => t.Status == "Resolved"),

                RecentTickets = await tickets
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}

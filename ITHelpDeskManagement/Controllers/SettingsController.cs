
using ITHelpDeskManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpDeskManagement.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Settings/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = user.FullName;
            ViewBag.UserName = user.UserName;
            ViewBag.Email = user.Email;
            ViewBag.PhoneNumber = user.PhoneNumber;

            ViewBag.Role = User.IsInRole("Admin")
                ? "Administrator"
                : User.IsInRole("SupportStaff")
                    ? "Support Staff"
                    : "User";

            return View();
        }
        // GET: Settings/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Settings/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword
            );

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Your password has been changed successfully.";

                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}

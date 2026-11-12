using ITHelpDeskManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace ITHelpDeskManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                UserName = model.Email.Trim(),
                Email = model.Email.Trim()
            };

            var result = await _userManager.CreateAsync(
                user, model.Password);

            if (result.Succeeded)
            {
                // Assign the User role to every newly registered account.
                var roleResult = await _userManager.AddToRoleAsync(user, "User");

                if (!roleResult.Succeeded)
                {
                    // Remove the newly created account if role assignment fails.
                    await _userManager.DeleteAsync(user);

                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Ticket");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }




        [HttpGet]
        public IActionResult Login(
            string? returnUrl = null,
            bool rateLimited = false,
            int retryAfter = 45)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;

            if (rateLimited)
            {
                ViewData["RetryAfter"] = Math.Clamp(retryAfter, 1, 45);
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(
            string? email,
            string? password,
            bool rememberMe,
            string? returnUrl = null)
        {
            // 1. Validate email
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email address is required."
                );
            }
            else if (!new EmailAddressAttribute().IsValid(email.Trim()))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please enter a valid email address."
                );
            }

            // 2. Validate password
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Password is required."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // 3. Find registered user by email
            var user = await _userManager.FindByEmailAsync(email!.Trim());

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email address is not registered."
                );

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // 4. Check password using Identity
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                password!,
                rememberMe,
                lockoutOnFailure: false
            );

            // 5. Successful login
            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            // 6. Account lockout
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is locked. Please try again later."
                );
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Incorrect password. Please try again."
                );
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }




        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeMeAdmin(string userId)
        {
            // Step 1: Check User ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is missing.";
                return RedirectToAction(nameof(Users));
            }

            // Step 2: Find user
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User account not found.";
                return RedirectToAction(nameof(Users));
            }

            // Step 3: Prevent changing your own role
            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction(nameof(Users));
            }

            // Step 4: Check whether user is already Admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "This account is already an Admin.";
                return RedirectToAction(nameof(Users));
            }

            // Step 5: Remove existing roles
            var existingRoles = await _userManager.GetRolesAsync(user);

            if (existingRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user, existingRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] = "Could not remove existing role.";
                    return RedirectToAction(nameof(Users));
                }
            }

            // Step 6: Assign Admin role
            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                TempData["Success"] =
                    $"{user.Email} has successfully been promoted to Admin.";
            }
            else
            {
                TempData["Error"] = string.Join(" | ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeSupportStaff(string userId)
        {
            // Step 1: Check User ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is missing.";
                return RedirectToAction(nameof(Users));
            }

            // Step 2: Find user
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User account not found.";
                return RedirectToAction(nameof(Users));
            }

            // Step 3: Prevent changing your own role
            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction(nameof(Users));
            }

            // Step 4: Check whether user is already SupportStaff
            if (await _userManager.IsInRoleAsync(user, "SupportStaff"))
            {
                TempData["Error"] = "This account is already Support Staff.";
                return RedirectToAction(nameof(Users));
            }

            // Step 5: Do not change an Admin account
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Admin accounts cannot be changed to Support Staff.";
                return RedirectToAction(nameof(Users));
            }

            // Step 6: Remove existing roles
            var existingRoles = await _userManager.GetRolesAsync(user);

            if (existingRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user, existingRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] = "Could not remove existing role.";
                    return RedirectToAction(nameof(Users));
                }
            }

            // Step 7: Assign SupportStaff role
            var result = await _userManager.AddToRoleAsync(user, "SupportStaff");

            if (result.Succeeded)
            {
                TempData["Success"] =
                    $"{user.Email} has successfully been promoted to Support Staff.";
            }
            else
            {
                TempData["Error"] = string.Join(" | ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();

            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles;
            }

            ViewBag.UserRoles = userRoles;

            return View(users);
        }



        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUserRole(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Admin accounts cannot be changed to Employee.";
                return RedirectToAction(nameof(Users));
            }

            var existingRoles = await _userManager.GetRolesAsync(user);

            if (existingRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user, existingRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] = "Could not remove existing roles.";
                    return RedirectToAction(nameof(Users));
                }
            }

            var result = await _userManager.AddToRoleAsync(user, "User");

            if (result.Succeeded)
            {
                TempData["Success"] = "Account is now an Employee.";
            }
            else
            {
                TempData["Error"] = string.Join(", ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }




        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeEmployee(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is missing.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction(nameof(Users));
            }

            var existingRoles = await _userManager.GetRolesAsync(user);

            if (existingRoles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                if (admins.Count <= 1)
                {
                    TempData["Error"] = "At least one Admin must remain.";
                    return RedirectToAction(nameof(Users));
                }
            }

            if (existingRoles.Count == 1 && existingRoles.Contains("User"))
            {
                TempData["Error"] = "This account is already an Employee.";
                return RedirectToAction(nameof(Users));
            }

            if (existingRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user, existingRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] = "Could not remove existing roles.";
                    return RedirectToAction(nameof(Users));
                }
            }

            var result = await _userManager.AddToRoleAsync(user, "User");

            if (result.Succeeded)
            {
                TempData["Success"] = $"{user.Email} is now an Employee.";
            }
            else
            {
                TempData["Error"] = string.Join(", ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeEmployeeAndSupportStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            if (!await _userManager.IsInRoleAsync(user, "SupportStaff"))
            {
                await _userManager.AddToRoleAsync(user, "SupportStaff");
            }

            TempData["Success"] =
                "Employee and Support Staff roles assigned successfully.";

            return RedirectToAction(nameof(Users));
        }


        // GET: Account/MyProfile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var model = new MyProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }


        // POST: Account/MyProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(MyProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();

            var email = model.Email.Trim();

            if (!string.Equals(user.Email, email,
                StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "This email address is already registered.");

                    return View(model);
                }

                user.Email = email;
                user.UserName = email;
                user.NormalizedEmail = _userManager.NormalizeEmail(email);
                user.NormalizedUserName = _userManager.NormalizeName(email);
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(MyProfile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

    }
}

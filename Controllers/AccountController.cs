
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCIS.Models;
using SCIS.Services;
using SCIS.ViewModels;
using System;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserService _userService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            UserService userService,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Attempting login for user: {model.Email}");

                // Check if user exists
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: User {model.Email} not found");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Login failed: User {model.Email} is inactive");
                    ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact an administrator.");
                    return View(model);
                }

                // Special case for admin user
                if (model.Email == "admin@gmail.com" && model.Password == "admin")
                {
                    _logger.LogInformation("Admin login detected with hardcoded credentials");

                    // Force sign in for admin
                    await _signInManager.SignInAsync(user, model.RememberMe);

                    _logger.LogInformation($"Admin login successful, redirecting to Admin/Index");
                    return RedirectToAction("Index", "Admin");
                }

                // Normal login flow
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Login successful for user: {model.Email}");

                    // Redirect based on user type
                    if (user.UserType == Constants.UserTypes.Admin)
                    {
                        _logger.LogInformation($"Redirecting admin user to Admin/Index");
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (user.UserType == Constants.UserTypes.ClubManager)
                    {
                        _logger.LogInformation($"Redirecting club manager to ClubManager/Index");
                        return RedirectToAction("Index", "ClubManager");
                    }
                    else
                    {
                        _logger.LogInformation($"Redirecting regular user to Home/Index");
                        return RedirectToAction("Index", "Home");
                    }
                }

                _logger.LogWarning($"Login failed for user {model.Email}: {result}");
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create the user with default type and role (will be updated by admin later)
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    UserType = "User", // Default user type
                    UserRole = "Student", // Default user role
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to the default Student role
                    await _userManager.AddToRoleAsync(user, "Student");

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // All new registrations go to the Home controller
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            // Repopulate the dropdown lists
            if (model.UserType != null)
            {
                model.AvailableUserRoles = _userService.GetRolesForUserType(model.UserType);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCIS.Data;
using SCIS.Models;
using SCIS.ViewModels;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get user's club memberships
            var memberships = await _context.Memberships
                .Include(m => m.Club)
                .Where(m => m.UserId == user.Id)
                .ToListAsync();

            // Get user's events
            var events = await _context.Events
                .Where(e => e.RegisteredUsers != null && e.RegisteredUsers.Contains(user.Id))
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                User = user,
                Memberships = memberships,
                Events = events
            };

            return View(model);
        }

        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new SettingsViewModel
            {
                FullName = user.FullName,
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Settings", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Update user profile
            user.FullName = model.FullName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Settings", model);
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var settingsModel = new SettingsViewModel
                {
                    FullName = (await _userManager.GetUserAsync(User)).FullName,
                    Email = User.Identity.Name
                };
                return View("Settings", settingsModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                await _signInManager.RefreshSignInAsync(user);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                var settingsModel = new SettingsViewModel
                {
                    FullName = user.FullName,
                    Email = user.Email
                };
                return View("Settings", settingsModel);
            }

            return RedirectToAction(nameof(Settings));
        }
    }
}

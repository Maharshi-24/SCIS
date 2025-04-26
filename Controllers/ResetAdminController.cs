using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCIS.Models;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    [Route("reset-admin")]
    public class ResetAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetAdminController> _logger;

        public ResetAdminController(
            UserManager<ApplicationUser> userManager,
            ILogger<ResetAdminController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Check if admin user exists
            var adminUser = await _userManager.FindByEmailAsync("admin@gmail.com");
            if (adminUser == null)
            {
                // Create admin user
                adminUser = new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    UserType = Constants.UserTypes.Admin,
                    UserRole = Constants.UserRoles.SystemAdmin,
                    IsActive = true
                };

                _logger.LogInformation("Creating admin user with password 'admin'");
                var result = await _userManager.CreateAsync(adminUser, "admin");
                if (result.Succeeded)
                {
                    // Add admin to role
                    await _userManager.AddToRoleAsync(adminUser, Constants.UserRoles.SystemAdmin);
                    _logger.LogInformation("Admin user created successfully.");
                    return Content("Admin user created successfully with email 'admin@gmail.com' and password 'admin'");
                }
                else
                {
                    _logger.LogError("Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"  - {error.Description}");
                    }
                    return Content($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                _logger.LogInformation("Admin user already exists. Resetting password...");
                
                // Reset admin password
                var token = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                var resetResult = await _userManager.ResetPasswordAsync(adminUser, token, "admin");
                if (resetResult.Succeeded)
                {
                    _logger.LogInformation("Reset admin password to 'admin'");
                    
                    // Ensure admin is active
                    adminUser.IsActive = true;
                    await _userManager.UpdateAsync(adminUser);
                    
                    // Ensure admin has the correct role
                    var roles = await _userManager.GetRolesAsync(adminUser);
                    if (!roles.Contains(Constants.UserRoles.SystemAdmin))
                    {
                        await _userManager.AddToRoleAsync(adminUser, Constants.UserRoles.SystemAdmin);
                    }
                    
                    return Content("Admin password reset successfully to 'admin'");
                }
                else
                {
                    _logger.LogError("Failed to reset admin password:");
                    foreach (var error in resetResult.Errors)
                    {
                        _logger.LogError($"  - {error.Description}");
                    }
                    return Content($"Failed to reset admin password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}

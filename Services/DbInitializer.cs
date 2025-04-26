using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SCIS.Data;
using SCIS.Models;

namespace SCIS.Services
{
    public class DbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DbInitializer> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task Initialize()
        {
            try
            {
                // Apply migrations if they are not applied
                if (_context.Database.GetPendingMigrations().Any())
                {
                    _logger.LogInformation("Applying migrations...");
                    await _context.Database.MigrateAsync();
                }

                // Seed roles
                await SeedRoles();

                // Seed admin user
                await SeedAdminUser();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database.");
            }
        }

        private async Task SeedRoles()
        {
            // Create Admin Roles
            await CreateRoleIfNotExists(Constants.UserRoles.SystemAdmin);
            await CreateRoleIfNotExists(Constants.UserRoles.ContentAdmin);

            // Create Club Manager Roles
            await CreateRoleIfNotExists(Constants.UserRoles.ClubPresident);
            await CreateRoleIfNotExists(Constants.UserRoles.ClubSecretary);
            await CreateRoleIfNotExists(Constants.UserRoles.ClubTreasurer);

            // Create User Roles
            await CreateRoleIfNotExists(Constants.UserRoles.Student);
            await CreateRoleIfNotExists(Constants.UserRoles.Faculty);
            await CreateRoleIfNotExists(Constants.UserRoles.Staff);
            await CreateRoleIfNotExists(Constants.UserRoles.Alumni);
        }

        private async Task CreateRoleIfNotExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation($"Created role: {roleName}");
            }
        }

        private async Task SeedAdminUser()
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
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _logger.LogInformation("Creating admin user with password 'admin'");
                var result = await _userManager.CreateAsync(adminUser, "admin");
                if (result.Succeeded)
                {
                    // Add admin to role
                    await _userManager.AddToRoleAsync(adminUser, Constants.UserRoles.SystemAdmin);
                    _logger.LogInformation("Admin user created successfully.");
                }
                else
                {
                    _logger.LogError("Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"  - {error.Description}");
                    }
                }
            }
            else
            {
                _logger.LogInformation("Admin user already exists.");

                // Ensure admin user has the correct role
                var roles = await _userManager.GetRolesAsync(adminUser);
                if (!roles.Contains(Constants.UserRoles.SystemAdmin))
                {
                    await _userManager.AddToRoleAsync(adminUser, Constants.UserRoles.SystemAdmin);
                    _logger.LogInformation("Added SystemAdmin role to existing admin user.");
                }

                // Reset admin password if needed
                var token = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                var resetResult = await _userManager.ResetPasswordAsync(adminUser, token, "admin");
                if (resetResult.Succeeded)
                {
                    _logger.LogInformation("Reset admin password to 'admin'");
                }
                else
                {
                    _logger.LogError("Failed to reset admin password:");
                    foreach (var error in resetResult.Errors)
                    {
                        _logger.LogError($"  - {error.Description}");
                    }
                }
            }

            // Check if content admin user exists
            if (await _userManager.FindByEmailAsync("contentadmin@gmail.com") == null)
            {
                // Create content admin user
                var contentAdminUser = new ApplicationUser
                {
                    UserName = "contentadmin@gmail.com",
                    Email = "contentadmin@gmail.com",
                    EmailConfirmed = true,
                    FullName = "Content Administrator",
                    UserType = Constants.UserTypes.Admin,
                    UserRole = Constants.UserRoles.ContentAdmin,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(contentAdminUser, "password");
                if (result.Succeeded)
                {
                    // Add admin to role
                    await _userManager.AddToRoleAsync(contentAdminUser, Constants.UserRoles.ContentAdmin);
                    _logger.LogInformation("Content admin user created successfully.");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Error creating content admin user: {error.Description}");
                    }
                }
            }
        }
    }
}

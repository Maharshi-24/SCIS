using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCIS.Data;
using SCIS.Models;
using SCIS.Services;
using SCIS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    [Authorize(Roles = "SystemAdmin,ContentAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserService _userService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            UserService userService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.FullName = currentUser?.FullName;
            ViewBag.UserType = currentUser?.UserType;
            ViewBag.UserRole = currentUser?.UserRole;

            // Get counts for dashboard
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalClubs = await _context.Clubs.CountAsync();
            ViewBag.TotalEvents = await _context.Events.CountAsync();

            // Get recent activities
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentClubs = await _context.Clubs
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentEvents = await _context.Events
                .OrderByDescending(e => e.EventDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentUsers = recentUsers;
            ViewBag.RecentClubs = recentClubs;
            ViewBag.RecentEvents = recentEvents;

            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();

            // Get roles for each user
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.UserRole = roles.FirstOrDefault() ?? user.UserRole;
            }

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                // Log the ID for debugging
                Console.WriteLine($"DeleteUser called with id: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "User ID is required.";
                    return RedirectToAction(nameof(Users));
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found.";
                    return RedirectToAction(nameof(Users));
                }

                // Check if trying to delete self
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && user.Id == currentUser.Id)
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(Users));
                }

                // Check if this is the last admin user
                if (user.UserType == Constants.UserTypes.Admin)
                {
                    var adminCount = await _context.Users.CountAsync(u => u.UserType == Constants.UserTypes.Admin);
                    if (adminCount <= 1)
                    {
                        TempData["ErrorMessage"] = "Cannot delete the last admin user.";
                        return RedirectToAction(nameof(Users));
                    }
                }

                // Check if user is a club president
                var isClubPresident = await _context.Clubs.AnyAsync(c => c.PresidentId == user.Id);
                if (isClubPresident)
                {
                    TempData["ErrorMessage"] = "Cannot delete a user who is a club president. Please assign a new president to their clubs first.";
                    return RedirectToAction(nameof(Users));
                }

                // Delete user's memberships
                var memberships = await _context.Memberships.Where(m => m.UserId == user.Id).ToListAsync();
                _context.Memberships.RemoveRange(memberships);

                // Delete user's event registrations
                var events = await _context.Events
                    .Where(e => e.RegisteredUsers != null && e.RegisteredUsers.Contains(user.Id))
                    .ToListAsync();

                foreach (var evt in events)
                {
                    var registeredUsers = evt.RegisteredUsers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    registeredUsers.Remove(user.Id);
                    evt.RegisteredUsers = string.Join(",", registeredUsers);
                }

                // Delete user's event winners
                var eventsWithWinners = await _context.Events
                    .Where(e => e.Winners != null && e.Winners.Contains(user.Id))
                    .ToListAsync();

                foreach (var evt in eventsWithWinners)
                {
                    var winners = evt.Winners.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    winners.Remove(user.Id);
                    evt.Winners = string.Join(",", winners);
                }

                await _context.SaveChangesAsync();

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User {user.FullName} has been deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error deleting user: {ex.Message}");
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction(nameof(EditUser), new { id = userId });
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction(nameof(EditUser), new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Remove existing password
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                TempData["ErrorMessage"] = $"Error removing existing password: {string.Join(", ", removePasswordResult.Errors.Select(e => e.Description))}";
                return RedirectToAction(nameof(EditUser), new { id = userId });
            }

            // Add new password
            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (addPasswordResult.Succeeded)
            {
                TempData["SuccessMessage"] = $"Password for {user.FullName} has been reset successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error setting new password: {string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction(nameof(EditUser), new { id = userId });
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel
            {
                AvailableUserTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = Constants.UserTypes.Admin, Text = "Administrator" },
                    new SelectListItem { Value = Constants.UserTypes.ClubManager, Text = "Club Manager" },
                    new SelectListItem { Value = Constants.UserTypes.User, Text = "Regular User" }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists");

                    // Repopulate dropdown lists
                    model.AvailableUserTypes = new List<SelectListItem>
                    {
                        new SelectListItem { Value = Constants.UserTypes.Admin, Text = "Administrator" },
                        new SelectListItem { Value = Constants.UserTypes.ClubManager, Text = "Club Manager" },
                        new SelectListItem { Value = Constants.UserTypes.User, Text = "Regular User" }
                    };

                    model.AvailableUserRoles = _userService.GetRolesForUserType(model.UserType);

                    return View(model);
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    UserType = model.UserType,
                    UserRole = model.UserRole,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to role
                    await _userManager.AddToRoleAsync(user, model.UserRole);

                    TempData["SuccessMessage"] = "User created successfully";
                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            model.AvailableUserTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = Constants.UserTypes.Admin, Text = "Administrator" },
                new SelectListItem { Value = Constants.UserTypes.ClubManager, Text = "Club Manager" },
                new SelectListItem { Value = Constants.UserTypes.User, Text = "Regular User" }
            };

            model.AvailableUserRoles = _userService.GetRolesForUserType(model.UserType);

            return View(model);
        }

        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Get current role
            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? user.UserRole;

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                UserType = user.UserType,
                UserRole = currentRole,
                IsActive = user.IsActive,
                AvailableUserTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = Constants.UserTypes.Admin, Text = "Administrator", Selected = user.UserType == Constants.UserTypes.Admin },
                    new SelectListItem { Value = Constants.UserTypes.ClubManager, Text = "Club Manager", Selected = user.UserType == Constants.UserTypes.ClubManager },
                    new SelectListItem { Value = Constants.UserTypes.User, Text = "Regular User", Selected = user.UserType == Constants.UserTypes.User }
                }
            };

            // Get roles based on selected user type
            model.AvailableUserRoles = _userService.GetRolesForUserType(user.UserType);

            // Set selected role
            foreach (var role in model.AvailableUserRoles)
            {
                role.Selected = role.Value == currentRole;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Update user properties
                user.FullName = model.FullName;
                user.UserType = model.UserType;
                user.UserRole = model.UserRole;
                user.IsActive = model.IsActive;

                // Update user
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update roles if changed
                    if (!currentRoles.Contains(model.UserRole))
                    {
                        // Remove from current roles
                        foreach (var role in currentRoles)
                        {
                            await _userManager.RemoveFromRoleAsync(user, role);
                        }

                        // Add to new role
                        await _userManager.AddToRoleAsync(user, model.UserRole);
                    }

                    TempData["SuccessMessage"] = "User updated successfully";
                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            model.AvailableUserTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = Constants.UserTypes.Admin, Text = "Administrator", Selected = model.UserType == Constants.UserTypes.Admin },
                new SelectListItem { Value = Constants.UserTypes.ClubManager, Text = "Club Manager", Selected = model.UserType == Constants.UserTypes.ClubManager },
                new SelectListItem { Value = Constants.UserTypes.User, Text = "Regular User", Selected = model.UserType == Constants.UserTypes.User }
            };

            model.AvailableUserRoles = _userService.GetRolesForUserType(model.UserType);

            // Set selected role
            foreach (var role in model.AvailableUserRoles)
            {
                role.Selected = role.Value == model.UserRole;
            }

            return View(model);
        }

        // This method is no longer needed as we have a more comprehensive DeleteUser method above
        // Keeping this commented out for reference
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserFromEdit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if trying to delete self
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account";
                return RedirectToAction(nameof(Users));
            }

            // Check if user is a club president
            var isClubPresident = await _context.Clubs.AnyAsync(c => c.PresidentId == user.Id);
            if (isClubPresident)
            {
                TempData["ErrorMessage"] = "Cannot delete user who is a club president. Please assign a new president to their clubs first.";
                return RedirectToAction(nameof(Users));
            }

            // Delete user's memberships
            var memberships = await _context.Memberships.Where(m => m.UserId == user.Id).ToListAsync();
            _context.Memberships.RemoveRange(memberships);

            // Delete user
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user";
            }

            return RedirectToAction(nameof(Users));
        }
        */

        [HttpGet]
        public IActionResult GetRolesForUserType(string userType)
        {
            if (string.IsNullOrEmpty(userType))
            {
                return BadRequest("User type is required");
            }

            var roles = _userService.GetRolesForUserType(userType);
            return Json(roles);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email is required" });
            }

            // First try exact match
            var user = await _userManager.FindByEmailAsync(email);

            // If no exact match, try partial match
            if (user == null)
            {
                // Search for users with email containing the search term
                var users = await _userManager.Users
                    .Where(u => u.Email.Contains(email) || u.FullName.Contains(email))
                    .OrderBy(u => u.Email)
                    .Take(1)
                    .ToListAsync();

                if (users.Any())
                {
                    user = users.First();
                }
            }

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            return Json(new {
                success = true,
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                userType = user.UserType,
                userRole = user.UserRole
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(string userId, int clubId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || clubId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid parameters: userId or clubId is missing";
                    return RedirectToAction(nameof(Clubs));
                }

                // Log the parameters for debugging
                Console.WriteLine($"RemoveMember called with userId: {userId}, clubId: {clubId}");

                var membership = await _context.Memberships
                    .FirstOrDefaultAsync(m => m.UserId == userId && m.ClubId == clubId);

                if (membership == null)
                {
                    TempData["ErrorMessage"] = $"Membership not found for user {userId} in club {clubId}";
                    return RedirectToAction(nameof(ClubDetails), new { id = clubId });
                }

                // Check if user is the president
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubId == clubId);
                if (club != null && club.PresidentId == userId)
                {
                    TempData["ErrorMessage"] = "Cannot remove the club president. Please assign a new president first.";
                    return RedirectToAction(nameof(ClubDetails), new { id = clubId });
                }

                _context.Memberships.Remove(membership);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Member removed successfully";
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error removing member: {ex.Message}");
                TempData["ErrorMessage"] = $"Error removing member: {ex.Message}";
            }

            return RedirectToAction(nameof(ClubDetails), new { id = clubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMembership(string userId, int clubId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || clubId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid parameters: userId or clubId is missing";
                    return RedirectToAction(nameof(Clubs));
                }

                // Log the parameters for debugging
                Console.WriteLine($"ApproveMembership called with userId: {userId}, clubId: {clubId}");

                var membership = await _context.Memberships
                    .FirstOrDefaultAsync(m => m.UserId == userId && m.ClubId == clubId);

                if (membership == null)
                {
                    TempData["ErrorMessage"] = $"Membership request not found for user {userId} in club {clubId}";
                    return RedirectToAction(nameof(ClubDetails), new { id = clubId });
                }

                membership.Status = Constants.MembershipStatus.Approved;
                membership.JoinedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Membership approved successfully";
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error approving membership: {ex.Message}");
                TempData["ErrorMessage"] = $"Error approving membership: {ex.Message}";
            }

            return RedirectToAction(nameof(ClubDetails), new { id = clubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectMembership(string userId, int clubId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || clubId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid parameters: userId or clubId is missing";
                    return RedirectToAction(nameof(Clubs));
                }

                // Log the parameters for debugging
                Console.WriteLine($"RejectMembership called with userId: {userId}, clubId: {clubId}");

                var membership = await _context.Memberships
                    .FirstOrDefaultAsync(m => m.UserId == userId && m.ClubId == clubId);

                if (membership == null)
                {
                    TempData["ErrorMessage"] = $"Membership request not found for user {userId} in club {clubId}";
                    return RedirectToAction(nameof(ClubDetails), new { id = clubId });
                }

                _context.Memberships.Remove(membership);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Membership request rejected";
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error rejecting membership: {ex.Message}");
                TempData["ErrorMessage"] = $"Error rejecting membership: {ex.Message}";
            }

            return RedirectToAction(nameof(ClubDetails), new { id = clubId });
        }

        public async Task<IActionResult> Clubs()
        {
            var clubs = await _context.Clubs
                .Include(c => c.President)
                .Include(c => c.CreatedByAdmin)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Get membership counts for each club
            var clubIds = clubs.Select(c => c.Id).ToList();
            var membershipCounts = await _context.Memberships
                .Where(m => clubIds.Contains(m.ClubId) && m.Status == Constants.MembershipStatus.Approved)
                .GroupBy(m => m.ClubId)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count);

            // Get event counts for each club
            var eventCounts = await _context.Events
                .Where(e => clubIds.Contains(e.ClubId))
                .GroupBy(e => e.ClubId)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count);

            var clubViewModels = clubs.Select(c => new AdminClubViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                IsActive = c.IsActive,
                PresidentName = c.President?.FullName ?? "Not Assigned",
                CreatedByAdminName = c.CreatedByAdmin?.FullName ?? "System",
                MemberCount = membershipCounts.ContainsKey(c.Id) ? membershipCounts[c.Id] : 0,
                EventCount = eventCounts.ContainsKey(c.Id) ? eventCounts[c.Id] : 0
            }).ToList();

            return View(clubViewModels);
        }

        [HttpGet]
        public IActionResult CreateClub()
        {
            var model = new AdminClubCreateViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClub(AdminClubCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Check if club name already exists
                var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.Name == model.Name);
                if (existingClub != null)
                {
                    ModelState.AddModelError("Name", "A club with this name already exists");
                    return View(model);
                }

                // Create new club
                var club = new Club
                {
                    Name = model.Name,
                    Description = model.Description,
                    CreatedAt = DateTime.Now,
                    IsActive = model.IsActive,
                    CreatedByAdminId = currentUser.Id
                };

                // If president is specified, assign them
                string presidentId = null;
                if (!string.IsNullOrEmpty(model.PresidentId))
                {
                    var president = await _userManager.FindByIdAsync(model.PresidentId);
                    if (president != null)
                    {
                        club.PresidentId = president.Id;
                        presidentId = president.Id;

                        // Update user role if needed
                        if (president.UserType != Constants.UserTypes.ClubManager)
                        {
                            president.UserType = Constants.UserTypes.ClubManager;
                            president.UserRole = Constants.UserRoles.ClubPresident;
                            await _userManager.UpdateAsync(president);

                            // Add to role
                            if (!await _userManager.IsInRoleAsync(president, Constants.UserRoles.ClubPresident))
                            {
                                await _userManager.AddToRoleAsync(president, Constants.UserRoles.ClubPresident);
                            }
                        }
                    }
                }

                _context.Clubs.Add(club);
                await _context.SaveChangesAsync();

                // Now that the club has an ID, add the president as a member
                if (!string.IsNullOrEmpty(presidentId))
                {
                    var membership = new Membership
                    {
                        UserId = presidentId,
                        ClubId = club.ClubId,
                        JoinedAt = DateTime.Now,
                        Status = Constants.MembershipStatus.Approved
                    };

                    _context.Memberships.Add(membership);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Club created successfully";
                return RedirectToAction(nameof(Clubs));
            }

            return View(model);
        }

        public async Task<IActionResult> EditClub(int id)
        {
            var club = await _context.Clubs
                .Include(c => c.President)
                .FirstOrDefaultAsync(c => c.ClubId == id);

            if (club == null)
            {
                return NotFound();
            }

            var model = new AdminClubEditViewModel
            {
                Id = club.Id,
                Name = club.Name,
                Description = club.Description,
                IsActive = club.IsActive,
                PresidentId = club.PresidentId
            };

            // Get potential presidents (club managers and regular users)
            var potentialPresidents = await _userManager.Users
                .Where(u => u.IsActive && (u.UserType == Constants.UserTypes.ClubManager || u.UserType == Constants.UserTypes.User))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            model.AvailablePresidents = potentialPresidents.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == club.PresidentId
            }).ToList();

            // Add empty option
            model.AvailablePresidents.Insert(0, new SelectListItem { Value = "", Text = "-- Select President --" });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClub(AdminClubEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var club = await _context.Clubs.FindAsync(model.Id);
                if (club == null)
                {
                    return NotFound();
                }

                // Check if club name already exists (excluding current club)
                var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.Name == model.Name && c.ClubId != model.Id);
                if (existingClub != null)
                {
                    ModelState.AddModelError("Name", "A club with this name already exists");

                    // Repopulate dropdown
                    var potentialPresidents = await _userManager.Users
                        .Where(u => u.IsActive && (u.UserType == Constants.UserTypes.ClubManager || u.UserType == Constants.UserTypes.User))
                        .OrderBy(u => u.FullName)
                        .ToListAsync();

                    model.AvailablePresidents = potentialPresidents.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = $"{u.FullName} ({u.Email})",
                        Selected = u.Id == model.PresidentId
                    }).ToList();

                    // Add empty option
                    model.AvailablePresidents.Insert(0, new SelectListItem { Value = "", Text = "-- Select President --" });

                    return View(model);
                }

                // Handle president change
                if (club.PresidentId != model.PresidentId)
                {
                    // If there was a previous president
                    if (!string.IsNullOrEmpty(club.PresidentId))
                    {
                        // Check if previous president is president of any other club
                        var otherClubsWithSamePresident = await _context.Clubs
                            .Where(c => c.PresidentId == club.PresidentId && c.ClubId != club.ClubId)
                            .AnyAsync();

                        if (!otherClubsWithSamePresident)
                        {
                            // Previous president is not president of any other club
                            // We could demote them here if needed
                            var previousPresident = await _userManager.FindByIdAsync(club.PresidentId);
                            if (previousPresident != null)
                            {
                                // Only change role if they're not president of any other club
                                previousPresident.UserRole = Constants.UserRoles.Member;
                                await _userManager.UpdateAsync(previousPresident);

                                // Remove from role
                                if (await _userManager.IsInRoleAsync(previousPresident, Constants.UserRoles.ClubPresident))
                                {
                                    await _userManager.RemoveFromRoleAsync(previousPresident, Constants.UserRoles.ClubPresident);
                                    await _userManager.AddToRoleAsync(previousPresident, Constants.UserRoles.Member);
                                }
                            }
                        }
                    }

                    // If new president is assigned
                    if (!string.IsNullOrEmpty(model.PresidentId))
                    {
                        var newPresident = await _userManager.FindByIdAsync(model.PresidentId);
                        if (newPresident != null)
                        {
                            // Update user type and role
                            newPresident.UserType = Constants.UserTypes.ClubManager;
                            newPresident.UserRole = Constants.UserRoles.ClubPresident;
                            await _userManager.UpdateAsync(newPresident);

                            // Add to role
                            if (!await _userManager.IsInRoleAsync(newPresident, Constants.UserRoles.ClubPresident))
                            {
                                // Remove from current roles
                                var currentRoles = await _userManager.GetRolesAsync(newPresident);
                                if (currentRoles.Any())
                                {
                                    await _userManager.RemoveFromRolesAsync(newPresident, currentRoles);
                                }

                                await _userManager.AddToRoleAsync(newPresident, Constants.UserRoles.ClubPresident);
                            }

                            // Add as member if not already
                            var membership = await _context.Memberships
                                .FirstOrDefaultAsync(m => m.UserId == newPresident.Id && m.ClubId == club.ClubId);

                            if (membership == null)
                            {
                                membership = new Membership
                                {
                                    UserId = newPresident.Id,
                                    ClubId = club.ClubId,
                                    JoinedAt = DateTime.Now,
                                    Status = Constants.MembershipStatus.Approved
                                };

                                _context.Memberships.Add(membership);
                            }
                            else if (membership.Status != Constants.MembershipStatus.Approved)
                            {
                                membership.Status = Constants.MembershipStatus.Approved;
                            }
                        }
                    }
                }

                // Update club properties
                club.Name = model.Name;
                club.Description = model.Description;
                club.IsActive = model.IsActive;
                club.PresidentId = model.PresidentId;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Club updated successfully";
                return RedirectToAction(nameof(Clubs));
            }

            // If we got this far, something failed, redisplay form
            var potentialPresidents2 = await _userManager.Users
                .Where(u => u.IsActive && (u.UserType == Constants.UserTypes.ClubManager || u.UserType == Constants.UserTypes.User))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            model.AvailablePresidents = potentialPresidents2.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == model.PresidentId
            }).ToList();

            // Add empty option
            model.AvailablePresidents.Insert(0, new SelectListItem { Value = "", Text = "-- Select President --" });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClub(int id)
        {
            try
            {
                // Log the incoming ID for debugging
                Console.WriteLine($"Attempting to delete club with ID: {id}");

                // First try to find by ClubId directly
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubId == id);

                if (club == null)
                {
                    // If not found by ClubId, try to find by Id (which is an alias for ClubId)
                    club = await _context.Clubs.FindAsync(id);
                }

                if (club == null)
                {
                    TempData["ErrorMessage"] = $"Club with ID {id} not found.";
                    return RedirectToAction(nameof(Clubs));
                }

                // Check if club has members
                var memberCount = await _context.Memberships.CountAsync(m => m.ClubId == club.ClubId);
                if (memberCount > 0)
                {
                    TempData["ErrorMessage"] = "Cannot delete club with members. Please remove all members first.";
                    return RedirectToAction(nameof(Clubs));
                }

                // Check if club has events
                var eventCount = await _context.Events.CountAsync(e => e.ClubId == club.ClubId);
                if (eventCount > 0)
                {
                    TempData["ErrorMessage"] = "Cannot delete club with events. Please remove all events first.";
                    return RedirectToAction(nameof(Clubs));
                }

                // Delete club
                _context.Clubs.Remove(club);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Club deleted successfully";
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error deleting club: {ex.Message}");
                TempData["ErrorMessage"] = $"Error deleting club: {ex.Message}";
            }

            return RedirectToAction(nameof(Clubs));
        }

        public async Task<IActionResult> ClubDetails(int id)
        {
            var club = await _context.Clubs
                .Include(c => c.President)
                .Include(c => c.CreatedByAdmin)
                .FirstOrDefaultAsync(c => c.ClubId == id);

            if (club == null)
            {
                return NotFound();
            }

            // Get club members
            var members = await _context.Memberships
                .Include(m => m.User)
                .Where(m => m.ClubId == id && m.Status == Constants.MembershipStatus.Approved)
                .OrderBy(m => m.User.FullName)
                .ToListAsync();

            // Get pending membership requests
            var pendingRequests = await _context.Memberships
                .Include(m => m.User)
                .Where(m => m.ClubId == id && m.Status == Constants.MembershipStatus.Pending)
                .OrderBy(m => m.RequestedAt)
                .ToListAsync();

            // Get club events
            var events = await _context.Events
                .Where(e => e.ClubId == id)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            var model = new AdminClubDetailsViewModel
            {
                Club = club,
                Members = members,
                PendingRequests = pendingRequests,
                Events = events
            };

            return View(model);
        }

        public async Task<IActionResult> Events()
        {
            var events = await _context.Events
                .Include(e => e.Club)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            var eventViewModels = new List<AdminEventViewModel>();

            foreach (var evt in events)
            {
                var registrationCount = 0;
                if (!string.IsNullOrEmpty(evt.RegisteredUsers))
                {
                    registrationCount = evt.RegisteredUsers.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                }

                eventViewModels.Add(new AdminEventViewModel
                {
                    Id = evt.Id,
                    Name = evt.Name,
                    Description = evt.Description,
                    EventDate = evt.EventDate,
                    Location = evt.Location,
                    MaxAttendees = evt.MaxAttendees,
                    RegistrationCount = registrationCount,
                    ClubName = evt.Club?.Name ?? "Unknown",
                    ClubId = evt.ClubId,
                    IsActive = evt.IsActive,
                    IsPast = evt.EventDate < DateTime.Now
                });
            }

            return View(eventViewModels);
        }

        public async Task<IActionResult> EventDetails(int id)
        {
            var evt = await _context.Events
                .Include(e => e.Club)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evt == null)
            {
                return NotFound();
            }

            // Get registered users
            var registeredUsers = new List<ApplicationUser>();
            if (!string.IsNullOrEmpty(evt.RegisteredUsers))
            {
                var userIds = evt.RegisteredUsers.Split(',', StringSplitOptions.RemoveEmptyEntries);
                registeredUsers = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();
            }

            // Get winners
            var winners = new List<ApplicationUser>();
            if (!string.IsNullOrEmpty(evt.Winners))
            {
                var winnerIds = evt.Winners.Split(',', StringSplitOptions.RemoveEmptyEntries);
                winners = await _userManager.Users
                    .Where(u => winnerIds.Contains(u.Id))
                    .ToListAsync();
            }

            var model = new AdminEventDetailsViewModel
            {
                Event = evt,
                RegisteredUsers = registeredUsers,
                Winners = winners
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEventStatus(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            evt.IsActive = !evt.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event {(evt.IsActive ? "activated" : "deactivated")} successfully";
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            // Check if event has registrations
            if (!string.IsNullOrEmpty(evt.RegisteredUsers) && evt.RegisteredUsers.Split(',').Length > 0)
            {
                TempData["ErrorMessage"] = "Cannot delete event with registrations";
                return RedirectToAction(nameof(Events));
            }

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully";
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRegistration(int eventId, string userId)
        {
            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(evt.RegisteredUsers))
            {
                TempData["ErrorMessage"] = "No registrations found";
                return RedirectToAction(nameof(EventDetails), new { id = eventId });
            }

            var userIds = evt.RegisteredUsers.Split(',').ToList();
            if (userIds.Contains(userId))
            {
                userIds.Remove(userId);
                evt.RegisteredUsers = string.Join(",", userIds);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration removed successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "User not registered for this event";
            }

            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWinner(int eventId, string userId)
        {
            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null)
            {
                return NotFound();
            }

            // Check if event is past
            if (evt.EventDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Cannot add winners for future events";
                return RedirectToAction(nameof(EventDetails), new { id = eventId });
            }

            // Check if user is registered
            if (string.IsNullOrEmpty(evt.RegisteredUsers) || !evt.RegisteredUsers.Split(',').Contains(userId))
            {
                TempData["ErrorMessage"] = "User is not registered for this event";
                return RedirectToAction(nameof(EventDetails), new { id = eventId });
            }

            // Add user to winners
            var winnerIds = string.IsNullOrEmpty(evt.Winners)
                ? new List<string>()
                : evt.Winners.Split(',').ToList();

            if (!winnerIds.Contains(userId))
            {
                winnerIds.Add(userId);
                evt.Winners = string.Join(",", winnerIds);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Winner added successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "User is already a winner";
            }

            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWinner(int eventId, string userId)
        {
            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(evt.Winners))
            {
                TempData["ErrorMessage"] = "No winners found";
                return RedirectToAction(nameof(EventDetails), new { id = eventId });
            }

            var winnerIds = evt.Winners.Split(',').ToList();
            if (winnerIds.Contains(userId))
            {
                winnerIds.Remove(userId);
                evt.Winners = string.Join(",", winnerIds);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Winner removed successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "User is not a winner";
            }

            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements
                .Include(a => a.Club)
                .Include(a => a.CreatedByUser)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(announcements);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAnnouncement()
        {
            var clubs = await _context.Clubs
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var model = new AdminAnnouncementViewModel
            {
                AvailableClubs = clubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name
                }).ToList()
            };

            // Add system-wide option
            model.AvailableClubs.Insert(0, new SelectListItem { Value = "0", Text = "System-wide Announcement" });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(AdminAnnouncementViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var announcement = new Announcement
                {
                    Title = model.Title,
                    Message = model.Message,
                    Content = model.Content,
                    CreatedAt = DateTime.Now,
                    Status = "Active",
                    CreatedByUserId = currentUser.Id
                };

                // If club-specific announcement
                if (model.ClubId > 0)
                {
                    announcement.ClubId = model.ClubId;
                }

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Announcement created successfully";
                return RedirectToAction(nameof(Announcements));
            }

            // If we got this far, something failed, redisplay form
            var clubs = await _context.Clubs
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            model.AvailableClubs = clubs.Select(c => new SelectListItem
            {
                Value = c.ClubId.ToString(),
                Text = c.Name,
                Selected = c.ClubId == model.ClubId
            }).ToList();

            // Add system-wide option
            model.AvailableClubs.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "System-wide Announcement",
                Selected = model.ClubId == 0
            });

            return View(model);
        }

        public async Task<IActionResult> EditAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            var model = new AdminAnnouncementViewModel
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Message = announcement.Message,
                Content = announcement.Content,
                ClubId = announcement.ClubId ?? 0,
                IsActive = announcement.Status == "Active"
            };

            var clubs = await _context.Clubs
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            model.AvailableClubs = clubs.Select(c => new SelectListItem
            {
                Value = c.ClubId.ToString(),
                Text = c.Name,
                Selected = c.ClubId == model.ClubId
            }).ToList();

            // Add system-wide option
            model.AvailableClubs.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "System-wide Announcement",
                Selected = model.ClubId == 0
            });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAnnouncement(AdminAnnouncementViewModel model)
        {
            if (ModelState.IsValid)
            {
                var announcement = await _context.Announcements.FindAsync(model.Id);
                if (announcement == null)
                {
                    return NotFound();
                }

                announcement.Title = model.Title;
                announcement.Message = model.Message;
                announcement.Content = model.Content;
                announcement.Status = model.IsActive ? "Active" : "Inactive";

                // Update club association
                if (model.ClubId > 0)
                {
                    announcement.ClubId = model.ClubId;
                }
                else
                {
                    announcement.ClubId = null;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Announcement updated successfully";
                return RedirectToAction(nameof(Announcements));
            }

            // If we got this far, something failed, redisplay form
            var clubs = await _context.Clubs
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            model.AvailableClubs = clubs.Select(c => new SelectListItem
            {
                Value = c.ClubId.ToString(),
                Text = c.Name,
                Selected = c.ClubId == model.ClubId
            }).ToList();

            // Add system-wide option
            model.AvailableClubs.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "System-wide Announcement",
                Selected = model.ClubId == 0
            });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement deleted successfully";
            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAnnouncementStatus(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            announcement.Status = announcement.Status == "Active"
                ? "Inactive"
                : "Active";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Announcement {(announcement.Status == "Active" ? "activated" : "deactivated")} successfully";
            return RedirectToAction(nameof(Announcements));
        }
    }
}

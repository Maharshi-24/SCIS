using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using SCIS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCIS.Services
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public List<SelectListItem> GetRolesForUserType(string userType)
        {
            var roles = new List<SelectListItem>();

            switch (userType)
            {
                case Constants.UserTypes.Admin:
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.SystemAdmin, Text = "System Administrator" });
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.ContentAdmin, Text = "Content Administrator" });
                    break;
                case Constants.UserTypes.ClubManager:
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.ClubPresident, Text = "Club President" });
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.ClubSecretary, Text = "Club Secretary" });
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.ClubTreasurer, Text = "Club Treasurer" });
                    break;
                case Constants.UserTypes.User:
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.Student, Text = "Student" });
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.Faculty, Text = "Faculty" });
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.Staff, Text = "Staff" });
                    roles.Add(new SelectListItem { Value = Constants.UserRoles.Alumni, Text = "Alumni" });
                    break;
                default:
                    break;
            }

            return roles;
        }

        public async Task<bool> CreateUserAsync(ApplicationUser user, string password, string role)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                return true;
            }
            return false;
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<bool> AddToRoleAsync(ApplicationUser user, string role)
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> RemoveFromRoleAsync(ApplicationUser user, string role)
        {
            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result.Succeeded;
        }
    }
}

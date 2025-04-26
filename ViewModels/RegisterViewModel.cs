
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using SCIS.Models;

namespace SCIS.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "User Type")]
        public string UserType { get; set; } = "User";

        [Display(Name = "User Role")]
        public string UserRole { get; set; } = "Student";

        // For dropdown lists in the view
        public List<SelectListItem> AvailableUserTypes { get; set; }
        public List<SelectListItem> AvailableUserRoles { get; set; }

        public RegisterViewModel()
        {
            // Initialize with default user types
            AvailableUserTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = Constants.UserTypes.Admin, Text = "Administrator" },
                new SelectListItem { Value = Constants.UserTypes.ClubManager, Text = "Club Manager" },
                new SelectListItem { Value = Constants.UserTypes.User, Text = "Regular User" }
            };

            // Initialize with empty roles list (will be populated based on selected user type)
            AvailableUserRoles = new List<SelectListItem>();
        }
    }
}

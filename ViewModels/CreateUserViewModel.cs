using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace SCIS.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "User type is required")]
        [Display(Name = "User Type")]
        public string UserType { get; set; }

        [Required(ErrorMessage = "User role is required")]
        [Display(Name = "User Role")]
        public string UserRole { get; set; }

        // For dropdown lists
        public List<SelectListItem> AvailableUserTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableUserRoles { get; set; } = new List<SelectListItem>();
    }
}

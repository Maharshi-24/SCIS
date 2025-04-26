using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SCIS.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "User type is required")]
        [Display(Name = "User Type")]
        public string UserType { get; set; }

        [Required(ErrorMessage = "User role is required")]
        [Display(Name = "User Role")]
        public string UserRole { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // For dropdown lists in the view
        public List<SelectListItem> AvailableUserTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableUserRoles { get; set; } = new List<SelectListItem>();
    }
}

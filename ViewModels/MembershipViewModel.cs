using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SCIS.ViewModels
{
    public class MembershipViewModel
    {
        public int MembershipId { get; set; }

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Club")]
        public int ClubId { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        // For dropdown selection
        public List<SelectListItem> AvailableUsers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableClubs { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Pending", Text = "Pending" },
            new SelectListItem { Value = "Approved", Text = "Approved" },
            new SelectListItem { Value = "Rejected", Text = "Rejected" }
        };

        // For display purposes
        public string UserName { get; set; }
        public string ClubName { get; set; }
    }
}

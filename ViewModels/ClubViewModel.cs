using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SCIS.ViewModels
{
    public class ClubViewModel
    {
        public int? ClubId { get; set; }

        [Required(ErrorMessage = "Club name is required")]
        [StringLength(100, ErrorMessage = "Club name cannot exceed 100 characters")]
        [Display(Name = "Club Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Club President")]
        public string PresidentId { get; set; }

        public string CreatedByAdminId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // For dropdown selection
        public List<SelectListItem> AvailablePresidents { get; set; } = new List<SelectListItem>();

        // For display purposes
        public string PresidentName { get; set; }
        public string CreatedByAdminName { get; set; }
        public int MemberCount { get; set; }
        public int EventCount { get; set; }
    }
}

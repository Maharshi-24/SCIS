using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SCIS.ViewModels
{
    public class EventViewModel
    {
        public int? EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters")]
        [Display(Name = "Event Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Club is required")]
        [Display(Name = "Club")]
        public int ClubId { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [Display(Name = "Event Date")]
        [DataType(DataType.DateTime)]
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "Seat limit is required")]
        [Range(1, 1000, ErrorMessage = "Seat limit must be between 1 and 1000")]
        [Display(Name = "Seat Limit")]
        public int SeatLimit { get; set; } = 50;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        // For dropdown selection
        public List<SelectListItem> AvailableClubs { get; set; } = new List<SelectListItem>();

        // For display purposes
        public string ClubName { get; set; }
        public string RegisteredUsers { get; set; } = string.Empty;
        public string Winners { get; set; } = string.Empty;
        public int RegisteredCount { get; set; }
        public bool IsRegistered { get; set; }
        public bool IsPast => EventDate < DateTime.Now;
    }
}

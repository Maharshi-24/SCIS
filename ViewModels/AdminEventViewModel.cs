using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SCIS.Models;

namespace SCIS.ViewModels
{
    public class AdminEventViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Event Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime EventDate { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }

        [Display(Name = "Max Attendees")]
        public int MaxAttendees { get; set; }

        [Display(Name = "Registrations")]
        public int RegistrationCount { get; set; }

        [Display(Name = "Club")]
        public string ClubName { get; set; }

        public int ClubId { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; }

        public bool IsPast { get; set; }
    }

    public class AdminEventDetailsViewModel
    {
        public Event Event { get; set; } = new Event();
        public List<ApplicationUser> RegisteredUsers { get; set; } = new List<ApplicationUser>();
        public List<ApplicationUser> Winners { get; set; } = new List<ApplicationUser>();
    }
}

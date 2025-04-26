using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SCIS.Models;

namespace SCIS.ViewModels
{
    public class AdminClubViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Club Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Created")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; }

        [Display(Name = "President")]
        public string PresidentName { get; set; }

        [Display(Name = "Created By")]
        public string CreatedByAdminName { get; set; }

        [Display(Name = "Members")]
        public int MemberCount { get; set; }

        [Display(Name = "Events")]
        public int EventCount { get; set; }
    }

    public class AdminClubCreateViewModel
    {
        [Required(ErrorMessage = "Club name is required")]
        [StringLength(100, ErrorMessage = "Club name cannot exceed 100 characters")]
        [Display(Name = "Club Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "President")]
        public string PresidentId { get; set; }

        [Display(Name = "President Email")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string PresidentEmail { get; set; }
    }

    public class AdminClubEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Club name is required")]
        [StringLength(100, ErrorMessage = "Club name cannot exceed 100 characters")]
        [Display(Name = "Club Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "President")]
        public string PresidentId { get; set; }

        // For dropdown list
        public List<SelectListItem> AvailablePresidents { get; set; } = new List<SelectListItem>();
    }

    public class AdminClubDetailsViewModel
    {
        public Club Club { get; set; } = new Club();
        public List<Membership> Members { get; set; } = new List<Membership>();
        public List<Membership> PendingRequests { get; set; } = new List<Membership>();
        public List<Event> Events { get; set; } = new List<Event>();
    }
}

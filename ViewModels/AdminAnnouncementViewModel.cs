using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SCIS.Models;

namespace SCIS.ViewModels
{
    public class AdminAnnouncementViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [StringLength(200, ErrorMessage = "Message cannot exceed 200 characters")]
        [Display(Name = "Short Message")]
        public string Message { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Full Content")]
        public string Content { get; set; }

        [Display(Name = "Club")]
        public int ClubId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // For dropdown list
        public List<SelectListItem> AvailableClubs { get; set; } = new List<SelectListItem>();
    }
}

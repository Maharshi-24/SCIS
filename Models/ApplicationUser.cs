
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SCIS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string UserType { get; set; } // Admin, ClubManager, User

        [StringLength(50)]
        public string UserRole { get; set; } // Specific role within user type

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}

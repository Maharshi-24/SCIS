
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCIS.Models
{
    public class Club
    {
        [Key]
        public int ClubId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PresidentId { get; set; } = string.Empty;
        public string CreatedByAdminId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        [ForeignKey("PresidentId")]
        public virtual ApplicationUser? President { get; set; }

        [ForeignKey("CreatedByAdminId")]
        public virtual ApplicationUser? CreatedByAdmin { get; set; }

        public virtual ICollection<Membership>? Memberships { get; set; }
        public virtual ICollection<Event>? Events { get; set; }

        // Alias for Id for backward compatibility
        [NotMapped]
        public int Id {
            get { return ClubId; }
            set { ClubId = value; }
        }
    }
}

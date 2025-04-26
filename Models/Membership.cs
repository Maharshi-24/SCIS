
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCIS.Models
{
    public class Membership
    {
        [Key]
        public int MembershipId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ClubId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? RequestedAt { get; set; } = DateTime.Now;
        public DateTime? JoinedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club? Club { get; set; }

        // Alias for Id for backward compatibility
        [NotMapped]
        public int Id {
            get { return MembershipId; }
            set { MembershipId = value; }
        }
    }
}

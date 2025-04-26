
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCIS.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? ClubId { get; set; }
        public string Status { get; set; } = "Active";
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club? Club { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        // Alias for Id for backward compatibility
        [NotMapped]
        public int Id {
            get { return AnnouncementId; }
            set { AnnouncementId = value; }
        }
    }
}

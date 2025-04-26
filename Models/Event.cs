
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCIS.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ClubId { get; set; }
        public DateTime EventDate { get; set; }
        public int SeatLimit { get; set; }
        public string Location { get; set; } = string.Empty;
        public string RegisteredUsers { get; set; } = string.Empty; // Comma separated UserIds
        public string Winners { get; set; } = string.Empty; // Comma separated UserIds
        public bool IsActive { get; set; } = true;

        [ForeignKey("ClubId")]
        public virtual Club? Club { get; set; }

        [NotMapped]
        public List<ApplicationUser>? RegisteredUsersList { get; set; }

        [NotMapped]
        public List<ApplicationUser>? WinnersList { get; set; }

        // Alias for MaxAttendees for backward compatibility
        [NotMapped]
        public int MaxAttendees {
            get { return SeatLimit; }
            set { SeatLimit = value; }
        }

        // Alias for Id for backward compatibility
        [NotMapped]
        public int Id {
            get { return EventId; }
            set { EventId = value; }
        }
    }
}

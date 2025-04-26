using SCIS.Models;
using System.Collections.Generic;

namespace SCIS.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Membership> Memberships { get; set; }
        public List<Event> Events { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCIS.Data;
using SCIS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    [Authorize(Roles = "ClubPresident,ClubSecretary,ClubTreasurer")]
    public class ClubManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClubManagerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.FullName = currentUser?.FullName;
            ViewBag.UserType = currentUser?.UserType;
            ViewBag.UserRole = currentUser?.UserRole;
            
            // Get clubs managed by this user
            var managedClubs = await _context.Clubs
                .Where(c => c.PresidentId == currentUser.Id)
                .ToListAsync();
            
            // Get club membership count
            var membershipCounts = await _context.Memberships
                .Where(m => managedClubs.Select(c => c.ClubId).Contains(m.ClubId))
                .GroupBy(m => m.ClubId)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            ViewBag.ManagedClubs = managedClubs.Count;
            ViewBag.TotalMembers = membershipCounts.Sum(m => m.Count);
            
            // Get upcoming events
            var upcomingEvents = await _context.Events
                .Where(e => managedClubs.Select(c => c.ClubId).Contains(e.ClubId) && e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .ToListAsync();
            
            ViewBag.UpcomingEvents = upcomingEvents.Count;
            
            return View(managedClubs);
        }
        
        public async Task<IActionResult> Members(int clubId)
        {
            var club = await _context.Clubs.FindAsync(clubId);
            if (club == null)
            {
                return NotFound();
            }
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }
            
            var memberships = await _context.Memberships
                .Where(m => m.ClubId == clubId)
                .ToListAsync();
            
            var memberIds = memberships.Select(m => m.UserId).ToList();
            var members = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();
            
            ViewBag.Club = club;
            
            return View(members);
        }
        
        public async Task<IActionResult> Events(int clubId)
        {
            var club = await _context.Clubs.FindAsync(clubId);
            if (club == null)
            {
                return NotFound();
            }
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }
            
            var events = await _context.Events
                .Where(e => e.ClubId == clubId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            
            ViewBag.Club = club;
            
            return View(events);
        }
    }
}

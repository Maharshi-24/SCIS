using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCIS.Data;
using SCIS.Models;
using SCIS.ViewModels;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.FullName = currentUser?.FullName;
            ViewBag.UserType = currentUser?.UserType;
            ViewBag.UserRole = currentUser?.UserRole;

            // Get user's club memberships
            var memberships = await _context.Memberships
                .Where(m => m.UserId == currentUser.Id)
                .ToListAsync();

            var clubIds = memberships.Select(m => m.ClubId).ToList();
            var clubs = await _context.Clubs
                .Where(c => clubIds.Contains(c.ClubId))
                .ToListAsync();

            // Get upcoming events for user's clubs
            var upcomingEvents = await _context.Events
                .Where(e => clubIds.Contains(e.ClubId) && e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .ToListAsync();

            // Get recent announcements
            var announcements = await _context.Announcements
                .Where(a => a.ClubId.HasValue && clubIds.Contains(a.ClubId.Value))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.ClubCount = clubs.Count;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.Announcements = announcements;

            return View(clubs);
        }

        public async Task<IActionResult> Clubs()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            var clubs = await _context.Clubs.ToListAsync();
            var clubViewModels = new List<ClubViewModel>();

            foreach (var club in clubs)
            {
                var president = await _userManager.FindByIdAsync(club.PresidentId);
                var admin = await _userManager.FindByIdAsync(club.CreatedByAdminId);
                var memberCount = await _context.Memberships.CountAsync(m => m.ClubId == club.ClubId);
                var eventCount = await _context.Events.CountAsync(e => e.ClubId == club.ClubId);

                // Check if user is a member
                var isMember = await _context.Memberships
                    .AnyAsync(m => m.ClubId == club.ClubId && m.UserId == currentUser.Id && m.Status == "Approved");

                var hasPendingRequest = await _context.Memberships
                    .AnyAsync(m => m.ClubId == club.ClubId && m.UserId == currentUser.Id && m.Status == "Pending");

                clubViewModels.Add(new ClubViewModel
                {
                    ClubId = club.ClubId,
                    Name = club.Name,
                    Description = club.Description,
                    PresidentId = club.PresidentId,
                    CreatedByAdminId = club.CreatedByAdminId,
                    CreatedAt = club.CreatedAt,
                    PresidentName = president?.FullName ?? "Unknown",
                    CreatedByAdminName = admin?.FullName ?? "Unknown",
                    MemberCount = memberCount,
                    EventCount = eventCount
                });
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = currentUser.Id;

            return View(clubViewModels);
        }

        public async Task<IActionResult> Events()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Get user's club memberships
            var memberships = await _context.Memberships
                .Where(m => m.UserId == currentUser.Id && m.Status == "Approved")
                .ToListAsync();

            var clubIds = memberships.Select(m => m.ClubId).ToList();

            var events = await _context.Events
                .Where(e => e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var eventViewModels = new List<EventViewModel>();

            foreach (var evt in events)
            {
                var club = await _context.Clubs.FindAsync(evt.ClubId);
                var registeredCount = !string.IsNullOrEmpty(evt.RegisteredUsers)
                    ? evt.RegisteredUsers.Split(',').Length
                    : 0;

                var isRegistered = !string.IsNullOrEmpty(evt.RegisteredUsers) &&
                                  evt.RegisteredUsers.Split(',').Contains(currentUser.Id);

                // Check if user is a member of the club
                var isMember = clubIds.Contains(evt.ClubId);

                eventViewModels.Add(new EventViewModel
                {
                    EventId = evt.EventId,
                    Name = evt.Name,
                    Description = evt.Description,
                    ClubId = evt.ClubId,
                    EventDate = evt.EventDate,
                    SeatLimit = evt.SeatLimit,
                    ClubName = club?.Name ?? "Unknown",
                    RegisteredUsers = evt.RegisteredUsers,
                    Winners = evt.Winners,
                    RegisteredCount = registeredCount,
                    IsRegistered = isRegistered
                });
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.ClubIds = clubIds;

            return View(eventViewModels);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCIS.Data;
using SCIS.Models;
using SCIS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCIS.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Get user's club memberships
            var memberships = await _context.Memberships
                .Where(m => m.UserId == currentUser.Id && m.Status == "Approved")
                .ToListAsync();

            var clubIds = memberships.Select(m => m.ClubId).ToList();

            // If admin, show all events, otherwise show events from user's clubs
            IQueryable<Event> eventsQuery;
            if (isAdmin)
            {
                eventsQuery = _context.Events;
            }
            else
            {
                eventsQuery = _context.Events.Where(e => clubIds.Contains(e.ClubId));
            }

            var events = await eventsQuery.OrderByDescending(e => e.EventDate).ToListAsync();
            var eventViewModels = new List<EventViewModel>();

            foreach (var evt in events)
            {
                var club = await _context.Clubs.FindAsync(evt.ClubId);
                var registeredCount = !string.IsNullOrEmpty(evt.RegisteredUsers)
                    ? evt.RegisteredUsers.Split(',').Length
                    : 0;

                var isRegistered = !string.IsNullOrEmpty(evt.RegisteredUsers) &&
                                  evt.RegisteredUsers.Split(',').Contains(currentUser.Id);

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

            // Get clubs where user is president
            var managedClubs = await _context.Clubs
                .Where(c => c.PresidentId == currentUser.Id)
                .ToListAsync();

            ViewBag.IsClubPresident = managedClubs.Any();
            ViewBag.ManagedClubIds = managedClubs.Select(c => c.ClubId).ToList();

            return View(eventViewModels);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs.FindAsync(evt.ClubId);
            var registeredUserIds = !string.IsNullOrEmpty(evt.RegisteredUsers)
                ? evt.RegisteredUsers.Split(',').ToList()
                : new List<string>();

            var winnerIds = !string.IsNullOrEmpty(evt.Winners)
                ? evt.Winners.Split(',').ToList()
                : new List<string>();

            var registeredUsers = new List<ApplicationUser>();
            var winners = new List<ApplicationUser>();

            foreach (var userId in registeredUserIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    registeredUsers.Add(user);
                }
            }

            foreach (var userId in winnerIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    winners.Add(user);
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isRegistered = registeredUserIds.Contains(currentUser.Id);
            var isWinner = winnerIds.Contains(currentUser.Id);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);
            var isClubPresident = club?.PresidentId == currentUser.Id;
            var isMember = await _context.Memberships
                .AnyAsync(m => m.ClubId == evt.ClubId && m.UserId == currentUser.Id && m.Status == "Approved");

            var eventViewModel = new EventViewModel
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
                RegisteredCount = registeredUsers.Count,
                IsRegistered = isRegistered
            };

            ViewBag.RegisteredUsers = registeredUsers;
            ViewBag.Winners = winners;
            ViewBag.IsRegistered = isRegistered;
            ViewBag.IsWinner = isWinner;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsClubPresident = isClubPresident;
            ViewBag.IsMember = isMember;
            ViewBag.IsPast = evt.EventDate < DateTime.Now;
            ViewBag.HasAvailableSeats = registeredUsers.Count < evt.SeatLimit;
            ViewBag.Club = club;

            return View(eventViewModel);
        }

        // GET: Event/Create
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            var eventViewModel = new EventViewModel();

            // If admin, show all clubs, otherwise show only clubs where user is president
            if (isAdmin)
            {
                var allClubs = await _context.Clubs.ToListAsync();
                eventViewModel.AvailableClubs = allClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name
                }).ToList();
            }
            else
            {
                var managedClubs = await _context.Clubs
                    .Where(c => c.PresidentId == currentUser.Id)
                    .ToListAsync();

                eventViewModel.AvailableClubs = managedClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name
                }).ToList();
            }

            return View(eventViewModel);
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Create(EventViewModel eventViewModel)
        {
            try
            {
                // Log the form submission for debugging
                Console.WriteLine($"Create Event form submitted: Name={eventViewModel.Name}, ClubId={eventViewModel.ClubId}, EventDate={eventViewModel.EventDate}, Location={eventViewModel.Location}");

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                              await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

                // Verify user has permission to create event for this club
                if (!isAdmin)
                {
                    var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ClubId == eventViewModel.ClubId);
                    if (club == null || club.PresidentId != currentUser.Id)
                    {
                        return Forbid();
                    }
                }

                // Ensure the event date is valid
                if (eventViewModel.EventDate < DateTime.Now)
                {
                    ModelState.AddModelError("EventDate", "Event date must be in the future");
                }

                if (ModelState.IsValid)
                {
                    var evt = new Event
                    {
                        Name = eventViewModel.Name,
                        Description = eventViewModel.Description,
                        ClubId = eventViewModel.ClubId,
                        EventDate = eventViewModel.EventDate,
                        SeatLimit = eventViewModel.SeatLimit,
                        Location = eventViewModel.Location ?? string.Empty,
                        RegisteredUsers = string.Empty,
                        Winners = string.Empty,
                        IsActive = true
                    };

                    _context.Events.Add(evt);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Event created successfully";
                    return RedirectToAction(nameof(Index));
                }

                // Log validation errors for debugging
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"Validation error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error creating event: {ex.Message}");
                ModelState.AddModelError(string.Empty, $"Error creating event: {ex.Message}");
            }

            // If we got this far, something failed, redisplay form
            var currentUser2 = await _userManager.GetUserAsync(User);
            var isAdmin2 = await _userManager.IsInRoleAsync(currentUser2, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser2, Constants.UserRoles.ContentAdmin);

            if (isAdmin2)
            {
                var allClubs = await _context.Clubs.ToListAsync();
                eventViewModel.AvailableClubs = allClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name,
                    Selected = c.ClubId == eventViewModel.ClubId
                }).ToList();
            }
            else
            {
                var managedClubs = await _context.Clubs
                    .Where(c => c.PresidentId == currentUser2.Id)
                    .ToListAsync();

                eventViewModel.AvailableClubs = managedClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name,
                    Selected = c.ClubId == eventViewModel.ClubId
                }).ToList();
            }

            return View(eventViewModel);
        }

        // GET: Event/Edit/5
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Edit(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Verify user has permission to edit this event
            if (!isAdmin)
            {
                var club = await _context.Clubs.FindAsync(evt.ClubId);
                if (club == null || club.PresidentId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            var eventViewModel = new EventViewModel
            {
                EventId = evt.EventId,
                Name = evt.Name,
                Description = evt.Description,
                ClubId = evt.ClubId,
                EventDate = evt.EventDate,
                SeatLimit = evt.SeatLimit
            };

            // If admin, show all clubs, otherwise show only clubs where user is president
            if (isAdmin)
            {
                var allClubs = await _context.Clubs.ToListAsync();
                eventViewModel.AvailableClubs = allClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name,
                    Selected = c.ClubId == evt.ClubId
                }).ToList();
            }
            else
            {
                var managedClubs = await _context.Clubs
                    .Where(c => c.PresidentId == currentUser.Id)
                    .ToListAsync();

                eventViewModel.AvailableClubs = managedClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name,
                    Selected = c.ClubId == evt.ClubId
                }).ToList();
            }

            return View(eventViewModel);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Edit(int id, EventViewModel eventViewModel)
        {
            if (id != eventViewModel.EventId)
            {
                return NotFound();
            }

            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Verify user has permission to edit this event
            if (!isAdmin)
            {
                var club = await _context.Clubs.FindAsync(evt.ClubId);
                if (club == null || club.PresidentId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    evt.Name = eventViewModel.Name;
                    evt.Description = eventViewModel.Description;
                    evt.EventDate = eventViewModel.EventDate;

                    // Only allow changing club if admin
                    if (isAdmin)
                    {
                        evt.ClubId = eventViewModel.ClubId;
                    }

                    // Only allow reducing seat limit if it doesn't conflict with registrations
                    var registeredCount = !string.IsNullOrEmpty(evt.RegisteredUsers)
                        ? evt.RegisteredUsers.Split(',').Length
                        : 0;

                    if (eventViewModel.SeatLimit < registeredCount)
                    {
                        ModelState.AddModelError("SeatLimit", "Cannot reduce seat limit below the number of registered users.");

                        // Repopulate available clubs
                        if (isAdmin)
                        {
                            var allClubs = await _context.Clubs.ToListAsync();
                            eventViewModel.AvailableClubs = allClubs.Select(c => new SelectListItem
                            {
                                Value = c.ClubId.ToString(),
                                Text = c.Name,
                                Selected = c.ClubId == evt.ClubId
                            }).ToList();
                        }
                        else
                        {
                            var managedClubs = await _context.Clubs
                                .Where(c => c.PresidentId == currentUser.Id)
                                .ToListAsync();

                            eventViewModel.AvailableClubs = managedClubs.Select(c => new SelectListItem
                            {
                                Value = c.ClubId.ToString(),
                                Text = c.Name,
                                Selected = c.ClubId == evt.ClubId
                            }).ToList();
                        }

                        return View(eventViewModel);
                    }

                    evt.SeatLimit = eventViewModel.SeatLimit;

                    _context.Update(evt);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(evt.EventId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = evt.EventId });
            }

            // If we got this far, something failed, redisplay form
            if (isAdmin)
            {
                var allClubs = await _context.Clubs.ToListAsync();
                eventViewModel.AvailableClubs = allClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name,
                    Selected = c.ClubId == evt.ClubId
                }).ToList();
            }
            else
            {
                var managedClubs = await _context.Clubs
                    .Where(c => c.PresidentId == currentUser.Id)
                    .ToListAsync();

                eventViewModel.AvailableClubs = managedClubs.Select(c => new SelectListItem
                {
                    Value = c.ClubId.ToString(),
                    Text = c.Name,
                    Selected = c.ClubId == evt.ClubId
                }).ToList();
            }

            return View(eventViewModel);
        }

        // GET: Event/Delete/5
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Delete(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Verify user has permission to delete this event
            if (!isAdmin)
            {
                var eventClub = await _context.Clubs.FindAsync(evt.ClubId);
                if (eventClub == null || eventClub.PresidentId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            var club = await _context.Clubs.FindAsync(evt.ClubId);
            var registeredCount = !string.IsNullOrEmpty(evt.RegisteredUsers)
                ? evt.RegisteredUsers.Split(',').Length
                : 0;

            var eventViewModel = new EventViewModel
            {
                EventId = evt.EventId,
                Name = evt.Name,
                Description = evt.Description,
                ClubId = evt.ClubId,
                EventDate = evt.EventDate,
                SeatLimit = evt.SeatLimit,
                ClubName = club?.Name ?? "Unknown",
                RegisteredCount = registeredCount
            };

            return View(eventViewModel);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Verify user has permission to delete this event
            if (!isAdmin)
            {
                var club = await _context.Clubs.FindAsync(evt.ClubId);
                if (club == null || club.PresidentId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Event/Register/5
        [Authorize]
        public async Task<IActionResult> Register(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            // Check if event is in the past
            if (evt.EventDate < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Cannot register for past events.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if user is a member of the club
            var isMember = await _context.Memberships
                .AnyAsync(m => m.ClubId == evt.ClubId && m.UserId == currentUser.Id && m.Status == "Approved");

            if (!isMember)
            {
                TempData["ErrorMessage"] = "You must be a member of the club to register for this event.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if already registered
            var registeredUserIds = !string.IsNullOrEmpty(evt.RegisteredUsers)
                ? evt.RegisteredUsers.Split(',').ToList()
                : new List<string>();

            if (registeredUserIds.Contains(currentUser.Id))
            {
                TempData["Message"] = "You are already registered for this event.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if event is full
            if (registeredUserIds.Count >= evt.SeatLimit)
            {
                TempData["ErrorMessage"] = "This event is full.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Add user to registered users
            if (string.IsNullOrEmpty(evt.RegisteredUsers))
            {
                evt.RegisteredUsers = currentUser.Id;
            }
            else
            {
                evt.RegisteredUsers += "," + currentUser.Id;
            }

            _context.Update(evt);
            await _context.SaveChangesAsync();

            TempData["Message"] = "You have successfully registered for this event.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Event/Unregister/5
        [Authorize]
        public async Task<IActionResult> Unregister(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            // Check if event is in the past
            if (evt.EventDate < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Cannot unregister from past events.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if registered
            var registeredUserIds = !string.IsNullOrEmpty(evt.RegisteredUsers)
                ? evt.RegisteredUsers.Split(',').ToList()
                : new List<string>();

            if (!registeredUserIds.Contains(currentUser.Id))
            {
                TempData["Message"] = "You are not registered for this event.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Remove user from registered users
            registeredUserIds.Remove(currentUser.Id);
            evt.RegisteredUsers = string.Join(",", registeredUserIds);

            _context.Update(evt);
            await _context.SaveChangesAsync();

            TempData["Message"] = "You have successfully unregistered from this event.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Event/SelectWinners/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> SelectWinners(int id, List<string> selectedWinners)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Verify user has permission to select winners
            if (!isAdmin)
            {
                var eventClub = await _context.Clubs.FindAsync(evt.ClubId);
                if (eventClub == null || eventClub.PresidentId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            // Check if event is in the past
            if (evt.EventDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Cannot select winners for future events.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Verify all selected winners are registered
            var registeredUserIds = !string.IsNullOrEmpty(evt.RegisteredUsers)
                ? evt.RegisteredUsers.Split(',').ToList()
                : new List<string>();

            foreach (var winnerId in selectedWinners)
            {
                if (!registeredUserIds.Contains(winnerId))
                {
                    TempData["ErrorMessage"] = "All winners must be registered for the event.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // Update winners
            evt.Winners = string.Join(",", selectedWinners);

            _context.Update(evt);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Winners have been successfully selected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}

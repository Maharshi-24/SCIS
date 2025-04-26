
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
    public class ClubController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClubController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Club
        public async Task<IActionResult> Index()
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
                var memberCount = await _context.Memberships.CountAsync(m => m.ClubId == club.Id);
                var eventCount = await _context.Events.CountAsync(e => e.ClubId == club.Id);

                clubViewModels.Add(new ClubViewModel
                {
                    ClubId = club.Id,
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

        // GET: Club/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var president = await _userManager.FindByIdAsync(club.PresidentId);
            var admin = await _userManager.FindByIdAsync(club.CreatedByAdminId);
            var memberCount = await _context.Memberships.CountAsync(m => m.ClubId == club.ClubId);
            var eventCount = await _context.Events.CountAsync(e => e.ClubId == club.ClubId);

            var clubViewModel = new ClubViewModel
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
            };

            // Get club members
            var memberships = await _context.Memberships
                .Where(m => m.ClubId == id && m.Status == "Approved")
                .ToListAsync();

            var memberIds = memberships.Select(m => m.UserId).ToList();
            var members = await _userManager.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            // Get upcoming events
            var upcomingEvents = await _context.Events
                .Where(e => e.ClubId == id && e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .ToListAsync();

            ViewBag.Members = members;
            ViewBag.UpcomingEvents = upcomingEvents;

            // Check if current user is a member
            var currentUser = await _userManager.GetUserAsync(User);
            var isMember = await _context.Memberships
                .AnyAsync(m => m.ClubId == id && m.UserId == currentUser.Id && m.Status == "Approved");
            var hasPendingRequest = await _context.Memberships
                .AnyAsync(m => m.ClubId == id && m.UserId == currentUser.Id && m.Status == "Pending");
            var isPresident = club.PresidentId == currentUser.Id;
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            ViewBag.IsMember = isMember;
            ViewBag.HasPendingRequest = hasPendingRequest;
            ViewBag.IsPresident = isPresident;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = currentUser.Id;

            return View(clubViewModel);
        }

        // GET: Club/Create
        [Authorize(Roles = "SystemAdmin,ContentAdmin")]
        public async Task<IActionResult> Create()
        {
            var clubViewModel = new ClubViewModel();

            // Get users who can be club presidents (ClubManager type)
            var clubManagers = await _userManager.GetUsersInRoleAsync(Constants.UserRoles.ClubPresident);
            clubViewModel.AvailablePresidents = clubManagers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})"
            }).ToList();

            return View(clubViewModel);
        }

        // POST: Club/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin")]
        public async Task<IActionResult> Create(ClubViewModel clubViewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var club = new Club
                {
                    Name = clubViewModel.Name,
                    Description = clubViewModel.Description,
                    PresidentId = clubViewModel.PresidentId,
                    CreatedByAdminId = currentUser.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Add(club);
                await _context.SaveChangesAsync();

                // Automatically add the president as a member
                var membership = new Membership
                {
                    UserId = clubViewModel.PresidentId,
                    ClubId = club.ClubId,
                    Status = "Approved"
                };

                _context.Add(membership);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            var clubManagers = await _userManager.GetUsersInRoleAsync(Constants.UserRoles.ClubPresident);
            clubViewModel.AvailablePresidents = clubManagers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})"
            }).ToList();

            return View(clubViewModel);
        }

        // GET: Club/Edit/5
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Edit(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Only admins or the club president can edit
            if (!isAdmin && club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }

            var clubViewModel = new ClubViewModel
            {
                ClubId = club.ClubId,
                Name = club.Name,
                Description = club.Description,
                PresidentId = club.PresidentId,
                CreatedByAdminId = club.CreatedByAdminId,
                CreatedAt = club.CreatedAt
            };

            // Get users who can be club presidents (ClubManager type)
            var clubManagers = await _userManager.GetUsersInRoleAsync(Constants.UserRoles.ClubPresident);
            clubViewModel.AvailablePresidents = clubManagers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == club.PresidentId
            }).ToList();

            return View(clubViewModel);
        }

        // POST: Club/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Edit(int id, ClubViewModel clubViewModel)
        {
            if (id != clubViewModel.ClubId)
            {
                return NotFound();
            }

            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Only admins or the club president can edit
            if (!isAdmin && club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    club.Name = clubViewModel.Name;
                    club.Description = clubViewModel.Description;

                    // Only admins can change the president
                    if (isAdmin && club.PresidentId != clubViewModel.PresidentId)
                    {
                        // Add new president as member if not already
                        var isMember = await _context.Memberships
                            .AnyAsync(m => m.ClubId == id && m.UserId == clubViewModel.PresidentId);

                        if (!isMember)
                        {
                            var membership = new Membership
                            {
                                UserId = clubViewModel.PresidentId,
                                ClubId = club.ClubId,
                                Status = "Approved"
                            };

                            _context.Add(membership);
                        }

                        club.PresidentId = clubViewModel.PresidentId;
                    }

                    _context.Update(club);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClubExists(club.ClubId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = club.ClubId });
            }

            // If we got this far, something failed, redisplay form
            var clubManagers = await _userManager.GetUsersInRoleAsync(Constants.UserRoles.ClubPresident);
            clubViewModel.AvailablePresidents = clubManagers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == club.PresidentId
            }).ToList();

            return View(clubViewModel);
        }

        // GET: Club/Delete/5
        [Authorize(Roles = "SystemAdmin,ContentAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var president = await _userManager.FindByIdAsync(club.PresidentId);
            var admin = await _userManager.FindByIdAsync(club.CreatedByAdminId);

            var clubViewModel = new ClubViewModel
            {
                ClubId = club.ClubId,
                Name = club.Name,
                Description = club.Description,
                PresidentId = club.PresidentId,
                CreatedByAdminId = club.CreatedByAdminId,
                CreatedAt = club.CreatedAt,
                PresidentName = president?.FullName ?? "Unknown",
                CreatedByAdminName = admin?.FullName ?? "Unknown"
            };

            return View(clubViewModel);
        }

        // POST: Club/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            // Delete related memberships
            var memberships = await _context.Memberships.Where(m => m.ClubId == id).ToListAsync();
            _context.Memberships.RemoveRange(memberships);

            // Delete related events
            var events = await _context.Events.Where(e => e.ClubId == id).ToListAsync();
            _context.Events.RemoveRange(events);

            // Delete related announcements
            var announcements = await _context.Announcements.Where(a => a.ClubId == id).ToListAsync();
            _context.Announcements.RemoveRange(announcements);

            // Delete the club
            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Club/Join/5
        [Authorize]
        public async Task<IActionResult> Join(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Check if already a member or has pending request
            var existingMembership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.ClubId == id && m.UserId == currentUser.Id);

            if (existingMembership != null)
            {
                if (existingMembership.Status == "Approved")
                {
                    TempData["Message"] = "You are already a member of this club.";
                }
                else if (existingMembership.Status == "Pending")
                {
                    TempData["Message"] = "Your membership request is pending approval.";
                }
                else if (existingMembership.Status == "Rejected")
                {
                    // Allow reapplying if previously rejected
                    existingMembership.Status = "Pending";
                    _context.Update(existingMembership);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Your membership request has been submitted and is pending approval.";
                }
            }
            else
            {
                // Create new membership request
                var membership = new Membership
                {
                    UserId = currentUser.Id,
                    ClubId = id,
                    Status = "Pending"
                };

                _context.Add(membership);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Your membership request has been submitted and is pending approval.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Club/Leave/5
        [Authorize]
        public async Task<IActionResult> Leave(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Cannot leave if you're the president
            if (club.PresidentId == currentUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot leave the club as you are the president. Please contact an administrator to assign a new president.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Find and remove membership
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.ClubId == id && m.UserId == currentUser.Id);

            if (membership != null)
            {
                _context.Memberships.Remove(membership);
                await _context.SaveChangesAsync();
                TempData["Message"] = "You have successfully left the club.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Club/Members/5
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> Members(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Only admins or the club president can view members
            if (!isAdmin && club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }

            // Get all memberships for this club
            var memberships = await _context.Memberships
                .Where(m => m.ClubId == id)
                .ToListAsync();

            var membershipViewModels = new List<MembershipViewModel>();

            foreach (var membership in memberships)
            {
                var user = await _userManager.FindByIdAsync(membership.UserId);

                membershipViewModels.Add(new MembershipViewModel
                {
                    MembershipId = membership.MembershipId,
                    UserId = membership.UserId,
                    ClubId = membership.ClubId,
                    Status = membership.Status,
                    UserName = user?.FullName ?? "Unknown",
                    ClubName = club.Name
                });
            }

            ViewBag.Club = club;
            ViewBag.IsPresident = club.PresidentId == currentUser.Id;
            ViewBag.IsAdmin = isAdmin;

            return View(membershipViewModels);
        }

        // POST: Club/ApproveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> ApproveMember(int membershipId)
        {
            var membership = await _context.Memberships.FindAsync(membershipId);
            if (membership == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs.FindAsync(membership.ClubId);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Only admins or the club president can approve members
            if (!isAdmin && club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }

            membership.Status = "Approved";
            _context.Update(membership);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Members), new { id = membership.ClubId });
        }

        // POST: Club/RejectMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> RejectMember(int membershipId)
        {
            var membership = await _context.Memberships.FindAsync(membershipId);
            if (membership == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs.FindAsync(membership.ClubId);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Only admins or the club president can reject members
            if (!isAdmin && club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }

            membership.Status = "Rejected";
            _context.Update(membership);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Members), new { id = membership.ClubId });
        }

        // POST: Club/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ContentAdmin,ClubPresident")]
        public async Task<IActionResult> RemoveMember(int membershipId)
        {
            var membership = await _context.Memberships.FindAsync(membershipId);
            if (membership == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs.FindAsync(membership.ClubId);
            if (club == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.SystemAdmin) ||
                          await _userManager.IsInRoleAsync(currentUser, Constants.UserRoles.ContentAdmin);

            // Only admins or the club president can remove members
            if (!isAdmin && club.PresidentId != currentUser.Id)
            {
                return Forbid();
            }

            // Cannot remove the president
            if (membership.UserId == club.PresidentId)
            {
                TempData["ErrorMessage"] = "Cannot remove the club president. Please assign a new president first.";
                return RedirectToAction(nameof(Members), new { id = membership.ClubId });
            }

            _context.Memberships.Remove(membership);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Members), new { id = membership.ClubId });
        }

        private bool ClubExists(int id)
        {
            return _context.Clubs.Any(e => e.ClubId == id);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Controllers
{
    //[Authorize]
    public class BadgesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BadgesController> _logger;

        public BadgesController(ApplicationDbContext context, ILogger<BadgesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var badges = await _context.Badges
                .Include(b => b.ClientBadges)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(badges);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Badge badge)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    badge.CreatedDate = DateTime.UtcNow;
                    _context.Badges.Add(badge);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Badge created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating badge");
                    ModelState.AddModelError("", "An error occurred while creating the badge.");
                }
            }

            return View(badge);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var badge = await _context.Badges.FindAsync(id);
            if (badge == null)
                return NotFound();

            return View(badge);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Badge badge)
        {
            if (id != badge.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(badge);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Badge updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating badge");
                    ModelState.AddModelError("", "An error occurred while updating the badge.");
                }
            }

            return View(badge);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var badge = await _context.Badges.FindAsync(id);
                if (badge == null)
                    return NotFound();

                _context.Badges.Remove(badge);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Badge deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting badge");
                TempData["Error"] = "An error occurred while deleting the badge.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> SeedDefaultBadges()
        {
            try
            {
                var defaultBadges = new List<Badge>
                {
                    new Badge { Name = "New User", Description = "Welcome to myPhotoBiz! Account created successfully", Type = BadgeType.Custom, Icon = "ti-user-plus", Color = "#007bff", IsActive = true },
                    new Badge { Name = "Contract Signed", Description = "Completed and signed a contract", Type = BadgeType.ContractSigned, Icon = "ti-file-check", Color = "#28a745", IsActive = true },
                    new Badge { Name = "Waiver Signed", Description = "Signed liability waiver", Type = BadgeType.WaiverSigned, Icon = "ti-shield-check", Color = "#17a2b8", IsActive = true },
                    new Badge { Name = "Payment Complete", Description = "Made full payment", Type = BadgeType.PaymentCompleted, Icon = "ti-credit-card", Color = "#ffc107", IsActive = true },
                    new Badge { Name = "First Session", Description = "Completed first photo shoot", Type = BadgeType.FirstSession, Icon = "ti-camera", Color = "#6610f2", IsActive = true },
                    new Badge { Name = "Returning Client", Description = "Booked second photo shoot", Type = BadgeType.ReturningClient, Icon = "ti-refresh", Color = "#e83e8c", IsActive = true },
                    new Badge { Name = "VIP Client", Description = "Premium client status", Type = BadgeType.VIPClient, Icon = "ti-star", Color = "#fd7e14", IsActive = true },
                    new Badge { Name = "Referral Source", Description = "Referred new clients", Type = BadgeType.ReferralSource, Icon = "ti-users", Color = "#20c997", IsActive = true }
                };

                foreach (var badge in defaultBadges)
                {
                    if (!await _context.Badges.AnyAsync(b => b.Name == badge.Name))
                    {
                        _context.Badges.Add(badge);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Default badges created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default badges");
                TempData["Error"] = "An error occurred while creating default badges.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AwardNewUserBadgeToExisting()
        {
            try
            {
                // Find the "New User" badge
                var newUserBadge = await _context.Badges
                    .FirstOrDefaultAsync(b => b.Name == "New User");

                if (newUserBadge == null)
                {
                    TempData["Error"] = "New User badge not found. Please seed default badges first.";
                    return RedirectToAction(nameof(Index));
                }

                // Get all client profiles who don't have this badge yet
                var clientProfilesWithoutBadge = await _context.ClientProfiles
                    .Include(c => c.ClientBadges)
                    .Where(c => !c.ClientBadges.Any(cb => cb.BadgeId == newUserBadge.Id))
                    .ToListAsync();

                if (!clientProfilesWithoutBadge.Any())
                {
                    TempData["Success"] = "All existing clients already have the New User badge!";
                    return RedirectToAction(nameof(Index));
                }

                // Award badge to all client profiles without it
                foreach (var clientProfile in clientProfilesWithoutBadge)
                {
                    var clientBadge = new ClientBadge
                    {
                        ClientProfileId = clientProfile.Id,
                        BadgeId = newUserBadge.Id,
                        EarnedDate = DateTime.UtcNow,
                        Notes = "Awarded to existing client"
                    };

                    _context.ClientBadges.Add(clientBadge);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Awarded New User badge to {clientProfilesWithoutBadge.Count} existing clients");
                TempData["Success"] = $"Successfully awarded New User badge to {clientProfilesWithoutBadge.Count} existing clients!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding New User badge to existing clients");
                TempData["Error"] = "An error occurred while awarding badges.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;

namespace MyPhotoBiz.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BadgesController : Controller
    {
        private readonly IBadgeService _badgeService;

        public BadgesController(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        public async Task<IActionResult> Index()
        {
            var badges = await _badgeService.GetAllBadgesAsync();
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
            if (!ModelState.IsValid)
                return View(badge);

            await _badgeService.CreateBadgeAsync(badge);
            TempData["Success"] = "Badge created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var badge = await _badgeService.GetBadgeByIdAsync(id);
            if (badge == null) return NotFound();

            return View(badge);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Badge badge)
        {
            if (id != badge.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(badge);

            await _badgeService.UpdateBadgeAsync(badge);
            TempData["Success"] = "Badge updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _badgeService.DeleteBadgeAsync(id);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Badge deleted successfully!"
                : "Badge not found.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedDefaultBadges()
        {
            var created = await _badgeService.SeedDefaultBadgesAsync();
            TempData["Success"] = created > 0
                ? $"Created {created} default badge(s)."
                : "Default badges already exist.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AwardNewUserBadgeToExisting()
        {
            try
            {
                var awarded = await _badgeService.AwardNewUserBadgeToExistingClientsAsync();
                TempData["Success"] = awarded > 0
                    ? $"Successfully awarded New User badge to {awarded} existing client(s)!"
                    : "All existing clients already have the New User badge!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

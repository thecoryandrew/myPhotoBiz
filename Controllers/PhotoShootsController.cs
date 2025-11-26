using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Models;
using MyPhotoBiz.Models.ViewModels;
using MyPhotoBiz.Services;
using MyPhotoBiz.DTOs;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class PhotoShootsController : Controller
    {
        private readonly IPhotoShootService _photoShootService;
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PhotoShootsController(
            IPhotoShootService photoShootService,
            IClientService clientService,
            UserManager<ApplicationUser> userManager)
        {
            _photoShootService = photoShootService;
            _clientService = clientService;
            _userManager = userManager;
        }

        // =====================================================
        // STANDARD CRUD
        // =====================================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var shoots = await _photoShootService.GetAllPhotoShootsAsync();
            return View(shoots);
        }

        public async Task<IActionResult> Details(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();

            // Clients may only view their own shoots
            if (User.IsInRole("Client"))
            {
                var userId = _userManager.GetUserId(User);
                var client = await _clientService.GetClientByUserIdAsync(userId!);
                if (client == null || shoot.ClientId != client.Id)
                    return Forbid();
            }

            return View(shoot);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? clientId)
        {
            var clients = await _clientService.GetAllClientsAsync();

            var model = new PhotoShootViewModel
            {
                ScheduledDate = DateTime.Today,
                ClientId = clientId ?? 0,
                Status = PhotoShootStatus.InProgress,
                Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.FirstName} {c.LastName}",
                    Selected = clientId.HasValue && c.Id == clientId.Value
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhotoShootViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientsDropdown(model);
                return View(model);
            }

            var shoot = new PhotoShoot
            {
                Title = model.Title!,
                ClientId = model.ClientId,
                PhotographerId = _userManager.GetUserId(User)!,
                ScheduledDate = model.ScheduledDate,
                DurationHours = model.DurationHours,
                Location = model.Location!,
                Status = model.Status,
                Price = model.Price,
                Notes = model.Notes!,
                CreatedDate = DateTime.Now
            };

            await _photoShootService.CreatePhotoShootAsync(shoot);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();

            var clients = await _clientService.GetAllClientsAsync();

            var model = new PhotoShootViewModel
            {
                Id = shoot.Id,
                Title = shoot.Title,
                ClientId = shoot.ClientId,
                ScheduledDate = shoot.ScheduledDate,
                DurationHours = shoot.DurationHours,
                Location = shoot.Location,
                Status = shoot.Status,
                Price = shoot.Price,
                Notes = shoot.Notes,
                Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.FirstName} {c.LastName}",
                    Selected = c.Id == shoot.ClientId
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PhotoShootViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientsDropdown(model);
                return View(model);
            }

            var shoot = await _photoShootService.GetPhotoShootByIdAsync(model.Id);
            if (shoot == null) return NotFound();

            shoot.Title = model.Title!;
            shoot.ClientId = model.ClientId;
            shoot.ScheduledDate = model.ScheduledDate;
            shoot.DurationHours = model.DurationHours;
            shoot.Location = model.Location!;
            shoot.Status = model.Status;
            shoot.Price = model.Price;
            shoot.Notes = model.Notes!;
            shoot.UpdatedDate = DateTime.Now;

            await _photoShootService.UpdatePhotoShootAsync(shoot);

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();
            return View(shoot);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _photoShootService.DeletePhotoShootAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyPhotoShoots()
        {
            var userId = _userManager.GetUserId(User);
            var client = await _clientService.GetClientByUserIdAsync(userId!);
            if (client == null) return NotFound();

            var shoots = await _photoShootService.GetPhotoShootsByClientIdAsync(client.Id);
            return View(shoots);
        }

        // Populate dropdown
        private async Task PopulateClientsDropdown(PhotoShootViewModel model)
        {
            var clients = await _clientService.GetAllClientsAsync();
            model.Clients = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.FirstName} {c.LastName}",
                Selected = c.Id == model.ClientId
            }).ToList();
        }

        // =====================================================
        // FULLCALENDAR API
        // =====================================================

        [Authorize(Roles = "Admin")]
        public IActionResult Calendar() => View();

        // ---- Get Events -------------------------------------
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEvents()
        {
            var shoots = await _photoShootService.GetAllPhotoShootsAsync();

            var events = shoots.Select(ps => new
            {
                id = ps.Id,
                title = ps.Title,
                start = ps.ScheduledDate.ToString("s"),
                end = ps.ScheduledDate.AddHours(ps.DurationHours).ToString("s"),
                extendedProps = new
                {
                    description = ps.Description,
                    location = ps.Location,
                    clientId = ps.ClientId,
                    photographerId = ps.PhotographerId,
                    price = ps.Price,
                    notes = ps.Notes,
                    status = ps.Status.ToString(),
                    durationHours = ps.DurationHours
                }
            });

            return Json(events);
        }

        // ---- Drag/Drop Update -------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Move(int id, DateTime start, DateTime end)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();

            shoot.ScheduledDate = start;
            shoot.DurationHours = (int)(end - start).TotalHours;
            shoot.UpdatedDate = DateTime.Now;

            await _photoShootService.UpdatePhotoShootAsync(shoot);

            return Json(new { success = true });
        }

        // ---- AJAX: Create -----------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAjax([FromBody] PhotoShootAjaxDto dto)
        {
            var shoot = new PhotoShoot
            {
                Title = dto.Title,
                Description = dto.Description,
                ScheduledDate = dto.ScheduledDate,
                DurationHours = dto.DurationHours,
                Location = dto.Location,
                Status = dto.Status,
                Price = dto.Price,
                Notes = dto.Notes,
                ClientId = dto.ClientId,
                PhotographerId = dto.PhotographerId,
                CreatedDate = DateTime.Now
            };

            await _photoShootService.CreatePhotoShootAsync(shoot);

            return Json(new { success = true });
        }

        // ---- AJAX: Update -----------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAjax([FromBody] PhotoShootAjaxDto dto)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(dto.Id);
            if (shoot == null) return NotFound();

            shoot.Title = dto.Title;
            shoot.Description = dto.Description;
            shoot.ScheduledDate = dto.ScheduledDate;
            shoot.DurationHours = dto.DurationHours;
            shoot.Location = dto.Location;
            shoot.Status = dto.Status;
            shoot.Price = dto.Price;
            shoot.Notes = dto.Notes;
            shoot.ClientId = dto.ClientId;
            shoot.PhotographerId = dto.PhotographerId;
            shoot.UpdatedDate = DateTime.Now;

            await _photoShootService.UpdatePhotoShootAsync(shoot);

            return Json(new { success = true });
        }

        // ---- AJAX: Delete -----------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            await _photoShootService.DeletePhotoShootAsync(id);
            return Json(new { success = true });
        }

        // =====================================================
        // NEW ENDPOINTS FOR DYNAMIC DROPDOWNS
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _clientService.GetAllClientsAsync();

            var result = clients.Select(c => new
            {
                id = c.Id,
                name = $"{c.FirstName} {c.LastName}"
            });

            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPhotographers()
        {
            var users = await _userManager.GetUsersInRoleAsync("Photographer");

            var result = users.Select(u => new
            {
                id = u.Id,
                name = u.FullName ?? u.UserName
            });

            return Json(result);
        }
    }
}

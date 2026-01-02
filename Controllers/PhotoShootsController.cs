using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;
using MyPhotoBiz.Services;
using MyPhotoBiz.DTOs;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Controllers
{
    // TODO: [HIGH] Calendar Move action doesn't update EndTime field
    // TODO: [HIGH] CreateAjax/UpdateAjax lack server-side validation
    // TODO: [HIGH] Uses PhotographerId (string) but should use PhotographerProfileId (int)
    // TODO: [MEDIUM] Add past date validation when creating/updating shoots
    // TODO: [MEDIUM] Calendar GetEvents doesn't include PhotographerProfile for display
    // TODO: [MEDIUM] Add conflict detection for overlapping shoots
    // TODO: [FEATURE] Add drag-to-create in calendar view
    // TODO: [FEATURE] Add color coding by shoot type/status
    // TODO: [FEATURE] Add travel time calculation between shoots
    // TODO: [FEATURE] Add recurring shoot support
    [Authorize(Roles = "Admin,Photographer")]
    public class PhotoShootsController : Controller
    {
        private readonly IPhotoShootService _photoShootService;
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityService _activityService;

        public PhotoShootsController(
            IPhotoShootService photoShootService,
            IClientService clientService,
            UserManager<ApplicationUser> userManager,
            IActivityService activityService)
        {
            _photoShootService = photoShootService;
            _clientService = clientService;
            _userManager = userManager;
            _activityService = activityService;
        }

        // =====================================================
        // STANDARD CRUD
        // =====================================================

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Index()
        {
            var shoots = await _photoShootService.GetAllPhotoShootsAsync();
            return View(shoots);
        }

        [Authorize(Roles = "Admin,Photographer,Client")]
        public async Task<IActionResult> Details(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();

            // Client-only users may only view their own shoots
            // Admin and Photographer roles can view any shoot
            if (User.IsInRole("Client") && !User.IsInRole("Admin") && !User.IsInRole("Photographer"))
            {
                var userId = _userManager.GetUserId(User);
                var clientProfile = await _clientService.GetClientByUserIdAsync(userId!);
                if (clientProfile == null || shoot.ClientProfileId != clientProfile.Id)
                    return Forbid();
            }

            return View(shoot);
        }

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Create(int? clientId)
        {
            var clients = await _clientService.GetAllClientsAsync();

            var model = new PhotoShootViewModel
            {
                ScheduledDate = DateTime.Today,
                EndTime = DateTime.Today.AddHours(2), // Default 2 hour duration
                ClientId = clientId ?? 0,
                Status = PhotoShootStatus.InProgress,
                Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.User?.FirstName} {c.User?.LastName}",
                    Selected = clientId.HasValue && c.Id == clientId.Value
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Photographer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhotoShootViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientsDropdown(model);
                return View(model);
            }

            // Calculate duration from start and end time
            var duration = (model.EndTime - model.ScheduledDate).TotalHours;

            var shoot = new PhotoShoot
            {
                Title = model.Title!,
                ClientProfileId = model.ClientId,
                PhotographerId = _userManager.GetUserId(User)!,
                ScheduledDate = model.ScheduledDate,
                EndTime = model.EndTime,
                DurationHours = (int)Math.Round(duration),
                DurationMinutes = (int)Math.Round((duration - Math.Floor(duration)) * 60),
                Location = model.Location!,
                Status = model.Status,
                Price = model.Price,
                Notes = model.Notes!,
                CreatedDate = DateTime.Now
            };

            await _photoShootService.CreatePhotoShootAsync(shoot);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Edit(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();

            var clients = await _clientService.GetAllClientsAsync();

            var model = new PhotoShootViewModel
            {
                Id = shoot.Id,
                Title = shoot.Title,
                ClientId = shoot.ClientProfileId,
                ScheduledDate = shoot.ScheduledDate,
                EndTime = shoot.EndTime,
                DurationHours = shoot.DurationHours,
                Location = shoot.Location,
                Status = shoot.Status,
                Price = shoot.Price,
                Notes = shoot.Notes,
                Clients = clients.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.User?.FirstName} {c.User?.LastName}",
                    Selected = c.Id == shoot.ClientProfileId
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Photographer")]
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

            // Calculate duration from start and end time
            var duration = (model.EndTime - model.ScheduledDate).TotalHours;

            shoot.Title = model.Title!;
            shoot.ClientProfileId = model.ClientId;
            shoot.ScheduledDate = model.ScheduledDate;
            shoot.EndTime = model.EndTime;
            shoot.DurationHours = (int)Math.Round(duration);
            shoot.DurationMinutes = (int)Math.Round((duration - Math.Floor(duration)) * 60);
            shoot.Location = model.Location!;
            shoot.Status = model.Status;
            shoot.Price = model.Price;
            shoot.Notes = model.Notes!;
            shoot.UpdatedDate = DateTime.Now;

            await _photoShootService.UpdatePhotoShootAsync(shoot);

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Delete(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null) return NotFound();
            return View(shoot);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Photographer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _photoShootService.DeletePhotoShootAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // API endpoint for AJAX delete
        [HttpDelete]
        [Route("api/photoshoots/{id}")]
        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> DeletePhotoShootApi(int id)
        {
            var shoot = await _photoShootService.GetPhotoShootByIdAsync(id);
            if (shoot == null)
            {
                return NotFound(new { message = "Photo shoot not found" });
            }

            var result = await _photoShootService.DeletePhotoShootAsync(id);
            if (result)
            {
                return Ok(new { success = true, message = "Photo shoot deleted successfully" });
            }

            return BadRequest(new { message = "Failed to delete photo shoot" });
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyPhotoShoots()
        {
            var userId = _userManager.GetUserId(User);
            var clientProfile = await _clientService.GetClientByUserIdAsync(userId!);
            if (clientProfile == null) return NotFound();

            var shoots = await _photoShootService.GetPhotoShootsByClientIdAsync(clientProfile.Id);
            return View(shoots);
        }

        // Populate dropdown
        private async Task PopulateClientsDropdown(PhotoShootViewModel model)
        {
            var clients = await _clientService.GetAllClientsAsync();
            model.Clients = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.User?.FirstName} {c.User?.LastName}",
                Selected = c.Id == model.ClientId
            }).ToList();
        }

        // =====================================================
        // FULLCALENDAR API
        // =====================================================

        [Authorize(Roles = "Admin,Photographer")]
        public IActionResult Calendar() => View();

        // ---- Get Events -------------------------------------
        [HttpGet]
        [Authorize(Roles = "Admin,Photographer")]
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
                    clientId = ps.ClientProfileId,
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
        [Authorize(Roles = "Admin,Photographer")]
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
        [Authorize(Roles = "Admin,Photographer")]
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
                ClientProfileId = dto.ClientId,
                PhotographerId = dto.PhotographerId,
                CreatedDate = DateTime.Now
            };

            await _photoShootService.CreatePhotoShootAsync(shoot);

            return Json(new { success = true });
        }

        // ---- AJAX: Update -----------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin,Photographer")]
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
            shoot.ClientProfileId = dto.ClientId;
            shoot.PhotographerId = dto.PhotographerId;
            shoot.UpdatedDate = DateTime.Now;

            await _photoShootService.UpdatePhotoShootAsync(shoot);

            return Json(new { success = true });
        }

        // ---- AJAX: Delete -----------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            await _photoShootService.DeletePhotoShootAsync(id);
            return Json(new { success = true });
        }

        // =====================================================
        // NEW ENDPOINTS FOR DYNAMIC DROPDOWNS
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _clientService.GetAllClientsAsync();

            var result = clients.Select(c => new
            {
                id = c.Id,
                name = $"{c.User?.FirstName} {c.User?.LastName}"
            });

            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Photographer")]
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

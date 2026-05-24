
// Controllers/ClientsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.Helpers;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBadgeService _badgeService;
        private readonly IActivityService _activityService;

        public ClientsController(IClientService clientService, UserManager<ApplicationUser> userManager,
            IBadgeService badgeService, IActivityService activityService)
        {
            _clientService = clientService;
            _userManager = userManager;
            _badgeService = badgeService;
            _activityService = activityService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var clients = await _clientService.GetAllClientsAsync();
            return View(clients);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var clientProfile = await _clientService.GetClientByIdAsync(id);
            if (clientProfile == null)
            {
                return NotFound();
            }

            return View("Details", clientProfile.ToDetailsViewModel());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateClientViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(model);
                }

                var temporaryPassword = PasswordGenerator.GenerateSecurePassword();

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, temporaryPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Client");

                    // Create ClientProfile
                    var clientProfile = new ClientProfile
                    {
                        UserId = user.Id,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Notes = model.Notes ?? string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    await _clientService.CreateClientAsync(clientProfile);

                    await _badgeService.AwardBadgeByNameAsync(clientProfile.Id, "New User", "Auto-awarded on account creation");

                    // Log activity
                    await _activityService.LogActivityAsync("Created", "Client", clientProfile.Id,
                        $"{model.FirstName} {model.LastName}", null, _userManager.GetUserId(User));

                    TempData["SuccessMessage"] = $"Client created successfully. Temporary password: {temporaryPassword}";
                    TempData["PasswordWarning"] = "Please share this password securely with the client. It will not be shown again.";

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var clientProfile = await _clientService.GetClientByIdAsync(id);
            if (clientProfile == null)
            {
                return NotFound();
            }

            var model = new EditClientViewModel
            {
                Id = clientProfile.Id,
                FirstName = clientProfile.User?.FirstName ?? "",
                LastName = clientProfile.User?.LastName ?? "",
                Email = clientProfile.User?.Email ?? "",
                PhoneNumber = clientProfile.PhoneNumber,
                Address = clientProfile.Address,
                Notes = clientProfile.Notes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EditClientViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var clientProfile = await _clientService.GetClientByIdAsync(id);
                if (clientProfile == null)
                {
                    return NotFound();
                }

                // Update ApplicationUser fields
                if (clientProfile.User != null)
                {
                    clientProfile.User.FirstName = model.FirstName;
                    clientProfile.User.LastName = model.LastName;
                    await _userManager.UpdateAsync(clientProfile.User);
                }

                // Update ClientProfile fields
                clientProfile.PhoneNumber = model.PhoneNumber;
                clientProfile.Address = model.Address;
                clientProfile.Notes = model.Notes ?? string.Empty;

                await _clientService.UpdateClientAsync(clientProfile);

                // Log activity
                await _activityService.LogActivityAsync("Updated", "Client", clientProfile.Id,
                    $"{model.FirstName} {model.LastName}", null, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var clientProfile = await _clientService.GetClientByIdAsync(id);
            if (clientProfile == null)
            {
                return NotFound();
            }
            return View(clientProfile);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clientProfile = await _clientService.GetClientByIdAsync(id);
            var clientName = clientProfile != null
                ? $"{clientProfile.User?.FirstName} {clientProfile.User?.LastName}"
                : $"ID: {id}";

            await _clientService.DeleteClientAsync(id);

            // Log activity
            await _activityService.LogActivityAsync("Deleted", "Client", id,
                clientName, null, _userManager.GetUserId(User));

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);
            var clientProfile = await _clientService.GetClientByUserIdAsync(userId!);
            if (clientProfile == null)
            {
                return NotFound();
            }

            return View(clientProfile.ToDetailsViewModel());
        }

        // API endpoint for getting clients list (used by manage access modal)
        [HttpGet]
        [Route("api/clients")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClientsApi()
        {
            var clients = await _clientService.GetAllClientsAsync();
            var result = clients.Select(c => new
            {
                id = c.Id,
                firstName = c.User?.FirstName ?? "",
                lastName = c.User?.LastName ?? "",
                email = c.User?.Email ?? ""
            });
            return Json(result);
        }
    }
}


// Controllers/ClientsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.Helpers;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientsController(IClientService clientService, UserManager<ApplicationUser> userManager)
        {
            _clientService = clientService;
            _userManager = userManager;
        }

        private MyPhotoBiz.ViewModels.ClientDetailsViewModel MapToClientDetailsViewModel(Client client)
        {
            return new MyPhotoBiz.ViewModels.ClientDetailsViewModel
            {
                Id = client.Id,
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                Address = client.Address,
                Notes = client.Notes,
                UpdatedDate = client.UpdatedDate,
                CreatedDate = client.CreatedDate,
                User = client.User,
                PhotoShootCount = client.PhotoShoots?.Count ?? 0,
                InvoiceCount = client.Invoices?.Count ?? 0,
                TotalRevenue = client.Invoices?.Sum(i => i.Amount + i.Tax) ?? 0m,
                PhotoShoots = client.PhotoShoots?.Select(ps => new MyPhotoBiz.ViewModels.PhotoShootViewModel
                {
                    Id = ps.Id,
                    Title = ps.Title,
                    ClientId = ps.ClientId,
                    ScheduledDate = ps.ScheduledDate,
                    UpdatedDate = ps.UpdatedDate,
                    Location = ps.Location,
                    Status = ps.Status,
                    Price = ps.Price,
                    Notes = ps.Notes,
                    DurationHours = ps.DurationHours,
                    DurationMinutes = ps.DurationMinutes
                }).ToList() ?? new List<MyPhotoBiz.ViewModels.PhotoShootViewModel>(),
                Invoices = client.Invoices?.ToList() ?? new List<MyPhotoBiz.Models.Invoice>()
            };
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
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            var model = MapToClientDetailsViewModel(client);
            return View("Details", model);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Client client)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(client.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(client);
                }

                var temporaryPassword = PasswordGenerator.GenerateSecurePassword();

                var user = new ApplicationUser
                {
                    UserName = client.Email,
                    Email = client.Email,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, temporaryPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Client");

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    client.UserId = user.Id;
                    await _clientService.CreateClientAsync(client);

                    TempData["SuccessMessage"] = $"Client created successfully. Temporary password: {temporaryPassword}";
                    TempData["PasswordWarning"] = "Please share this password securely with the client. It will not be shown again.";

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(client);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _clientService.UpdateClientAsync(client);
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _clientService.DeleteClientAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);
            var client = await _clientService.GetClientByUserIdAsync(userId!);
            if (client == null)
            {
                return NotFound();
            }

            var model = MapToClientDetailsViewModel(client);
            return View(model);
        }
    }
}

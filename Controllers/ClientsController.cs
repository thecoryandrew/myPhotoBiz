
// Controllers/ClientsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;

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
            return View(client);
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
                // Create user account for client
                var user = new ApplicationUser
                {
                    UserName = client.Email,
                    Email = client.Email,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, "TempPassword123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Client");
                    client.UserId = user.Id;
                    await _clientService.CreateClientAsync(client);
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
            return View(client);
        }
    }
}

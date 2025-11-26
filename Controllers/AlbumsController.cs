using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Models.ViewModels;
using MyPhotoBiz.Services;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class AlbumsController : Controller
    {
        private readonly IAlbumService _albumService;
        private readonly IPhotoShootService _photoShootService;
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AlbumsController(
            IAlbumService albumService,
            IPhotoShootService photoShootService,
            IClientService clientService,
            UserManager<ApplicationUser> userManager)
        {
            _albumService = albumService;
            _photoShootService = photoShootService;
            _clientService = clientService;
            _userManager = userManager;
        }

        // GET: Albums
        public async Task<IActionResult> Index()
        {
            IEnumerable<Album> albums;

            if (User.IsInRole("Admin"))
            {
                albums = await _albumService.GetAllAlbumsAsync();
            }
            else if (User.IsInRole("Client"))
            {
                var userId = _userManager.GetUserId(User);
                var client = await _clientService.GetClientByUserIdAsync(userId!);
                if (client == null)
                    return Forbid();

                albums = await _albumService.GetAlbumsByClientIdAsync(client.Id);
            }
            else
            {
                return Forbid();
            }

            return View(albums);
        }

        // GET: Albums/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null || album.PhotoShoot == null)
                return NotFound();

            // Client access check
            if (User.IsInRole("Client"))
            {
                var userId = _userManager.GetUserId(User);
                var client = await _clientService.GetClientByUserIdAsync(userId!);
                if (client == null || album.ClientId != client.Id)
                    return Forbid();
            }

            // Map Album to AlbumViewModel
            var viewModel = new AlbumViewModel
            {
                Id = album.Id,
                Name = album.Name,
                Description = album.Description ?? string.Empty,
                PhotoShootId = album.PhotoShootId,
                PhotoShootTitle = album.PhotoShoot?.Title ?? string.Empty,
                Photos = album.Photos?.ToList() ?? new List<Photo>()
            };

            return View(viewModel);
        }

        // GET: Albums/Create?photoShootId=5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int photoShootId)
        {
            var photoShoot = await _photoShootService.GetPhotoShootByIdAsync(photoShootId);
            if (photoShoot == null)
                return NotFound();

            ViewBag.PhotoShootTitle = photoShoot.Title;

            var model = new AlbumViewModel
            {
                PhotoShootId = photoShootId,
                Description = string.Empty
            };

            return View(model);
        }

        // POST: Albums/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(AlbumViewModel model)
        {
            // Ensure the PhotoShoot exists
            var photoShoot = await _photoShootService.GetPhotoShootByIdAsync(model.PhotoShootId);
            if (photoShoot == null)
            {
                ModelState.AddModelError("", "Associated PhotoShoot does not exist.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PhotoShootTitle = photoShoot.Title;
                return View(model);
            }

            // Create Album with all required foreign keys
            var album = new Album
            {
                Name = model.Name,
                Description = model.Description ?? string.Empty,
                PhotoShootId = photoShoot.Id,
                ClientId = photoShoot.ClientId,
                CreatedDate = DateTime.Now,
                IsPublic = false
            };

            await _albumService.CreateAlbumAsync(album);

            return RedirectToAction("Details", "PhotoShoots", new { id = photoShoot.Id });
        }

        // GET: Albums/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
                return NotFound();

            var model = new AlbumViewModel
            {
                Id = album.Id,
                Name = album.Name,
                Description = album.Description ?? string.Empty,
                PhotoShootId = album.PhotoShootId,
                PhotoShootTitle = album.PhotoShoot?.Title ?? string.Empty,
                Photos = album.Photos?.ToList() ?? new List<Photo>()
            };

            return View(model);
        }

        // POST: Albums/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, AlbumViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.PhotoShootTitle = album.PhotoShoot?.Title ?? string.Empty;
                return View(model);
            }

            // Update only the editable fields
            album.Name = model.Name;
            album.Description = model.Description ?? string.Empty;

            await _albumService.UpdateAlbumAsync(album);

            return RedirectToAction(nameof(Details), new { id = album.Id });
        }

        // GET: Albums/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
                return NotFound();

            var viewModel = new AlbumViewModel
            {
                Id = album.Id,
                Name = album.Name,
                Description = album.Description ?? string.Empty,
                PhotoShootId = album.PhotoShootId,
                PhotoShootTitle = album.PhotoShoot?.Title ?? string.Empty,
                Photos = album.Photos?.ToList() ?? new List<Photo>()
            };

            return View(viewModel);
        }

        // POST: Albums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
                return NotFound();

            var photoShootId = album.PhotoShootId;

            await _albumService.DeleteAlbumAsync(id);

            return RedirectToAction("Details", "PhotoShoots", new { id = photoShootId });
        }

        // POST: Albums/TogglePublic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TogglePublic(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
                return NotFound();

            album.IsPublic = !album.IsPublic;
            await _albumService.UpdateAlbumAsync(album);

            return RedirectToAction(nameof(Details), new { id = album.Id });
        }
    }
}
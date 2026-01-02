using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;
using System;
using System.Threading.Tasks;

namespace MyPhotoBiz.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GalleriesController : Controller
    {
        private readonly IGalleryService _galleryService;
        private readonly ILogger<GalleriesController> _logger;

        public GalleriesController(IGalleryService galleryService, ILogger<GalleriesController> logger)
        {
            _galleryService = galleryService;
            _logger = logger;
        }

        // GET: Galleries
        public async Task<IActionResult> Index()
        {
            try
            {
                var galleries = await _galleryService.GetAllGalleriesAsync();
                var stats = await _galleryService.GetGalleryStatsAsync();

                var viewModel = new GalleriesIndexViewModel
                {
                    Galleries = galleries.ToList(),
                    Stats = stats
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading galleries index");
                TempData["ErrorMessage"] = "An error occurred while loading galleries.";
                return View(new GalleriesIndexViewModel());
            }
        }

        // GET: Galleries/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var gallery = await _galleryService.GetGalleryDetailsAsync(id);

                if (gallery == null)
                {
                    return NotFound();
                }

                // Set access URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                gallery.AccessUrl = await _galleryService.GetGalleryAccessUrlAsync(id, baseUrl);

                return PartialView("_GalleryDetailsModal", gallery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading gallery details for ID: {id}");
                return StatusCode(500, "An error occurred while loading gallery details.");
            }
        }

        // GET: Galleries/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new CreateGalleryViewModel
                {
                    AvailableAlbums = await _galleryService.GetAvailableAlbumsAsync()
                };

                return PartialView("_CreateGalleryModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create gallery modal");
                return StatusCode(500, "An error occurred while loading the form.");
            }
        }

        // POST: Galleries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGalleryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableAlbums = await _galleryService.GetAvailableAlbumsAsync();
                    return Json(new
                    {
                        success = false,
                        message = "Please correct the errors in the form.",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var gallery = await _galleryService.CreateGalleryAsync(model);

                _logger.LogInformation($"Gallery created: {gallery.Name} (ID: {gallery.Id})");

                return Json(new
                {
                    success = true,
                    message = $"Gallery '{gallery.Name}' created successfully!",
                    galleryId = gallery.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gallery");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while creating the gallery. Please try again."
                });
            }
        }

        // GET: Galleries/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var gallery = await _galleryService.GetGalleryByIdAsync(id);

                if (gallery == null)
                {
                    return NotFound();
                }

                var model = new EditGalleryViewModel
                {
                    Id = gallery.Id,
                    Name = gallery.Name,
                    Description = gallery.Description,
                    ExpiryDate = gallery.ExpiryDate,
                    BrandColor = gallery.BrandColor,
                    IsActive = gallery.IsActive,
                    CreatedDate = gallery.CreatedDate,
                    SelectedAlbumIds = gallery.Albums.Select(a => a.Id).ToList(),
                    AvailableAlbums = await _galleryService.GetAvailableAlbumsAsync(id)
                };

                return PartialView("_EditGalleryModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit gallery modal for ID: {id}");
                return StatusCode(500, "An error occurred while loading the form.");
            }
        }

        // POST: Galleries/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditGalleryViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    model.AvailableAlbums = await _galleryService.GetAvailableAlbumsAsync(id);
                    return Json(new
                    {
                        success = false,
                        message = "Please correct the errors in the form.",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var gallery = await _galleryService.UpdateGalleryAsync(model);

                _logger.LogInformation($"Gallery updated: {gallery.Name} (ID: {gallery.Id})");

                return Json(new
                {
                    success = true,
                    message = $"Gallery '{gallery.Name}' updated successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating gallery ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while updating the gallery. Please try again."
                });
            }
        }

        // POST: Galleries/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _galleryService.DeleteGalleryAsync(id);

                if (!result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Gallery not found."
                    });
                }

                _logger.LogInformation($"Gallery deleted: ID {id}");

                return Json(new
                {
                    success = true,
                    message = "Gallery deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting gallery ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the gallery. It may have active sessions."
                });
            }
        }

        // POST: Galleries/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var result = await _galleryService.ToggleGalleryStatusAsync(id, isActive);

                if (!result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Gallery not found."
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Gallery {(isActive ? "activated" : "deactivated")} successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling gallery status for ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while updating the gallery status."
                });
            }
        }

        // GET: Galleries/Sessions/5
        public async Task<IActionResult> Sessions(int id)
        {
            try
            {
                var sessions = await _galleryService.GetGallerySessionsAsync(id);
                var gallery = await _galleryService.GetGalleryByIdAsync(id);

                if (gallery == null)
                {
                    return NotFound();
                }

                ViewBag.GalleryName = gallery.Name;
                ViewBag.GalleryId = id;

                return PartialView("_GallerySessionsModal", sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading sessions for gallery ID: {id}");
                return StatusCode(500, "An error occurred while loading sessions.");
            }
        }

        // POST: Galleries/EndSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            try
            {
                var result = await _galleryService.EndSessionAsync(sessionId);

                if (!result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session not found."
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Session ended successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending session ID: {sessionId}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while ending the session."
                });
            }
        }

        // GET: Galleries/ManageAccess/5
        public async Task<IActionResult> ManageAccess(int id)
        {
            try
            {
                var gallery = await _galleryService.GetGalleryByIdAsync(id);
                if (gallery == null)
                {
                    return NotFound();
                }

                var accesses = await _galleryService.GetGalleryAccessesAsync(id);

                ViewBag.GalleryId = id;
                ViewBag.GalleryName = gallery.Name;

                return PartialView("_ManageAccessModal", accesses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading access management for gallery ID: {id}");
                return StatusCode(500, "An error occurred.");
            }
        }

        // POST: Galleries/GrantAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAccess(int galleryId, int clientProfileId, DateTime? expiryDate = null)
        {
            try
            {
                await _galleryService.GrantAccessAsync(galleryId, clientProfileId, expiryDate);
                return Json(new { success = true, message = "Access granted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error granting access to gallery {galleryId} for client {clientProfileId}");
                return Json(new { success = false, message = "An error occurred while granting access." });
            }
        }

        // POST: Galleries/RevokeAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAccess(int galleryId, int clientProfileId)
        {
            try
            {
                var result = await _galleryService.RevokeAccessAsync(galleryId, clientProfileId);
                if (!result)
                {
                    return Json(new { success = false, message = "Access not found." });
                }
                return Json(new { success = true, message = "Access revoked successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking access to gallery {galleryId} from client {clientProfileId}");
                return Json(new { success = false, message = "An error occurred while revoking access." });
            }
        }

        public async Task<IActionResult> GetAccessUrl(int id)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var url = await _galleryService.GetGalleryAccessUrlAsync(id, baseUrl);

                return Json(new
                {
                    success = true,
                    url = url
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting access URL for gallery ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred."
                });
            }
        }
    }
}

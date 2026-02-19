// Controllers/GalleryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;

namespace MyPhotoBiz.Controllers
{
    [Authorize(Roles = "Client")]
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalleryController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGalleryService _galleryService;

        public GalleryController(
            ApplicationDbContext context,
            ILogger<GalleryController> logger,
            UserManager<ApplicationUser> userManager,
            IGalleryService galleryService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _galleryService = galleryService;
        }

        /// <summary>
        /// Display list of accessible galleries for the logged-in client
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == userId);

            if (clientProfile == null)
            {
                _logger.LogWarning("No client profile found for user: {UserId}", userId);
                return View("NoAccess");
            }

            var viewModel = await _galleryService.GetClientAccessibleGalleriesAsync(clientProfile.Id);
            return View(viewModel);
        }

        /// <summary>
        /// Display gallery with photos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewGallery(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Validate user has access to this gallery
                var hasAccess = await _galleryService.ValidateUserAccessAsync(id, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} attempted to access gallery {GalleryId} without permission", userId, id);
                    return RedirectToAction("Index");
                }

                var gallery = await _galleryService.GetGalleryByIdAsync(id);
                if (gallery == null || !gallery.IsActive || gallery.ExpiryDate < DateTime.UtcNow)
                {
                    return RedirectToAction("Index");
                }

                // Create or update session for tracking (with expiry)
                var session = await _galleryService.GetOrCreateSessionAsync(id, userId);
                ViewBag.SessionToken = session?.SessionToken;

                // Get photos from all albums in this gallery
                var photos = await _galleryService.GetGalleryPhotosAsync(id);

                ViewBag.GalleryName = gallery.Name;
                ViewBag.BrandColor = gallery.BrandColor ?? "#2c3e50";
                ViewBag.GalleryId = gallery.Id;

                return View(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing gallery {GalleryId}", id);
                TempData["Error"] = "An error occurred while loading the gallery. Please try again.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Download full resolution photo
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Download(int photoId, int galleryId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Validate user has access to this gallery
                var hasAccess = await _galleryService.ValidateUserAccessAsync(galleryId, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("Download attempt without permission: user {UserId}, gallery {GalleryId}", userId, galleryId);
                    return Unauthorized();
                }

                // Get client profile to check download permission
                var clientProfile = await _context.ClientProfiles
                    .FirstOrDefaultAsync(cp => cp.UserId == userId);

                if (clientProfile != null)
                {
                    var canDownload = await _galleryService.CanClientDownloadAsync(galleryId, clientProfile.Id);
                    if (!canDownload)
                    {
                        _logger.LogWarning("Download not permitted for user {UserId} on gallery {GalleryId}", userId, galleryId);
                        return Forbid();
                    }
                }

                // Verify photo belongs to an album in the gallery
                var photo = await _context.Photos
                    .Include(p => p.Album)
                        .ThenInclude(a => a.Galleries)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == photoId && p.Album.Galleries.Any(g => g.Id == galleryId));

                if (photo == null)
                {
                    _logger.LogWarning("Download attempt for non-existent photo: {PhotoId}", photoId);
                    return NotFound();
                }

                // Validate file path
                if (string.IsNullOrEmpty(photo.FullImagePath))
                {
                    _logger.LogWarning("Photo has no file path: {PhotoId}", photoId);
                    return NotFound();
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.FullImagePath.TrimStart('/'));

                // Security: Validate path doesn't escape wwwroot
                var fullWwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var resolvedPath = Path.GetFullPath(filePath);
                if (!resolvedPath.StartsWith(fullWwwrootPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    return Unauthorized();
                }

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Photo file not found: {FilePath}", filePath);
                    return NotFound();
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = string.IsNullOrEmpty(photo.Title) ? $"photo_{photo.Id}.jpg" : $"{photo.Title}.jpg";

                _logger.LogInformation("Photo downloaded: {PhotoId} by user: {UserId}", photo.Id, userId);

                return File(fileBytes, "image/jpeg", fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when downloading photo");
                return StatusCode(403);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading photo");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get gallery session info via API
        /// </summary>
        [HttpGet]
        [Route("api/gallery/session/{galleryId}")]
        public async Task<IActionResult> GetSessionInfo(int galleryId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Not authenticated" });

                var hasAccess = await _galleryService.ValidateUserAccessAsync(galleryId, userId);
                if (!hasAccess)
                    return Unauthorized(new { success = false, message = "No access to gallery" });

                var gallery = await _galleryService.GetGalleryByIdAsync(galleryId);
                if (gallery == null || !gallery.IsActive || gallery.ExpiryDate < DateTime.UtcNow)
                    return Unauthorized(new { success = false, message = "Gallery expired" });

                var session = await _context.GallerySessions
                    .FirstOrDefaultAsync(s => s.GalleryId == galleryId && s.UserId == userId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        GalleryId = gallery.Id,
                        gallery.Name,
                        gallery.Description,
                        gallery.BrandColor,
                        gallery.LogoPath,
                        gallery.ExpiryDate,
                        CreatedDate = session?.CreatedDate,
                        LastAccessDate = session?.LastAccessDate,
                        SessionExpiresAt = session?.ExpiresAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session info");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// End gallery session
        /// </summary>
        [HttpPost]
        [Route("api/gallery/session/end/{galleryId}")]
        public async Task<IActionResult> EndSession(int galleryId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Not authenticated" });

                var session = await _context.GallerySessions
                    .FirstOrDefaultAsync(s => s.GalleryId == galleryId && s.UserId == userId);

                if (session == null)
                    return NotFound(new { success = false, message = "Session not found" });

                _context.GallerySessions.Remove(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Gallery session ended for user {UserId} on gallery {GalleryId}", userId, galleryId);

                return Ok(new { success = true, message = "Session ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }
}

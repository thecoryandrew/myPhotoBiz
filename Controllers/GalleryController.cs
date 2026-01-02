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
                _logger.LogWarning($"No client profile found for user: {userId}");
                return View("NoAccess");
            }

            // Get galleries the client has access to
            var accessibleGalleries = await _context.GalleryAccesses
                .Include(ga => ga.Gallery)
                    .ThenInclude(g => g.Albums)
                        .ThenInclude(a => a.Photos)
                .Where(ga => ga.ClientProfileId == clientProfile.Id &&
                            ga.IsActive &&
                            (!ga.ExpiryDate.HasValue || ga.ExpiryDate > DateTime.UtcNow) &&
                            ga.Gallery.IsActive &&
                            ga.Gallery.ExpiryDate > DateTime.UtcNow)
                .Select(ga => new
                {
                    Gallery = ga.Gallery,
                    Access = ga
                })
                .ToListAsync();

            var viewModel = accessibleGalleries.Select(item => new MyPhotoBiz.ViewModels.ClientGalleryViewModel
            {
                GalleryId = item.Gallery.Id,
                Name = item.Gallery.Name,
                Description = item.Gallery.Description,
                BrandColor = item.Gallery.BrandColor,
                PhotoCount = item.Gallery.Albums.SelectMany(a => a.Photos).Count(),
                ExpiryDate = item.Gallery.ExpiryDate,
                GrantedDate = item.Access.GrantedDate,
                CanDownload = item.Access.CanDownload,
                CanProof = item.Access.CanProof,
                CanOrder = item.Access.CanOrder
            }).ToList();

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
                    _logger.LogWarning($"User {userId} attempted to access gallery {id} without permission");
                    return RedirectToAction("Index");
                }

                var gallery = await _context.Galleries
                    .Include(g => g.Albums)
                        .ThenInclude(a => a.Photos)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (gallery == null || !gallery.IsActive || gallery.ExpiryDate < DateTime.UtcNow)
                {
                    return RedirectToAction("Index");
                }

                // Create or update session for tracking
                var clientProfile = await _context.ClientProfiles
                    .FirstOrDefaultAsync(cp => cp.UserId == userId);

                if (clientProfile != null)
                {
                    var session = await _context.GallerySessions
                        .FirstOrDefaultAsync(s => s.GalleryId == id && s.UserId == userId);

                    if (session == null)
                    {
                        session = new GallerySession
                        {
                            GalleryId = id,
                            UserId = userId,
                            SessionToken = Guid.NewGuid().ToString(),
                            CreatedDate = DateTime.UtcNow,
                            LastAccessDate = DateTime.UtcNow
                        };
                        _context.GallerySessions.Add(session);
                    }
                    else
                    {
                        session.LastAccessDate = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();

                    ViewBag.SessionToken = session.SessionToken;
                }

                // Get photos from all albums in this gallery
                var photos = gallery.Albums.SelectMany(a => a.Photos)
                    .OrderBy(p => p.DisplayOrder)
                    .ToList();

                ViewBag.GalleryName = gallery.Name;
                ViewBag.BrandColor = gallery.BrandColor ?? "#2c3e50";
                ViewBag.GalleryId = gallery.Id;

                return View(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing gallery {id}");
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
                    _logger.LogWarning($"Download attempt without permission: user {userId}, gallery {galleryId}");
                    return Unauthorized();
                }

                // Get gallery access to check download permission
                var clientProfile = await _context.ClientProfiles
                    .FirstOrDefaultAsync(cp => cp.UserId == userId);

                if (clientProfile != null)
                {
                    var access = await _context.GalleryAccesses
                        .FirstOrDefaultAsync(ga => ga.GalleryId == galleryId && ga.ClientProfileId == clientProfile.Id);

                    if (access != null && !access.CanDownload)
                    {
                        _logger.LogWarning($"Download not permitted for user {userId} on gallery {galleryId}");
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
                    _logger.LogWarning($"Download attempt for non-existent photo: {photoId}");
                    return NotFound();
                }

                // Validate file path
                if (string.IsNullOrEmpty(photo.FullImagePath))
                {
                    _logger.LogWarning($"Photo has no file path: {photoId}");
                    return NotFound();
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.FullImagePath.TrimStart('/'));

                // Security: Validate path doesn't escape wwwroot
                var fullWwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var resolvedPath = Path.GetFullPath(filePath);
                if (!resolvedPath.StartsWith(fullWwwrootPath))
                {
                    _logger.LogWarning($"Path traversal attempt detected: {filePath}");
                    return Unauthorized();
                }

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"Photo file not found: {filePath}");
                    return NotFound();
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = string.IsNullOrEmpty(photo.Title) ? $"photo_{photo.Id}.jpg" : $"{photo.Title}.jpg";

                _logger.LogInformation($"Photo downloaded: {photo.Id} by user: {userId}");

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

                var gallery = await _context.Galleries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == galleryId);

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
                        LastAccessDate = session?.LastAccessDate
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

                _logger.LogInformation($"Gallery session ended for user {userId} on gallery {galleryId}");

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

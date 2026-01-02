using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;

namespace MyPhotoBiz.Controllers
{
    // TODO: [CRITICAL-SECURITY] Uncomment [Authorize] - photos are currently publicly accessible!
    // TODO: [SECURITY] Add anonymous user validation in View/Thumbnail actions - currently only checks Client role
    // TODO: [SECURITY] Add rate limiting to prevent photo enumeration attacks
    // TODO: [FEATURE] Add watermarking support for client-facing photos
    // TODO: [FEATURE] Add batch download (ZIP) functionality
    // TODO: [FEATURE] Add photo reordering UI (DisplayOrder property exists but no endpoint)
    // [Authorize]
    public class PhotosController : Controller
    {
        private readonly IPhotoService _photoService;
        private readonly IAlbumService _albumService;
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IImageService _imageService;

        public PhotosController(IPhotoService photoService, IAlbumService albumService,
            IClientService clientService, UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment, IImageService imageService)
        {
            _photoService = photoService;
            _albumService = albumService;
            _clientService = clientService;
            _userManager = userManager;
            _environment = environment;
            _imageService = imageService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upload(int albumId)
        {
            var album = await _albumService.GetAlbumByIdAsync(albumId);
            if (album == null)
            {
                return NotFound();
            }

            ViewBag.AlbumId = albumId;
            ViewBag.AlbumName = album.Name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upload(int albumId, List<IFormFile> files)
        {
            var album = await _albumService.GetAlbumByIdAsync(albumId);
            if (album == null)
            {
                return NotFound();
            }

            if (files != null && files.Count > 0)
            {
                // Create the uploads directory path per-album
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "albums", albumId.ToString());
                Directory.CreateDirectory(uploadsPath);

                const long maxBytes = 20L * 1024 * 1024; // 20 MB per file

                foreach (var file in files)
                {
                    if (file == null) continue;
                    if (file.Length == 0) continue;
                    if (file.Length > maxBytes)
                    {
                        // skip overly large files; optionally add a TempData warning
                        continue;
                    }
                    if (!IsImageFile(file))
                    {
                        // skip non-images
                        continue;
                    }
                    // generate base name and process via ImageService (creates fullsize + thumbnail)
                    var baseName = Guid.NewGuid().ToString();
                    try
                    {
                        var (filePath, thumbPath, publicUrl) = await _imageService.ProcessAndSaveAlbumImageAsync(file, albumId, baseName);

                        var photo = new Photo
                        {
                            FileName = file.FileName,
                            FilePath = filePath,
                            ThumbnailPath = thumbPath,
                            FileSize = file.Length,
                            AlbumId = albumId,
                            ClientProfileId = album.PhotoShoot.ClientProfileId,
                            UploadDate = DateTime.Now,
                            UploadedDate = DateTime.Now,
                            DisplayOrder = 0,
                            IsSelected = false
                        };

                        await _photoService.CreatePhotoAsync(photo);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // collect warnings in a TempData string list stored as JSON
                        var warnings = new List<string>();
                        if (TempData.ContainsKey("UploadWarnings"))
                        {
                            var prev = TempData["UploadWarnings"] as string;
                            if (!string.IsNullOrEmpty(prev))
                            {
                                try { warnings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(prev) ?? new List<string>(); } catch { warnings = new List<string>(); }
                            }
                        }
                        warnings.Add($"{file.FileName}: {ex.Message}");
                        TempData["UploadWarnings"] = System.Text.Json.JsonSerializer.Serialize(warnings);
                    }
                }

                TempData["SuccessMessage"] = $"{files.Count} photo(s) uploaded successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "No files were selected for upload.";
            }

            return RedirectToAction("Details", "Albums", new { id = albumId });
        }

        public async Task<IActionResult> View(int id)
        {
            var photo = await _photoService.GetPhotoByIdAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            // Check if client can access this photo
            if (User.IsInRole("Client"))
            {
                var userId = _userManager.GetUserId(User);
                var client = await _clientService.GetClientByUserIdAsync(userId!);
                if (client == null || photo.Album.PhotoShoot.ClientProfileId != client.Id)
                {
                    return Forbid();
                }
            }

            // Return the photo file
            if (System.IO.File.Exists(photo.FilePath))
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(photo.FilePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                var mimeType = GetMimeType(photo.FilePath);
                return File(memory, mimeType);
            }

            return NotFound();
        }

        public async Task<IActionResult> Thumbnail(int id)
        {
            var photo = await _photoService.GetPhotoByIdAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            // Check if client can access this photo
            if (User.IsInRole("Client"))
            {
                var userId = _userManager.GetUserId(User);
                var client = await _clientService.GetClientByUserIdAsync(userId!);
                if (client == null || photo.Album.PhotoShoot.ClientProfileId != client.Id)
                {
                    return Forbid();
                }
            }

            // Return the thumbnail file, fall back to full image if thumbnail doesn't exist
            var thumbnailPath = !string.IsNullOrEmpty(photo.ThumbnailPath) && System.IO.File.Exists(photo.ThumbnailPath)
                ? photo.ThumbnailPath
                : photo.FilePath;

            if (System.IO.File.Exists(thumbnailPath))
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                var mimeType = GetMimeType(thumbnailPath);
                return File(memory, mimeType);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _photoService.GetPhotoByIdAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            var albumId = photo.AlbumId;
            var deleted = await _photoService.DeletePhotoAsync(id);

            if (deleted)
            {
                TempData["SuccessMessage"] = "Photo deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete photo.";
            }

            return RedirectToAction("Details", "Albums", new { id = albumId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleSelection(int id)
        {
            var photo = await _photoService.GetPhotoByIdAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            photo.IsSelected = !photo.IsSelected;
            await _photoService.UpdatePhotoAsync(photo);

            return RedirectToAction("Details", "Albums", new { id = photo.AlbumId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkDelete(int albumId, int[] photoIds)
        {
            if (photoIds == null || photoIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No photos selected for deletion.";
                return RedirectToAction("Details", "Albums", new { id = albumId });
            }

            int deletedCount = 0;
            foreach (var id in photoIds)
            {
                try
                {
                    var ok = await _photoService.DeletePhotoAsync(id);
                    if (ok) deletedCount++;
                }
                catch
                {
                    // ignore individual delete errors but continue
                }
            }

            TempData["SuccessMessage"] = $"Deleted {deletedCount} photo(s).";
            return RedirectToAction("Details", "Albums", new { id = albumId });
        }

        private bool IsImageFile(IFormFile file)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }

        private string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
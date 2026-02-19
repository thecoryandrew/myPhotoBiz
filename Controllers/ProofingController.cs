// Controllers/ProofingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPhotoBiz.Controllers
{
    [AllowAnonymous] // Proofing API accessible to clients via session token
    [Route("api/[controller]")]
    [ApiController]
    public class ProofingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProofingController> _logger;

        public ProofingController(ApplicationDbContext context, ILogger<ProofingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Validates session token and checks expiry. Returns null if invalid/expired.
        /// </summary>
        private async Task<GallerySession?> ValidateSessionAsync(string? sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
                return null;

            var session = await _context.GallerySessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session == null)
                return null;

            // Check if session has expired
            if (session.IsExpired)
            {
                _logger.LogWarning("Session {SessionToken} has expired", sessionToken);
                return null;
            }

            return session;
        }

        /// <summary>
        /// Mark or unmark a photo as favorite
        /// </summary>
        [HttpPost("mark-favorite")]
        public async Task<IActionResult> MarkFavorite([FromQuery] int photoId, [FromQuery] string sessionToken, [FromQuery] bool isFavorite)
        {
            try
            {
                var session = await ValidateSessionAsync(sessionToken);
                if (session == null)
                    return Unauthorized(new { success = false, message = "Invalid or expired session" });

                // Verify photo belongs to an album in the gallery
                var photo = await _context.Photos
                    .Include(p => p.Album)
                        .ThenInclude(a => a.Galleries)
                    .FirstOrDefaultAsync(p => p.Id == photoId && p.Album.Galleries.Any(g => g.Id == session.GalleryId));

                if (photo == null)
                    return NotFound(new { success = false, message = "Photo not found" });

                var proof = await _context.Proofs
                    .FirstOrDefaultAsync(p => p.PhotoId == photoId && p.GallerySessionId == session.Id);

                if (proof == null)
                {
                    proof = new Proof
                    {
                        PhotoId = photoId,
                        GallerySessionId = session.Id,
                        IsFavorite = isFavorite,
                        SelectedDate = DateTime.UtcNow,
                        ClientName = null
                    };
                    _context.Proofs.Add(proof);
                }
                else
                {
                    proof.IsFavorite = isFavorite;
                    proof.SelectedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Favorite updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking favorite");
                return StatusCode(500, new { success = false, message = "An error occurred while updating favorite" });
            }
        }

        /// <summary>
        /// Mark a photo for editing with optional notes
        /// </summary>
        [HttpPost("mark-for-editing")]
        public async Task<IActionResult> MarkForEditing([FromQuery] int photoId, [FromQuery] string sessionToken, [FromBody] EditingNotesRequest request)
        {
            try
            {
                var session = await ValidateSessionAsync(sessionToken);
                if (session == null)
                    return Unauthorized(new { success = false, message = "Invalid or expired session" });

                // Verify photo belongs to an album in the gallery
                var photo = await _context.Photos
                    .Include(p => p.Album)
                        .ThenInclude(a => a.Galleries)
                    .FirstOrDefaultAsync(p => p.Id == photoId && p.Album.Galleries.Any(g => g.Id == session.GalleryId));

                if (photo == null)
                    return NotFound(new { success = false, message = "Photo not found" });

                var proof = await _context.Proofs
                    .FirstOrDefaultAsync(p => p.PhotoId == photoId && p.GallerySessionId == session.Id);

                if (proof == null)
                {
                    proof = new Proof
                    {
                        PhotoId = photoId,
                        GallerySessionId = session.Id,
                        IsMarkedForEditing = true,
                        EditingNotes = request?.EditingNotes,
                        SelectedDate = DateTime.UtcNow,
                        ClientName = null
                    };
                    _context.Proofs.Add(proof);
                }
                else
                {
                    proof.IsMarkedForEditing = true;
                    proof.EditingNotes = request?.EditingNotes;
                    proof.SelectedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Photo marked for editing successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking for editing");
                return StatusCode(500, new { success = false, message = "An error occurred while marking for editing" });
            }
        }

        /// <summary>
        /// Get all favorite photos for a session
        /// </summary>
        [HttpGet("favorites/{sessionToken}")]
        public async Task<IActionResult> GetFavorites(string sessionToken)
        {
            try
            {
                var session = await ValidateSessionAsync(sessionToken);
                if (session == null)
                    return Unauthorized(new { success = false, message = "Invalid or expired session" });

                var favorites = await _context.Proofs
                    .Where(p => p.GallerySessionId == session.Id && p.IsFavorite && p.Photo != null)
                    .Include(p => p.Photo)
                    .Select(p => new
                    {
                        p.Photo!.Id,
                        p.Photo.Title,
                        p.Photo.ThumbnailPath,
                        p.Photo.FullImagePath,
                        p.IsFavorite
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = favorites });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving favorites" });
            }
        }

        /// <summary>
        /// Get all photos marked for editing for a session
        /// </summary>
        [HttpGet("editing/{sessionToken}")]
        public async Task<IActionResult> GetEditingPhotos(string sessionToken)
        {
            try
            {
                var session = await ValidateSessionAsync(sessionToken);
                if (session == null)
                    return Unauthorized(new { success = false, message = "Invalid or expired session" });

                var editingPhotos = await _context.Proofs
                    .Where(p => p.GallerySessionId == session.Id && p.IsMarkedForEditing && p.Photo != null)
                    .Include(p => p.Photo)
                    .Select(p => new
                    {
                        p.Photo!.Id,
                        p.Photo.Title,
                        p.Photo.ThumbnailPath,
                        p.EditingNotes,
                        p.SelectedDate
                    })
                    .OrderByDescending(p => p.SelectedDate)
                    .ToListAsync();

                return Ok(new { success = true, data = editingPhotos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting editing photos");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving editing photos" });
            }
        }

        /// <summary>
        /// Get proofing summary for a session
        /// </summary>
        [HttpGet("summary/{sessionToken}")]
        public async Task<IActionResult> GetProofingSummary(string sessionToken)
        {
            try
            {
                var session = await ValidateSessionAsync(sessionToken);
                if (session == null)
                    return Unauthorized(new { success = false, message = "Invalid or expired session" });

                var favoriteCount = await _context.Proofs
                    .CountAsync(p => p.GallerySessionId == session.Id && p.IsFavorite);

                var editingCount = await _context.Proofs
                    .CountAsync(p => p.GallerySessionId == session.Id && p.IsMarkedForEditing);

                // Count photos in all albums associated with the gallery
                var totalPhotos = await _context.Photos
                    .Include(p => p.Album)
                        .ThenInclude(a => a.Galleries)
                    .CountAsync(p => p.Album.Galleries.Any(g => g.Id == session.GalleryId));

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        TotalPhotos = totalPhotos,
                        FavoriteCount = favoriteCount,
                        EditingCount = editingCount,
                        ReviewedCount = favoriteCount + editingCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proofing summary");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving summary" });
            }
        }

        /// <summary>
        /// Remove a proof marking
        /// </summary>
        [HttpDelete("remove/{proofId}")]
        public async Task<IActionResult> RemoveProof(int proofId, [FromQuery] string sessionToken)
        {
            try
            {
                var session = await ValidateSessionAsync(sessionToken);
                if (session == null)
                    return Unauthorized(new { success = false, message = "Invalid or expired session" });

                var proof = await _context.Proofs.FindAsync(proofId);

                if (proof == null)
                    return NotFound(new { success = false, message = "Proof not found" });

                // Verify the proof belongs to this session
                if (proof.GallerySessionId != session.Id)
                    return Unauthorized(new { success = false, message = "Access denied" });

                _context.Proofs.Remove(proof);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Proof removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing proof");
                return StatusCode(500, new { success = false, message = "An error occurred while removing proof" });
            }
        }
    }

    /// <summary>
    /// Request model for editing notes
    /// </summary>
    public class EditingNotesRequest
    {
        public string? EditingNotes { get; set; }
    }
}
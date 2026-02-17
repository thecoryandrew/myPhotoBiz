using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MyPhotoBiz.Services
{
    // TODO: [HIGH] GetGalleryAccessUrlAsync returns generic URL - needs gallery-specific token/slug
    // TODO: [HIGH] GrantAccessAsync hardcodes all permissions to true - should be configurable
    // TODO: [MEDIUM] GetGalleryStatsAsync loads entire photo collection to count - use SQL
    // TODO: [MEDIUM] Add gallery photo pagination support
    // TODO: [MEDIUM] Add gallery search/filtering functionality
    // TODO: [FEATURE] Add public gallery sharing without login requirement
    // TODO: [FEATURE] Add gallery expiry notification emails
    // TODO: [FEATURE] Add download audit trail logging
    public class GalleryService : IGalleryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalleryService> _logger;

        public GalleryService(ApplicationDbContext context, ILogger<GalleryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<GalleryListItemViewModel>> GetAllGalleriesAsync()
        {
            try
            {
                // Project directly without Include - EF Core translates counts to SQL subqueries
                var galleries = await _context.Galleries
                    .AsNoTracking()
                    .OrderByDescending(g => g.CreatedDate)
                    .Select(g => new GalleryListItemViewModel
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Description = g.Description,
                        CreatedDate = g.CreatedDate,
                        ExpiryDate = g.ExpiryDate,
                        IsActive = g.IsActive,
                        PhotoCount = g.Albums.SelectMany(a => a.Photos).Count(),
                        SessionCount = g.Sessions.Count,
                        TotalProofs = g.Sessions.SelectMany(s => s.Proofs).Count(),
                        LastAccessDate = g.Sessions.Any() ? g.Sessions.Max(s => s.LastAccessDate) : (DateTime?)null
                    })
                    .ToListAsync();

                return galleries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all galleries");
                throw;
            }
        }

        public async Task<GalleryDetailsViewModel?> GetGalleryDetailsAsync(int id)
        {
            try
            {
                // Query gallery metadata and aggregate counts via SQL (no full photo load)
                var gallery = await _context.Galleries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (gallery == null)
                    return null;

                // Compute counts via SQL instead of loading all entities into memory
                var photoCount = await _context.Galleries
                    .Where(g => g.Id == id)
                    .SelectMany(g => g.Albums.SelectMany(a => a.Photos))
                    .CountAsync();

                var totalSessions = await _context.GallerySessions
                    .CountAsync(s => s.GalleryId == id);

                var activeSessions = await _context.GallerySessions
                    .CountAsync(s => s.GalleryId == id && s.LastAccessDate > DateTime.UtcNow.AddHours(-24));

                var lastAccessDate = await _context.GallerySessions
                    .Where(s => s.GalleryId == id)
                    .Select(s => (DateTime?)s.LastAccessDate)
                    .MaxAsync();

                var totalProofs = await _context.Proofs
                    .CountAsync(p => p.Session != null && p.Session.GalleryId == id);

                var totalFavorites = await _context.Proofs
                    .CountAsync(p => p.Session != null && p.Session.GalleryId == id && p.IsFavorite);

                var totalEditingRequests = await _context.Proofs
                    .CountAsync(p => p.Session != null && p.Session.GalleryId == id && p.IsMarkedForEditing);

                // Load only first page of photos (limit to 100)
                var photos = await _context.Galleries
                    .Where(g => g.Id == id)
                    .SelectMany(g => g.Albums.SelectMany(a => a.Photos))
                    .AsNoTracking()
                    .Take(100)
                    .Select(p => new PhotoViewModel
                    {
                        Id = p.Id,
                        Title = p.Title ?? p.FileName ?? "",
                        ThumbnailPath = p.ThumbnailPath ?? "",
                        FullImagePath = p.FullImagePath ?? ""
                    })
                    .ToListAsync();

                // Load only recent sessions (limit to 10)
                var recentSessions = await _context.GallerySessions
                    .Where(s => s.GalleryId == id)
                    .Include(s => s.Proofs)
                    .AsNoTracking()
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(10)
                    .Select(s => new GallerySessionViewModel
                    {
                        Id = s.Id,
                        SessionToken = s.SessionToken,
                        CreatedDate = s.CreatedDate,
                        LastAccessDate = s.LastAccessDate,
                        ProofCount = s.Proofs != null ? s.Proofs.Count : 0
                    })
                    .ToListAsync();

                var viewModel = new GalleryDetailsViewModel
                {
                    Id = gallery.Id,
                    Name = gallery.Name,
                    Description = gallery.Description,
                    CreatedDate = gallery.CreatedDate,
                    ExpiryDate = gallery.ExpiryDate,
                    IsActive = gallery.IsActive,
                    BrandColor = gallery.BrandColor,
                    PhotoCount = photoCount,
                    Photos = photos,
                    TotalSessions = totalSessions,
                    ActiveSessions = activeSessions,
                    LastAccessDate = lastAccessDate,
                    RecentSessions = recentSessions,
                    TotalProofs = totalProofs,
                    TotalFavorites = totalFavorites,
                    TotalEditingRequests = totalEditingRequests,
                    AccessUrl = "" // Will be set by controller with base URL
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving gallery details for ID: {id}");
                throw;
            }
        }

        public async Task<Gallery?> GetGalleryByIdAsync(int id)
        {
            try
            {
                return await _context.Galleries
                    .Include(g => g.Albums)
                        .ThenInclude(a => a.Photos)
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving gallery by ID: {id}");
                throw;
            }
        }

        public async Task<Gallery> CreateGalleryAsync(CreateGalleryViewModel model)
        {
            try
            {
                // Create gallery entity
                var gallery = new Gallery
                {
                    Name = model.Name,
                    Description = model.Description,
                    CreatedDate = DateTime.UtcNow,
                    ExpiryDate = model.ExpiryDate,
                    IsActive = model.IsActive,
                    BrandColor = model.BrandColor,
                    LogoPath = "" // Default logo path
                };

                _context.Galleries.Add(gallery);
                await _context.SaveChangesAsync();

                // Associate selected albums
                if (model.SelectedAlbumIds.Any())
                {
                    await AddAlbumsToGalleryAsync(gallery.Id, model.SelectedAlbumIds);
                }

                // Grant access to selected clients
                if (model.SelectedClientProfileIds?.Any() == true)
                {
                    foreach (var clientProfileId in model.SelectedClientProfileIds)
                    {
                        await GrantAccessAsync(gallery.Id, clientProfileId);
                    }
                }

                _logger.LogInformation($"Gallery created: {gallery.Name} (ID: {gallery.Id})");

                return gallery;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gallery");
                throw;
            }
        }

        public async Task<Gallery> UpdateGalleryAsync(EditGalleryViewModel model)
        {
            try
            {
                var gallery = await _context.Galleries
                    .Include(g => g.Albums)
                    .FirstOrDefaultAsync(g => g.Id == model.Id);

                if (gallery == null)
                    throw new InvalidOperationException($"Gallery not found: {model.Id}");

                // Update properties
                gallery.Name = model.Name;
                gallery.Description = model.Description;
                gallery.ExpiryDate = model.ExpiryDate;
                gallery.BrandColor = model.BrandColor;
                gallery.IsActive = model.IsActive;


                // Update album associations
                var currentAlbumIds = gallery.Albums.Select(a => a.Id).ToList();
                var albumsToAdd = model.SelectedAlbumIds.Except(currentAlbumIds).ToList();
                var albumsToRemove = currentAlbumIds.Except(model.SelectedAlbumIds).ToList();

                if (albumsToRemove.Any())
                {
                    var albumsToRemoveEntities = gallery.Albums.Where(a => albumsToRemove.Contains(a.Id)).ToList();
                    foreach (var album in albumsToRemoveEntities)
                    {
                        gallery.Albums.Remove(album);
                    }
                }

                if (albumsToAdd.Any())
                {
                    var newAlbums = await _context.Albums
                        .Where(a => albumsToAdd.Contains(a.Id))
                        .ToListAsync();

                    foreach (var album in newAlbums)
                    {
                        gallery.Albums.Add(album);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Gallery updated: {gallery.Name} (ID: {gallery.Id})");

                return gallery;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating gallery ID: {model.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteGalleryAsync(int id)
        {
            try
            {
                var gallery = await _context.Galleries
                    .Include(g => g.Sessions)
                    .Include(g => g.Albums)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (gallery == null)
                    return false;

                // Remove gallery (this will cascade delete sessions if configured)
                _context.Galleries.Remove(gallery);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Gallery deleted: {gallery.Name} (ID: {id})");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting gallery ID: {id}");
                throw;
            }
        }

        public async Task<bool> ToggleGalleryStatusAsync(int id, bool isActive)
        {
            try
            {
                var gallery = await _context.Galleries.FindAsync(id);

                if (gallery == null)
                    return false;

                gallery.IsActive = isActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Gallery {(isActive ? "activated" : "deactivated")}: {gallery.Name} (ID: {id})");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling gallery status for ID: {id}");
                throw;
            }
        }

        // Grant gallery access to a client
        public async Task<GalleryAccess> GrantAccessAsync(int galleryId, int clientProfileId, DateTime? expiryDate = null)
        {
            try
            {
                // Check if access already exists
                var existingAccess = await _context.GalleryAccesses
                    .FirstOrDefaultAsync(ga => ga.GalleryId == galleryId && ga.ClientProfileId == clientProfileId);

                if (existingAccess != null)
                {
                    // Reactivate if previously revoked
                    existingAccess.IsActive = true;
                    existingAccess.ExpiryDate = expiryDate;
                    await _context.SaveChangesAsync();
                    return existingAccess;
                }

                var access = new GalleryAccess
                {
                    GalleryId = galleryId,
                    ClientProfileId = clientProfileId,
                    GrantedDate = DateTime.UtcNow,
                    ExpiryDate = expiryDate,
                    IsActive = true,
                    CanDownload = true,
                    CanProof = true,
                    CanOrder = true
                };

                _context.GalleryAccesses.Add(access);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Access granted for gallery {galleryId} to client profile {clientProfileId}");

                return access;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error granting access for gallery {galleryId} to client profile {clientProfileId}");
                throw;
            }
        }

        // Revoke gallery access from a client
        public async Task<bool> RevokeAccessAsync(int galleryId, int clientProfileId)
        {
            try
            {
                var access = await _context.GalleryAccesses
                    .FirstOrDefaultAsync(ga => ga.GalleryId == galleryId && ga.ClientProfileId == clientProfileId);

                if (access == null)
                    return false;

                access.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Access revoked for gallery {galleryId} from client profile {clientProfileId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking access for gallery {galleryId} from client profile {clientProfileId}");
                throw;
            }
        }

        // Validate if a user has access to a gallery
        public async Task<bool> ValidateUserAccessAsync(int galleryId, string userId)
        {
            try
            {
                var clientProfile = await _context.ClientProfiles
                    .FirstOrDefaultAsync(cp => cp.UserId == userId);

                if (clientProfile == null)
                    return false;

                return await _context.GalleryAccesses
                    .AnyAsync(ga => ga.GalleryId == galleryId &&
                                    ga.ClientProfileId == clientProfile.Id &&
                                    ga.IsActive &&
                                    (!ga.ExpiryDate.HasValue || ga.ExpiryDate > DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating user access for gallery {galleryId}");
                throw;
            }
        }

        // Get all clients with access to a gallery
        public async Task<IEnumerable<GalleryAccess>> GetGalleryAccessesAsync(int galleryId)
        {
            try
            {
                return await _context.GalleryAccesses
                    .Include(ga => ga.ClientProfile)
                        .ThenInclude(cp => cp.User)
                    .Where(ga => ga.GalleryId == galleryId)
                    .OrderByDescending(ga => ga.GrantedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving gallery accesses for gallery {galleryId}");
                throw;
            }
        }

        public async Task<bool> AddAlbumsToGalleryAsync(int galleryId, List<int> albumIds)
        {
            try
            {
                var gallery = await _context.Galleries
                    .Include(g => g.Albums)
                    .FirstOrDefaultAsync(g => g.Id == galleryId);

                if (gallery == null)
                    return false;

                var albums = await _context.Albums
                    .Where(a => albumIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var album in albums)
                {
                    if (!gallery.Albums.Contains(album))
                    {
                        gallery.Albums.Add(album);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Added {albums.Count} albums to gallery ID: {galleryId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding albums to gallery ID: {galleryId}");
                throw;
            }
        }

        public async Task<bool> RemoveAlbumsFromGalleryAsync(int galleryId, List<int> albumIds)
        {
            try
            {
                var gallery = await _context.Galleries
                    .Include(g => g.Albums)
                    .FirstOrDefaultAsync(g => g.Id == galleryId);

                if (gallery == null)
                    return false;

                var albumsToRemove = gallery.Albums.Where(a => albumIds.Contains(a.Id)).ToList();

                foreach (var album in albumsToRemove)
                {
                    gallery.Albums.Remove(album);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Removed {albumsToRemove.Count} albums from gallery ID: {galleryId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing albums from gallery ID: {galleryId}");
                throw;
            }
        }

        public async Task<List<AlbumSelectionViewModel>> GetAvailableAlbumsAsync(int? currentGalleryId = null)
        {
            try
            {
                var query = _context.Albums
                    .Include(a => a.Photos)
                    .Include(a => a.ClientProfile)
                        .ThenInclude(cp => cp.User)
                    .Include(a => a.Galleries)
                    .AsQueryable();

                var albums = await query
                    .OrderBy(a => a.CreatedDate)
                    .ThenBy(a => a.Name)
                    .Select(a => new AlbumSelectionViewModel
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Description = a.Description,
                        PhotoCount = a.Photos.Count,
                        ClientName = a.ClientProfile != null && a.ClientProfile.User != null
                            ? $"{a.ClientProfile.User.FirstName} {a.ClientProfile.User.LastName}"
                            : null,
                        IsSelected = currentGalleryId.HasValue && a.Galleries.Any(g => g.Id == currentGalleryId.Value)
                    })
                    .ToListAsync();

                return albums;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available albums");
                throw;
            }
        }

        public async Task<IEnumerable<GallerySessionViewModel>> GetGallerySessionsAsync(int galleryId)
        {
            try
            {
                var sessions = await _context.GallerySessions
                    .Where(s => s.GalleryId == galleryId)
                    .Include(s => s.Proofs)
                    .AsNoTracking()
                    .OrderByDescending(s => s.CreatedDate)
                    .Select(s => new GallerySessionViewModel
                    {
                        Id = s.Id,
                        SessionToken = s.SessionToken,
                        CreatedDate = s.CreatedDate,
                        LastAccessDate = s.LastAccessDate,
                        ProofCount = s.Proofs != null ? s.Proofs.Count : 0
                    })
                    .ToListAsync();

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sessions for gallery ID: {galleryId}");
                throw;
            }
        }

        public async Task<bool> EndSessionAsync(int sessionId)
        {
            try
            {
                var session = await _context.GallerySessions.FindAsync(sessionId);

                if (session == null)
                    return false;

                _context.GallerySessions.Remove(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Session ended: {session.SessionToken}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending session ID: {sessionId}");
                throw;
            }
        }

        public async Task<bool> EndAllSessionsAsync(int galleryId)
        {
            try
            {
                var sessions = await _context.GallerySessions
                    .Where(s => s.GalleryId == galleryId)
                    .ToListAsync();

                _context.GallerySessions.RemoveRange(sessions);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"All sessions ended for gallery ID: {galleryId} ({sessions.Count} sessions)");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending all sessions for gallery ID: {galleryId}");
                throw;
            }
        }

        public async Task<GalleryStatsSummaryViewModel> GetGalleryStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Count total photos across all galleries
                var totalPhotosInGalleries = await _context.Galleries
                    .Include(g => g.Albums)
                        .ThenInclude(a => a.Photos)
                    .SelectMany(g => g.Albums.SelectMany(a => a.Photos))
                    .Distinct()
                    .CountAsync();

                var stats = new GalleryStatsSummaryViewModel
                {
                    TotalGalleries = await _context.Galleries.CountAsync(),
                    ActiveGalleries = await _context.Galleries.CountAsync(g => g.IsActive && g.ExpiryDate > now),
                    ExpiredGalleries = await _context.Galleries.CountAsync(g => g.ExpiryDate <= now),
                    TotalSessions = await _context.GallerySessions.CountAsync(),
                    TotalPhotos = totalPhotosInGalleries
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gallery statistics");
                throw;
            }
        }

        public Task<string> GetGalleryAccessUrlAsync(int galleryId, string baseUrl)
        {
            var url = $"{baseUrl.TrimEnd('/')}/Gallery/Index";
            return Task.FromResult(url);
        }
    }
}

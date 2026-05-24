using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public class GalleryService : IGalleryService
    {
        private readonly ApplicationDbContext _context;

        public GalleryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GalleryListItemViewModel>> GetAllGalleriesAsync()
        {
            return await _context.Galleries
                .Include(g => g.Albums)
                    .ThenInclude(a => a.Photos)
                .Include(g => g.Sessions)
                    .ThenInclude(s => s.Proofs)
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
                    TotalProofs = g.Sessions.Where(s => s.Proofs != null).SelectMany(s => s.Proofs).Count(),
                    LastAccessDate = g.Sessions.Any() ? g.Sessions.Max(s => s.LastAccessDate) : (DateTime?)null
                })
                .ToListAsync();
        }

        public async Task<GalleryDetailsViewModel?> GetGalleryDetailsAsync(int id)
        {
            var gallery = await _context.Galleries
                .Include(g => g.Albums)
                    .ThenInclude(a => a.Photos)
                .Include(g => g.Sessions)
                    .ThenInclude(s => s.Proofs)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gallery == null) return null;

            var allPhotos = gallery.Albums.SelectMany(a => a.Photos).ToList();
            var allProofs = gallery.Sessions.Where(s => s.Proofs != null).SelectMany(s => s.Proofs).ToList();

            return new GalleryDetailsViewModel
            {
                Id = gallery.Id,
                Name = gallery.Name,
                Description = gallery.Description,
                CreatedDate = gallery.CreatedDate,
                ExpiryDate = gallery.ExpiryDate,
                IsActive = gallery.IsActive,
                BrandColor = gallery.BrandColor,
                PhotoCount = allPhotos.Count,
                Photos = allPhotos.Select(p => new PhotoViewModel
                {
                    Id = p.Id,
                    Title = p.Title ?? p.FileName ?? "",
                    ThumbnailPath = p.ThumbnailPath ?? "",
                    FullImagePath = p.FullImagePath ?? ""
                }).ToList(),
                TotalSessions = gallery.Sessions.Count,
                ActiveSessions = gallery.Sessions.Count(s => s.LastAccessDate > DateTime.UtcNow.AddHours(-24)),
                LastAccessDate = gallery.Sessions.Any() ? gallery.Sessions.Max(s => s.LastAccessDate) : (DateTime?)null,
                RecentSessions = gallery.Sessions
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(10)
                    .Select(s => new GallerySessionViewModel
                    {
                        Id = s.Id,
                        SessionToken = s.SessionToken,
                        CreatedDate = s.CreatedDate,
                        LastAccessDate = s.LastAccessDate,
                        ProofCount = s.Proofs?.Count ?? 0
                    }).ToList(),
                TotalProofs = allProofs.Count,
                TotalFavorites = allProofs.Count(p => p.IsFavorite),
                TotalEditingRequests = allProofs.Count(p => p.IsMarkedForEditing),
                AccessUrl = ""
            };
        }

        public async Task<Gallery?> GetGalleryByIdAsync(int id)
        {
            return await _context.Galleries
                .Include(g => g.Albums)
                    .ThenInclude(a => a.Photos)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Gallery> CreateGalleryAsync(CreateGalleryViewModel model)
        {
            var gallery = new Gallery
            {
                Name = model.Name,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = model.ExpiryDate,
                IsActive = model.IsActive,
                BrandColor = model.BrandColor,
                LogoPath = ""
            };

            _context.Galleries.Add(gallery);
            await _context.SaveChangesAsync();

            if (model.SelectedAlbumIds.Any())
            {
                await AddAlbumsToGalleryAsync(gallery.Id, model.SelectedAlbumIds);
            }

            if (model.SelectedClientProfileIds?.Any() == true)
            {
                foreach (var clientProfileId in model.SelectedClientProfileIds)
                {
                    await GrantAccessAsync(gallery.Id, clientProfileId);
                }
            }

            return gallery;
        }

        public async Task<Gallery> UpdateGalleryAsync(EditGalleryViewModel model)
        {
            var gallery = await _context.Galleries
                .Include(g => g.Albums)
                .FirstOrDefaultAsync(g => g.Id == model.Id)
                ?? throw new InvalidOperationException($"Gallery not found: {model.Id}");

            gallery.Name = model.Name;
            gallery.Description = model.Description;
            gallery.ExpiryDate = model.ExpiryDate;
            gallery.BrandColor = model.BrandColor;
            gallery.IsActive = model.IsActive;

            var currentAlbumIds = gallery.Albums.Select(a => a.Id).ToList();
            var albumsToAdd = model.SelectedAlbumIds.Except(currentAlbumIds).ToList();
            var albumsToRemove = currentAlbumIds.Except(model.SelectedAlbumIds).ToList();

            if (albumsToRemove.Any())
            {
                foreach (var album in gallery.Albums.Where(a => albumsToRemove.Contains(a.Id)).ToList())
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
            return gallery;
        }

        public async Task<bool> DeleteGalleryAsync(int id)
        {
            var gallery = await _context.Galleries
                .Include(g => g.Sessions)
                .Include(g => g.Albums)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gallery == null) return false;

            _context.Galleries.Remove(gallery);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleGalleryStatusAsync(int id, bool isActive)
        {
            var gallery = await _context.Galleries.FindAsync(id);
            if (gallery == null) return false;

            gallery.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<GalleryAccess> GrantAccessAsync(int galleryId, int clientProfileId, DateTime? expiryDate = null)
        {
            var existingAccess = await _context.GalleryAccesses
                .FirstOrDefaultAsync(ga => ga.GalleryId == galleryId && ga.ClientProfileId == clientProfileId);

            if (existingAccess != null)
            {
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
            return access;
        }

        public async Task<bool> RevokeAccessAsync(int galleryId, int clientProfileId)
        {
            var access = await _context.GalleryAccesses
                .FirstOrDefaultAsync(ga => ga.GalleryId == galleryId && ga.ClientProfileId == clientProfileId);

            if (access == null) return false;

            access.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateUserAccessAsync(int galleryId, string userId)
        {
            var clientProfileId = await _context.ClientProfiles
                .Where(cp => cp.UserId == userId)
                .Select(cp => (int?)cp.Id)
                .FirstOrDefaultAsync();

            if (clientProfileId == null) return false;

            return await _context.GalleryAccesses
                .AnyAsync(ga => ga.GalleryId == galleryId &&
                                ga.ClientProfileId == clientProfileId &&
                                ga.IsActive &&
                                (!ga.ExpiryDate.HasValue || ga.ExpiryDate > DateTime.UtcNow));
        }

        public async Task<IEnumerable<GalleryAccess>> GetGalleryAccessesAsync(int galleryId)
        {
            return await _context.GalleryAccesses
                .Include(ga => ga.ClientProfile)
                    .ThenInclude(cp => cp!.User)
                .Where(ga => ga.GalleryId == galleryId)
                .OrderByDescending(ga => ga.GrantedDate)
                .ToListAsync();
        }

        public async Task<bool> AddAlbumsToGalleryAsync(int galleryId, List<int> albumIds)
        {
            var gallery = await _context.Galleries
                .Include(g => g.Albums)
                .FirstOrDefaultAsync(g => g.Id == galleryId);

            if (gallery == null) return false;

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
            return true;
        }

        public async Task<bool> RemoveAlbumsFromGalleryAsync(int galleryId, List<int> albumIds)
        {
            var gallery = await _context.Galleries
                .Include(g => g.Albums)
                .FirstOrDefaultAsync(g => g.Id == galleryId);

            if (gallery == null) return false;

            foreach (var album in gallery.Albums.Where(a => albumIds.Contains(a.Id)).ToList())
            {
                gallery.Albums.Remove(album);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AlbumSelectionViewModel>> GetAvailableAlbumsAsync(int? currentGalleryId = null)
        {
            return await _context.Albums
                .Include(a => a.Photos)
                .Include(a => a.ClientProfile)
                    .ThenInclude(cp => cp!.User)
                .Include(a => a.Galleries)
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
        }

        public async Task<IEnumerable<GallerySessionViewModel>> GetGallerySessionsAsync(int galleryId)
        {
            return await _context.GallerySessions
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
        }

        public async Task<bool> EndSessionAsync(int sessionId)
        {
            var session = await _context.GallerySessions.FindAsync(sessionId);
            if (session == null) return false;

            _context.GallerySessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EndAllSessionsAsync(int galleryId)
        {
            var sessions = await _context.GallerySessions
                .Where(s => s.GalleryId == galleryId)
                .ToListAsync();

            _context.GallerySessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<GalleryStatsSummaryViewModel> GetGalleryStatsAsync()
        {
            var now = DateTime.UtcNow;

            var totalPhotosInGalleries = await _context.Galleries
                .SelectMany(g => g.Albums.SelectMany(a => a.Photos))
                .Distinct()
                .CountAsync();

            return new GalleryStatsSummaryViewModel
            {
                TotalGalleries = await _context.Galleries.CountAsync(),
                ActiveGalleries = await _context.Galleries.CountAsync(g => g.IsActive && g.ExpiryDate > now),
                ExpiredGalleries = await _context.Galleries.CountAsync(g => g.ExpiryDate <= now),
                TotalSessions = await _context.GallerySessions.CountAsync(),
                TotalPhotos = totalPhotosInGalleries
            };
        }

        public Task<string> GetGalleryAccessUrlAsync(int galleryId, string baseUrl)
        {
            return Task.FromResult($"{baseUrl.TrimEnd('/')}/Gallery/Index");
        }
    }
}

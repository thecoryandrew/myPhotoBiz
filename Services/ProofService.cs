using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPhotoBiz.Services
{
    public class ProofService : IProofService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProofService> _logger;

        public ProofService(ApplicationDbContext context, ILogger<ProofService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ProofListItemViewModel>> GetAllProofsAsync(ProofFilterViewModel? filter = null)
        {
            try
            {
                var query = _context.Proofs
                    .Include(p => p.Photo)
                    .Include(p => p.Session)
                        .ThenInclude(s => s!.Gallery)
                    .AsQueryable();

                // Apply filters
                if (filter != null)
                {
                    if (filter.GalleryId.HasValue)
                    {
                        query = query.Where(p => p.Session != null && p.Session.GalleryId == filter.GalleryId.Value);
                    }

                    if (filter.IsFavorite.HasValue)
                    {
                        query = query.Where(p => p.IsFavorite == filter.IsFavorite.Value);
                    }

                    if (filter.IsMarkedForEditing.HasValue)
                    {
                        query = query.Where(p => p.IsMarkedForEditing == filter.IsMarkedForEditing.Value);
                    }

                    if (filter.StartDate.HasValue)
                    {
                        query = query.Where(p => p.SelectedDate >= filter.StartDate.Value);
                    }

                    if (filter.EndDate.HasValue)
                    {
                        var endDate = filter.EndDate.Value.Date.AddDays(1); // Include entire end date
                        query = query.Where(p => p.SelectedDate < endDate);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    {
                        var searchTerm = filter.SearchTerm.ToLower();
                        query = query.Where(p =>
                            (p.Photo != null && (p.Photo.Title ?? "").ToLower().Contains(searchTerm)) ||
                            (p.ClientName ?? "").ToLower().Contains(searchTerm) ||
                            (p.EditingNotes ?? "").ToLower().Contains(searchTerm));
                    }
                }

                var proofs = await query
                    .OrderByDescending(p => p.SelectedDate)
                    .Select(p => new ProofListItemViewModel
                    {
                        Id = p.Id,
                        PhotoId = p.PhotoId,
                        PhotoTitle = p.Photo != null ? (p.Photo.Title ?? p.Photo.FileName ?? "") : "",
                        PhotoThumbnailPath = p.Photo != null ? (p.Photo.ThumbnailPath ?? "") : "",
                        GalleryId = p.Session != null ? p.Session.GalleryId : 0,
                        GalleryName = p.Session != null && p.Session.Gallery != null ? p.Session.Gallery.Name : "Unknown",
                        GallerySessionId = p.GallerySessionId,
                        ClientName = p.ClientName,
                        SessionCreatedDate = p.Session != null ? p.Session.CreatedDate : DateTime.MinValue,
                        IsFavorite = p.IsFavorite,
                        IsMarkedForEditing = p.IsMarkedForEditing,
                        EditingNotes = p.EditingNotes,
                        SelectedDate = p.SelectedDate
                    })
                    .ToListAsync();

                return proofs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proofs");
                throw;
            }
        }

        public async Task<ProofDetailsViewModel?> GetProofDetailsAsync(int id)
        {
            try
            {
                var proof = await _context.Proofs
                    .Include(p => p.Photo)
                    .Include(p => p.Session)
                        .ThenInclude(s => s!.Gallery)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (proof == null)
                    return null;

                var viewModel = new ProofDetailsViewModel
                {
                    Id = proof.Id,
                    PhotoId = proof.PhotoId,
                    PhotoTitle = proof.Photo?.Title ?? proof.Photo?.FileName ?? "",
                    PhotoThumbnailPath = proof.Photo?.ThumbnailPath ?? "",
                    PhotoFullImagePath = proof.Photo?.FullImagePath ?? "",
                    GalleryId = proof.Session?.GalleryId ?? 0,
                    GalleryName = proof.Session?.Gallery?.Name ?? "Unknown",
                    GalleryDescription = proof.Session?.Gallery?.Description ?? "",
                    GallerySessionId = proof.GallerySessionId,
                    SessionToken = proof.Session?.SessionToken,
                    SessionCreatedDate = proof.Session?.CreatedDate,
                    SessionLastAccessDate = proof.Session?.LastAccessDate,
                    ClientName = proof.ClientName,
                    IsFavorite = proof.IsFavorite,
                    IsMarkedForEditing = proof.IsMarkedForEditing,
                    EditingNotes = proof.EditingNotes,
                    SelectedDate = proof.SelectedDate
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proof details for ID: {ProofId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ProofListItemViewModel>> GetProofsByGalleryAsync(int galleryId)
        {
            var filter = new ProofFilterViewModel { GalleryId = galleryId };
            return await GetAllProofsAsync(filter);
        }

        public async Task<IEnumerable<ProofListItemViewModel>> GetProofsByPhotoAsync(int photoId)
        {
            try
            {
                var proofs = await _context.Proofs
                    .Include(p => p.Photo)
                    .Include(p => p.Session)
                        .ThenInclude(s => s!.Gallery)
                    .Where(p => p.PhotoId == photoId)
                    .OrderByDescending(p => p.SelectedDate)
                    .Select(p => new ProofListItemViewModel
                    {
                        Id = p.Id,
                        PhotoId = p.PhotoId,
                        PhotoTitle = p.Photo != null ? (p.Photo.Title ?? p.Photo.FileName ?? "") : "",
                        PhotoThumbnailPath = p.Photo != null ? (p.Photo.ThumbnailPath ?? "") : "",
                        GalleryId = p.Session != null ? p.Session.GalleryId : 0,
                        GalleryName = p.Session != null && p.Session.Gallery != null ? p.Session.Gallery.Name : "Unknown",
                        GallerySessionId = p.GallerySessionId,
                        ClientName = p.ClientName,
                        SessionCreatedDate = p.Session != null ? p.Session.CreatedDate : DateTime.MinValue,
                        IsFavorite = p.IsFavorite,
                        IsMarkedForEditing = p.IsMarkedForEditing,
                        EditingNotes = p.EditingNotes,
                        SelectedDate = p.SelectedDate
                    })
                    .ToListAsync();

                return proofs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proofs for photo ID: {PhotoId}", photoId);
                throw;
            }
        }

        public async Task<ProofStatsSummaryViewModel> GetProofStatsAsync()
        {
            try
            {
                var stats = new ProofStatsSummaryViewModel
                {
                    TotalProofs = await _context.Proofs.CountAsync(),
                    TotalFavorites = await _context.Proofs.CountAsync(p => p.IsFavorite),
                    TotalEditingRequests = await _context.Proofs.CountAsync(p => p.IsMarkedForEditing),
                    UniquePhotosMarked = await _context.Proofs.Select(p => p.PhotoId).Distinct().CountAsync(),
                    ActiveGalleriesWithProofs = await _context.Proofs
                        .Include(p => p.Session)
                        .Where(p => p.Session != null)
                        .Select(p => p.Session!.GalleryId)
                        .Distinct()
                        .CountAsync()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proof statistics");
                throw;
            }
        }

        public async Task<ProofAnalyticsViewModel> GetProofAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Proofs
                    .Include(p => p.Photo)
                    .Include(p => p.Session)
                        .ThenInclude(s => s!.Gallery)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.SelectedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    var end = endDate.Value.Date.AddDays(1);
                    query = query.Where(p => p.SelectedDate < end);
                }

                var proofs = await query.ToListAsync();

                var analytics = new ProofAnalyticsViewModel
                {
                    MostFavoritedPhotos = await GetMostFavoritedPhotosAsync(10),
                    MostEditRequested = await GetMostEditRequestedPhotosAsync(10),
                    GalleryStats = await GetGalleryProofStatsAsync(),
                    ProofsByDate = proofs
                        .GroupBy(p => p.SelectedDate.Date)
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key.ToString("MMM dd"), g => g.Count()),
                    ProofsByGallery = proofs
                        .Where(p => p.Session != null && p.Session.Gallery != null)
                        .GroupBy(p => p.Session!.Gallery!.Name)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proof analytics");
                throw;
            }
        }

        public async Task<List<PopularPhotoViewModel>> GetMostFavoritedPhotosAsync(int topN = 10)
        {
            try
            {
                var favoritePhotos = await _context.Proofs
                    .Include(p => p.Photo)
                    .Include(p => p.Session)
                        .ThenInclude(s => s!.Gallery)
                    .Where(p => p.IsFavorite && p.Photo != null)
                    .GroupBy(p => new { p.PhotoId, p.Photo!.Title, p.Photo.ThumbnailPath })
                    .Select(g => new
                    {
                        PhotoId = g.Key.PhotoId,
                        PhotoTitle = g.Key.Title ?? "Untitled",
                        ThumbnailPath = g.Key.ThumbnailPath ?? "",
                        Count = g.Count(),
                        Proofs = g.ToList()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(topN)
                    .ToListAsync();

                var result = favoritePhotos.Select(fp => new PopularPhotoViewModel
                {
                    PhotoId = fp.PhotoId,
                    PhotoTitle = fp.PhotoTitle,
                    ThumbnailPath = fp.ThumbnailPath,
                    Count = fp.Count,
                    GalleryNames = fp.Proofs
                        .Where(p => p.Session != null && p.Session.Gallery != null)
                        .Select(p => p.Session!.Gallery!.Name)
                        .Distinct()
                        .ToList()
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most favorited photos");
                throw;
            }
        }

        public async Task<List<PopularPhotoViewModel>> GetMostEditRequestedPhotosAsync(int topN = 10)
        {
            try
            {
                var editRequestedPhotos = await _context.Proofs
                    .Include(p => p.Photo)
                    .Include(p => p.Session)
                        .ThenInclude(s => s!.Gallery)
                    .Where(p => p.IsMarkedForEditing && p.Photo != null)
                    .GroupBy(p => new { p.PhotoId, p.Photo!.Title, p.Photo.ThumbnailPath })
                    .Select(g => new
                    {
                        PhotoId = g.Key.PhotoId,
                        PhotoTitle = g.Key.Title ?? "Untitled",
                        ThumbnailPath = g.Key.ThumbnailPath ?? "",
                        Count = g.Count(),
                        Proofs = g.ToList()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(topN)
                    .ToListAsync();

                var result = editRequestedPhotos.Select(erp => new PopularPhotoViewModel
                {
                    PhotoId = erp.PhotoId,
                    PhotoTitle = erp.PhotoTitle,
                    ThumbnailPath = erp.ThumbnailPath,
                    Count = erp.Count,
                    GalleryNames = erp.Proofs
                        .Where(p => p.Session != null && p.Session.Gallery != null)
                        .Select(p => p.Session!.Gallery!.Name)
                        .Distinct()
                        .ToList()
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most edit requested photos");
                throw;
            }
        }

        private async Task<List<GalleryProofStatsViewModel>> GetGalleryProofStatsAsync()
        {
            try
            {
                var galleryStats = await _context.Galleries
                    .Include(g => g.Albums)
                        .ThenInclude(a => a.Photos)
                    .Include(g => g.Sessions)
                        .ThenInclude(s => s.Proofs)
                    .Select(g => new GalleryProofStatsViewModel
                    {
                        GalleryId = g.Id,
                        GalleryName = g.Name,
                        TotalProofs = g.Sessions.SelectMany(s => s.Proofs ?? new List<Models.Proof>()).Count(),
                        Favorites = g.Sessions.SelectMany(s => s.Proofs ?? new List<Models.Proof>()).Count(p => p.IsFavorite),
                        EditRequests = g.Sessions.SelectMany(s => s.Proofs ?? new List<Models.Proof>()).Count(p => p.IsMarkedForEditing),
                        TotalPhotos = g.Albums.SelectMany(a => a.Photos).Count()
                    })
                    .Where(g => g.TotalProofs > 0)
                    .OrderByDescending(g => g.TotalProofs)
                    .ToListAsync();

                return galleryStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gallery proof stats");
                throw;
            }
        }

        public async Task<string> ExportProofsToCsvAsync(ProofFilterViewModel? filter = null)
        {
            try
            {
                var proofs = await GetAllProofsAsync(filter);

                var csv = new StringBuilder();
                csv.AppendLine("Gallery,Photo,Client,Type,Favorite,Edit Request,Notes,Selected Date");

                foreach (var proof in proofs)
                {
                    csv.AppendLine($"\"{proof.GalleryName}\",\"{proof.PhotoTitle}\",\"{proof.ClientName ?? "Anonymous"}\",\"{proof.ProofType}\",{proof.IsFavorite},{proof.IsMarkedForEditing},\"{proof.EditingNotes ?? ""}\",\"{proof.SelectedDate:yyyy-MM-dd HH:mm}\"");
                }

                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting proofs to CSV");
                throw;
            }
        }

        public async Task<bool> DeleteProofAsync(int id)
        {
            try
            {
                var proof = await _context.Proofs.FindAsync(id);

                if (proof == null)
                    return false;

                _context.Proofs.Remove(proof);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Proof deleted: ID {ProofId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting proof ID: {ProofId}", id);
                throw;
            }
        }

        public async Task<bool> BulkDeleteProofsAsync(List<int> ids)
        {
            try
            {
                var proofs = await _context.Proofs
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();

                if (!proofs.Any())
                    return false;

                _context.Proofs.RemoveRange(proofs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk delete: {DeletedCount} proofs deleted", proofs.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting proofs");
                throw;
            }
        }

        public async Task<List<GalleryFilterOption>> GetGalleryFilterOptionsAsync()
        {
            try
            {
                var galleries = await _context.Galleries
                    .OrderBy(g => g.Name)
                    .Select(g => new GalleryFilterOption
                    {
                        Id = g.Id,
                        Name = g.Name
                    })
                    .ToListAsync();

                return galleries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gallery filter options");
                throw;
            }
        }
    }
}

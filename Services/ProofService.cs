using System.Text;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public class ProofService : IProofService
    {
        private readonly ApplicationDbContext _context;

        public ProofService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProofListItemViewModel>> GetAllProofsAsync(ProofFilterViewModel? filter = null)
        {
            var query = _context.Proofs
                .Include(p => p.Photo)
                .Include(p => p.Session)
                    .ThenInclude(s => s!.Gallery)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.GalleryId.HasValue)
                    query = query.Where(p => p.Session != null && p.Session.GalleryId == filter.GalleryId.Value);

                if (filter.IsFavorite.HasValue)
                    query = query.Where(p => p.IsFavorite == filter.IsFavorite.Value);

                if (filter.IsMarkedForEditing.HasValue)
                    query = query.Where(p => p.IsMarkedForEditing == filter.IsMarkedForEditing.Value);

                if (filter.StartDate.HasValue)
                    query = query.Where(p => p.SelectedDate >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                {
                    var endDate = filter.EndDate.Value.Date.AddDays(1);
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

            return await query
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
        }

        public async Task<ProofDetailsViewModel?> GetProofDetailsAsync(int id)
        {
            var proof = await _context.Proofs
                .Include(p => p.Photo)
                .Include(p => p.Session)
                    .ThenInclude(s => s!.Gallery)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proof == null) return null;

            return new ProofDetailsViewModel
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
        }

        public Task<IEnumerable<ProofListItemViewModel>> GetProofsByGalleryAsync(int galleryId)
            => GetAllProofsAsync(new ProofFilterViewModel { GalleryId = galleryId });

        public async Task<IEnumerable<ProofListItemViewModel>> GetProofsByPhotoAsync(int photoId)
        {
            return await _context.Proofs
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
        }

        public async Task<ProofStatsSummaryViewModel> GetProofStatsAsync()
        {
            return new ProofStatsSummaryViewModel
            {
                TotalProofs = await _context.Proofs.CountAsync(),
                TotalFavorites = await _context.Proofs.CountAsync(p => p.IsFavorite),
                TotalEditingRequests = await _context.Proofs.CountAsync(p => p.IsMarkedForEditing),
                UniquePhotosMarked = await _context.Proofs.Select(p => p.PhotoId).Distinct().CountAsync(),
                ActiveGalleriesWithProofs = await _context.Proofs
                    .Where(p => p.Session != null)
                    .Select(p => p.Session!.GalleryId)
                    .Distinct()
                    .CountAsync()
            };
        }

        public async Task<ProofAnalyticsViewModel> GetProofAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Proofs
                .Include(p => p.Photo)
                .Include(p => p.Session)
                    .ThenInclude(s => s!.Gallery)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.SelectedDate >= startDate.Value);

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(p => p.SelectedDate < end);
            }

            var proofs = await query.ToListAsync();

            return new ProofAnalyticsViewModel
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
        }

        public async Task<List<PopularPhotoViewModel>> GetMostFavoritedPhotosAsync(int topN = 10)
            => await GetPopularPhotosAsync(p => p.IsFavorite, topN);

        public async Task<List<PopularPhotoViewModel>> GetMostEditRequestedPhotosAsync(int topN = 10)
            => await GetPopularPhotosAsync(p => p.IsMarkedForEditing, topN);

        public async Task<string> ExportProofsToCsvAsync(ProofFilterViewModel? filter = null)
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

        public async Task<bool> DeleteProofAsync(int id)
        {
            var proof = await _context.Proofs.FindAsync(id);
            if (proof == null) return false;

            _context.Proofs.Remove(proof);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BulkDeleteProofsAsync(List<int> ids)
        {
            var proofs = await _context.Proofs
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            if (!proofs.Any()) return false;

            _context.Proofs.RemoveRange(proofs);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GalleryFilterOption>> GetGalleryFilterOptionsAsync()
        {
            return await _context.Galleries
                .OrderBy(g => g.Name)
                .Select(g => new GalleryFilterOption
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .ToListAsync();
        }

        private async Task<List<PopularPhotoViewModel>> GetPopularPhotosAsync(
            System.Linq.Expressions.Expression<Func<Models.Proof, bool>> predicate, int topN)
        {
            var matching = await _context.Proofs
                .Include(p => p.Photo)
                .Include(p => p.Session)
                    .ThenInclude(s => s!.Gallery)
                .Where(predicate)
                .Where(p => p.Photo != null)
                .ToListAsync();

            return matching
                .GroupBy(p => new { p.PhotoId, Title = p.Photo!.Title, p.Photo.ThumbnailPath })
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .Select(g => new PopularPhotoViewModel
                {
                    PhotoId = g.Key.PhotoId,
                    PhotoTitle = g.Key.Title ?? "Untitled",
                    ThumbnailPath = g.Key.ThumbnailPath ?? "",
                    Count = g.Count(),
                    GalleryNames = g
                        .Where(p => p.Session != null && p.Session.Gallery != null)
                        .Select(p => p.Session!.Gallery!.Name)
                        .Distinct()
                        .ToList()
                })
                .ToList();
        }

        private async Task<List<GalleryProofStatsViewModel>> GetGalleryProofStatsAsync()
        {
            return await _context.Galleries
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
        }

    }
}

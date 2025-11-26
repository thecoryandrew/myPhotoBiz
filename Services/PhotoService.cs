
// Services/PhotoService.cs
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PhotoService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IEnumerable<Photo>> GetPhotosByAlbumIdAsync(int albumId)
        {
            return await _context.Photos
                .Where(p => p.AlbumId == albumId)
                .OrderBy(p => p.UploadDate)
                .ToListAsync();
        }

        public async Task<Photo?> GetPhotoByIdAsync(int id)
        {
            return await _context.Photos
                .Include(p => p.Album)
                    .ThenInclude(a => a.PhotoShoot)
                        .ThenInclude(ps => ps.Client)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Photo> CreatePhotoAsync(Photo photo)
        {
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();
            return photo;
        }

        public async Task<bool> DeletePhotoAsync(int id)
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null) return false;

            // Delete physical file
            if (File.Exists(photo.FilePath))
            {
                File.Delete(photo.FilePath);
            }
            if (!string.IsNullOrEmpty(photo.ThumbnailPath) && File.Exists(photo.ThumbnailPath))
            {
                File.Delete(photo.ThumbnailPath);
            }

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> SavePhotoAsync(IFormFile file, string uploadsPath)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, fileName);

            Directory.CreateDirectory(uploadsPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public async Task<Photo> UpdatePhotoAsync(Photo photo)
        {
            _context.Photos.Update(photo);
            await _context.SaveChangesAsync();
            return photo;
        }
    }
}
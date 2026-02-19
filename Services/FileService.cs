
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class FileService : IFileService
   {
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IActivityService _activityService;

    public FileService(ApplicationDbContext context, IWebHostEnvironment env, IActivityService activityService)
    {
        _context = context;
        _env = env;
        _activityService = activityService;
    }

        public async Task<IEnumerable<FileItem>> GetFilesAsync(string filterType, int page, int pageSize)
        {
            var query = _context.Files.AsQueryable();
            if (!string.IsNullOrEmpty(filterType))
                query = query.Where(f => f.Type.Equals(filterType, StringComparison.OrdinalIgnoreCase));

            return await query
                .OrderByDescending(f => f.Modified)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<FileItem?> GetFileAsync(int id) => await _context.Files.FindAsync(id);

        public async Task UploadFileAsync(IFormFile file, string owner)
        {
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Sanitize filename: use GUID to prevent path traversal and overwrites
            var originalName = Path.GetFileName(file.FileName); // strip directory components
            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalName)}";
            var filePath = Path.Combine(uploadPath, safeFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileItem = new FileItem
            {
                Name = originalName,
                Type = Path.GetExtension(originalName).Trim('.').ToUpper(),
                Size = file.Length,
                Modified = DateTime.UtcNow,
                Owner = owner,
                FilePath = filePath
            };

            _context.Files.Add(fileItem);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFileAsync(int id)
        {
            var fileItem = await _context.Files.FindAsync(id);
            if (fileItem != null)
            {
                var fileName = fileItem.Name;
                if (System.IO.File.Exists(fileItem.FilePath))
                    System.IO.File.Delete(fileItem.FilePath);

                _context.Files.Remove(fileItem);
                await _context.SaveChangesAsync();

                // Audit log
                await _activityService.LogActivityAsync(
                    "Deleted",
                    "File",
                    id,
                    fileName,
                    $"File '{fileName}' was deleted");
            }
        }
    }
}
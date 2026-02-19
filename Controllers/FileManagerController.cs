using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers;

[Authorize]
public class FileManagerController : Controller
{
    private readonly IFileService _fileService;

    public FileManagerController(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<IActionResult> Index(string filterType = "", int page = 1, int pageSize = 10)
    {
        var files = await _fileService.GetFilesAsync(filterType, page, pageSize);

        var vm = new FileManagerViewModel
        {
            Files = files,
            FilterType = filterType ?? "",
            PageSize = pageSize,
            CurrentPage = page
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var owner = User.Identity?.Name ?? "Unknown";
            await _fileService.UploadFileAsync(file, owner);
            TempData["SuccessMessage"] = "File uploaded successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(int id)
    {
        var fileItem = await _fileService.GetFileAsync(id);
        if (fileItem == null || fileItem.FilePath == null)
        {
            TempData["ErrorMessage"] = "File not found.";
            return RedirectToAction(nameof(Index));
        }

        if (!System.IO.File.Exists(fileItem.FilePath))
        {
            TempData["ErrorMessage"] = "File no longer exists on disk.";
            return RedirectToAction(nameof(Index));
        }

        var mimeType = "application/octet-stream";
        return PhysicalFile(fileItem.FilePath, mimeType, fileItem.Name);
    }

    [HttpDelete]
    [Route("api/files/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApi(int id)
    {
        var fileItem = await _fileService.GetFileAsync(id);
        if (fileItem == null)
        {
            return NotFound(new { message = "File not found" });
        }

        await _fileService.DeleteFileAsync(id);
        return Ok(new { success = true, message = "File deleted successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _fileService.DeleteFileAsync(id);
        TempData["SuccessMessage"] = "File deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}

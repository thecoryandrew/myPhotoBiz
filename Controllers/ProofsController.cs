using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MyPhotoBiz.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProofsController : Controller
    {
        private readonly IProofService _proofService;
        private readonly ILogger<ProofsController> _logger;

        public ProofsController(IProofService proofService, ILogger<ProofsController> logger)
        {
            _proofService = proofService;
            _logger = logger;
        }

        // GET: Proofs
        public async Task<IActionResult> Index(ProofFilterViewModel? filter)
        {
            try
            {
                filter ??= new ProofFilterViewModel();

                var proofs = await _proofService.GetAllProofsAsync(filter);
                var stats = await _proofService.GetProofStatsAsync();
                var galleries = await _proofService.GetGalleryFilterOptionsAsync();

                var viewModel = new ProofsIndexViewModel
                {
                    Proofs = proofs.ToList(),
                    Filters = filter,
                    Stats = stats,
                    AvailableGalleries = galleries
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proofs index");
                TempData["ErrorMessage"] = "An error occurred while loading proofs.";
                return View(new ProofsIndexViewModel());
            }
        }

        // GET: Proofs/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var proof = await _proofService.GetProofDetailsAsync(id);

                if (proof == null)
                {
                    return NotFound();
                }

                return PartialView("_ProofDetailsModal", proof);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proof details for ID: {ProofId}", id);
                return StatusCode(500, "An error occurred while loading proof details.");
            }
        }

        // GET: Proofs/Analytics
        public async Task<IActionResult> Analytics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var analytics = await _proofService.GetProofAnalyticsAsync(startDate, endDate);

                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;

                return View(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proof analytics");
                TempData["ErrorMessage"] = "An error occurred while loading analytics.";
                return View(new ProofAnalyticsViewModel());
            }
        }

        // GET: Proofs/Export
        public async Task<IActionResult> Export(string format, ProofFilterViewModel? filter)
        {
            try
            {
                if (format?.ToLower() == "csv")
                {
                    var csvContent = await _proofService.ExportProofsToCsvAsync(filter);
                    var bytes = Encoding.UTF8.GetBytes(csvContent);
                    var fileName = $"proofs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                    return File(bytes, "text/csv", fileName);
                }

                return BadRequest("Unsupported export format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting proofs");
                TempData["ErrorMessage"] = "An error occurred while exporting proofs.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Proofs/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _proofService.DeleteProofAsync(id);

                if (!result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Proof not found."
                    });
                }

                _logger.LogInformation("Proof deleted: ID {ProofId}", id);

                return Json(new
                {
                    success = true,
                    message = "Proof deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting proof ID: {ProofId}", id);
                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the proof."
                });
            }
        }

        // POST: Proofs/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "No proofs selected."
                    });
                }

                var result = await _proofService.BulkDeleteProofsAsync(ids);

                if (!result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Proofs not found."
                    });
                }

                _logger.LogInformation("Bulk delete: {Count} proofs deleted", ids.Count);

                return Json(new
                {
                    success = true,
                    message = $"{ids.Count} proofs deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting proofs");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting proofs."
                });
            }
        }

        // GET: Proofs/ByGallery/5
        public async Task<IActionResult> ByGallery(int galleryId)
        {
            try
            {
                var proofs = await _proofService.GetProofsByGalleryAsync(galleryId);

                return Json(new
                {
                    success = true,
                    data = proofs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proofs for gallery ID: {GalleryId}", galleryId);
                return Json(new
                {
                    success = false,
                    message = "An error occurred while retrieving proofs."
                });
            }
        }
    }
}

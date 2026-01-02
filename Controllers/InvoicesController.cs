using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;
using MyPhotoBiz.Models;

// ADD THIS ALIAS to resolve the ambiguity
using InvoiceItemVM = MyPhotoBiz.ViewModels.InvoiceItemViewModel;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IClientService _clientService;
        private readonly IPhotoShootService _photoShootService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IInvoiceService invoiceService,
            IClientService clientService,
            IPhotoShootService photoShootService,
            IPdfService pdfService,
            ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _clientService = clientService;
            _photoShootService = photoShootService;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var vmList = invoices.Select(i => new InvoiceViewModel
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                Status = i.Status,
                Amount = i.Amount,
                Tax = i.Tax,
                Notes = i.Notes,
                ClientName = i.ClientProfile?.User != null ? $"{i.ClientProfile.User.FirstName} {i.ClientProfile.User.LastName}" : "Unknown Client",
                ClientEmail = i.ClientProfile?.User?.Email ?? "No Email",
                PhotoShootTitle = i.PhotoShoot?.Title
            }).ToList();

            return View(vmList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            var vm = new InvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                Amount = invoice.Amount,
                Tax = invoice.Tax,
                Notes = invoice.Notes,
                PaidDate = invoice.PaidDate,
                ClientName = invoice.ClientProfile?.User != null ? $"{invoice.ClientProfile.User.FirstName} {invoice.ClientProfile.User.LastName}" : "Unknown Client",
                ClientEmail = invoice.ClientProfile?.User?.Email ?? "No Email",
                PhotoShootTitle = invoice.PhotoShoot?.Title,
                InvoiceItems = invoice.InvoiceItems?.Select(ii => new InvoiceItemVM
                {
                    Description = ii.Description,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice
                }).ToList() ?? new List<InvoiceItemVM>()
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateClientsAndPhotoShootsAsync();

            var vm = new CreateInvoiceViewModel
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Amount = 0m,
                Tax = 0m,
                InvoiceItems = new List<InvoiceItemVM>
                {
                    new InvoiceItemVM
                    {
                        Description = "",
                        Quantity = 1,
                        UnitPrice = 0m
                    }
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateInvoiceViewModel vm, string action)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientsAndPhotoShootsAsync();
                return View(vm);
            }

            // Note: Client creation should be done through the proper ClientsController
            // which creates both ApplicationUser and ClientProfile

            var invoice = new Invoice
            {
                InvoiceNumber = vm.InvoiceNumber,
                InvoiceDate = vm.InvoiceDate,
                DueDate = vm.DueDate,
                Status = action?.ToLower() == "send" ? InvoiceStatus.Pending : vm.Status,
                Amount = vm.Amount,
                Tax = vm.Tax,
                Notes = vm.Notes,
                ClientProfileId = vm.ClientId,
                PhotoShootId = vm.PhotoShootId,
                InvoiceItems = vm.InvoiceItems?.Where(ii => !string.IsNullOrWhiteSpace(ii.Description))
                    .Select(ii => new InvoiceItem
                    {
                        Description = ii.Description,
                        Quantity = ii.Quantity,
                        UnitPrice = ii.UnitPrice
                    }).ToList() ?? new List<InvoiceItem>()
            };

            await _invoiceService.CreateInvoiceAsync(invoice);

            if (action?.ToLower() == "send")
            {
                try
                {
                    var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice);
                    TempData["SuccessMessage"] = "Invoice has been created and sent successfully.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending invoice {InvoiceId}", invoice.Id);
                    TempData["WarningMessage"] = "Invoice was created but there was an error sending it.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Invoice has been saved as a draft.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            await PopulateClientsAndPhotoShootsAsync();

            var vm = new CreateInvoiceViewModel
            {
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                Amount = invoice.Amount,
                Tax = invoice.Tax,
                Notes = invoice.Notes,
                ClientId = invoice.ClientProfileId ?? 0,
                PhotoShootId = invoice.PhotoShootId,
                InvoiceItems = invoice.InvoiceItems?.Select(ii => new InvoiceItemVM
                {
                    Description = ii.Description,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice
                }).ToList() ?? new List<InvoiceItemVM>()
            };

            // Store the ID in ViewBag for the POST action
            ViewBag.InvoiceId = id;

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CreateInvoiceViewModel vm, string action)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientsAndPhotoShootsAsync();
                ViewBag.InvoiceId = id;
                return View("Create", vm);
            }

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            invoice.InvoiceNumber = vm.InvoiceNumber;
            invoice.InvoiceDate = vm.InvoiceDate;
            invoice.DueDate = vm.DueDate;
            invoice.Status = action?.ToLower() == "send" ? InvoiceStatus.Pending : vm.Status;
            invoice.Amount = vm.Amount;
            invoice.Tax = vm.Tax;
            invoice.Notes = vm.Notes;
            invoice.ClientProfileId = vm.ClientId;
            invoice.PhotoShootId = vm.PhotoShootId;
            invoice.InvoiceItems = vm.InvoiceItems?.Where(ii => !string.IsNullOrWhiteSpace(ii.Description))
                .Select(ii => new InvoiceItem
                {
                    Description = ii.Description,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice
                }).ToList() ?? new List<InvoiceItem>();

            await _invoiceService.UpdateInvoiceAsync(invoice);

            TempData["SuccessMessage"] = "Invoice has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            var vm = new InvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                Amount = invoice.Amount,
                Tax = invoice.Tax,
                ClientName = invoice.ClientProfile?.User != null ? $"{invoice.ClientProfile.User.FirstName} {invoice.ClientProfile.User.LastName}" : "Unknown Client",
                ClientEmail = invoice.ClientProfile?.User?.Email ?? "No Email"
            };

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            // Assuming you have an UpdateInvoiceAsync method that can handle the delete
            // Or you need to add a DeleteInvoiceAsync method to IInvoiceService
            invoice.Status = InvoiceStatus.Draft; // Mark as deleted or implement proper delete
            await _invoiceService.UpdateInvoiceAsync(invoice);

            TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} has been deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateClientsAndPhotoShootsAsync()
        {
            var clients = await _clientService.GetAllClientsAsync();
            ViewBag.ClientId = new SelectList(clients, "Id", "FullName");

            var photoShoots = await _photoShootService.GetAllPhotoShootsAsync();
            ViewBag.PhotoShootList = photoShoots;
            ViewBag.PhotoShootId = new SelectList(photoShoots, "Id", "Title");
        }

        private string GenerateInvoiceNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"INV-{timestamp}-{random}";
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Preview(string id, [FromQuery] decimal? amount = null, [FromQuery] decimal? tax = null)
        {
            Invoice? invoice = await _invoiceService.GetInvoiceByNumberAsync(id);

            if (invoice == null && amount.HasValue)
            {
                invoice = new Invoice
                {
                    InvoiceNumber = id,
                    InvoiceDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(30),
                    Amount = amount.Value,
                    Tax = tax ?? 0m,
                    Status = InvoiceStatus.Draft
                };
            }

            if (invoice == null) return NotFound();

            try
            {
                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview PDF for invoice {InvoiceId}", invoice.Id);
                return StatusCode(500, "Error generating PDF preview.");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Download(string id)
        {
            var invoice = await _invoiceService.GetInvoiceByNumberAsync(id);
            if (invoice == null) return NotFound();

            try
            {
                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice);
                return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceNumber}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", invoice.Id);
                return StatusCode(500, "Error generating PDF");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            await _invoiceService.UpdateInvoiceStatusAsync(id, InvoiceStatus.Paid, DateTime.Now);

            TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} marked as Paid.";
            return RedirectToAction(nameof(Index));
        }
    }
}
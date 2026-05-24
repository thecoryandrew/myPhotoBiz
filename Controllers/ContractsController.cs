using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBadgeService _badgeService;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(ApplicationDbContext context, IBadgeService badgeService, ILogger<ContractsController> logger)
        {
            _context = context;
            _badgeService = badgeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var contracts = await _context.Contracts
                    .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                    .Include(c => c.PhotoShoot)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToListAsync();

                return View(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contracts");
                TempData["Error"] = "An error occurred while loading contracts.";
                return View(new List<Contract>());
            }
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateContractViewModel
            {
                AvailableClients = await GetClientsAsync(),
                AvailablePhotoShoots = await GetPhotoShootsAsync(),
                AvailableBadges = await GetBadgesAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContractViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? pdfPath = null;
                    if (model.PdfFile != null && model.PdfFile.Length > 0)
                    {
                        pdfPath = await SavePdfFileAsync(model.PdfFile);
                    }

                    var contract = new Contract
                    {
                        Title = model.Title,
                        Content = model.Content,
                        PdfFilePath = pdfPath,
                        ClientProfileId = model.ClientId,
                        PhotoShootId = model.PhotoShootId,
                        CreatedDate = DateTime.UtcNow,
                        Status = ContractStatus.Draft,
                        AwardBadgeOnSign = model.AwardBadgeOnSign,
                        BadgeToAwardId = model.BadgeToAwardId
                    };

                    _context.Contracts.Add(contract);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Contract created successfully!";
                    return RedirectToAction(nameof(Details), new { id = contract.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating contract");
                    ModelState.AddModelError("", "An error occurred while creating the contract.");
                }
            }

            model.AvailableClients = await GetClientsAsync();
            model.AvailablePhotoShoots = await GetPhotoShootsAsync();
            model.AvailableBadges = await GetBadgesAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                .Include(c => c.PhotoShoot)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return NotFound();

            var viewModel = new EditContractViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Content = contract.Content ?? "",
                ClientId = contract.ClientProfileId,
                PhotoShootId = contract.PhotoShootId,
                Status = contract.Status,
                CreatedDate = contract.CreatedDate,
                AvailableClients = await GetClientsAsync(),
                AvailablePhotoShoots = await GetPhotoShootsAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditContractViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var contract = await _context.Contracts.FindAsync(id);
                    if (contract == null)
                        return NotFound();

                    contract.Title = model.Title;
                    contract.Content = model.Content;
                    contract.ClientProfileId = model.ClientId;
                    contract.PhotoShootId = model.PhotoShootId;
                    contract.Status = model.Status;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Contract updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = contract.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating contract");
                    ModelState.AddModelError("", "An error occurred while updating the contract.");
                }
            }

            model.AvailableClients = await GetClientsAsync();
            model.AvailablePhotoShoots = await GetPhotoShootsAsync();
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                .Include(c => c.PhotoShoot)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return NotFound();

            var viewModel = new ContractDetailsViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Content = contract.Content,
                CreatedDate = contract.CreatedDate,
                SignedDate = contract.SignedDate,
                SentDate = contract.SentDate,
                SignatureImagePath = contract.SignatureImagePath,
                Status = contract.Status,
                ClientId = contract.ClientProfileId,
                ClientName = contract.ClientProfile?.User != null ? $"{contract.ClientProfile.User.FirstName} {contract.ClientProfile.User.LastName}" : null,
                ClientEmail = contract.ClientProfile?.User?.Email,
                PhotoShootId = contract.PhotoShootId,
                PhotoShootTitle = contract.PhotoShoot?.Title
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Sign(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return NotFound();

            if (contract.Status == ContractStatus.Signed)
            {
                TempData["Error"] = "This contract has already been signed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new SignContractViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Content = contract.Content,
                ClientName = contract.ClientProfile?.User != null ? $"{contract.ClientProfile.User.FirstName} {contract.ClientProfile.User.LastName}" : null,
                CreatedDate = contract.CreatedDate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(int id, string signatureBase64)
        {
            var contract = await _context.Contracts
                .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                .Include(c => c.BadgeToAward)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return NotFound();

            if (contract.Status == ContractStatus.Signed)
            {
                TempData["Error"] = "This contract has already been signed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var signaturePath = SaveSignature(signatureBase64);
                contract.SignatureImagePath = signaturePath;
                contract.SignedDate = DateTime.UtcNow;
                contract.Status = ContractStatus.Signed;

                await _context.SaveChangesAsync();

                // Award badge if configured
                if (contract.AwardBadgeOnSign && contract.BadgeToAwardId.HasValue && contract.ClientProfileId.HasValue)
                {
                    await _badgeService.AwardBadgeAsync(contract.ClientProfileId.Value, contract.BadgeToAwardId.Value,
                        contract.Id, "Awarded by contract signature");
                    TempData["Success"] = $"Contract signed successfully! Badge '{contract.BadgeToAward?.Name}' awarded!";
                }
                else
                {
                    TempData["Success"] = "Contract signed successfully!";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing contract");
                TempData["Error"] = "An error occurred while signing the contract.";
                return RedirectToAction(nameof(Sign), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                    return NotFound();

                // Delete signature file if exists
                if (!string.IsNullOrEmpty(contract.SignatureImagePath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contract.SignatureImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Contract deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract");
                TempData["Error"] = "An error occurred while deleting the contract.";
                return RedirectToAction(nameof(Index));
            }
        }

        private string SaveSignature(string base64)
        {
            // Ensure directory exists
            var signaturesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "signatures");
            if (!Directory.Exists(signaturesDir))
            {
                Directory.CreateDirectory(signaturesDir);
            }

            // Extract base64 data and save
            var base64Data = base64.Contains(',') ? base64.Split(',')[1] : base64;
            var bytes = Convert.FromBase64String(base64Data);
            var fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(signaturesDir, fileName);

            System.IO.File.WriteAllBytes(filePath, bytes);

            return $"/signatures/{fileName}";
        }

        private async Task<List<ClientSelectionViewModel>> GetClientsAsync()
        {
            return await _context.ClientProfiles
                .Include(c => c.User)
                .OrderBy(c => c.User.FirstName)
                .ThenBy(c => c.User.LastName)
                .Select(c => new ClientSelectionViewModel
                {
                    Id = c.Id,
                    FullName = $"{c.User.FirstName} {c.User.LastName}",
                    Email = c.User.Email,
                    PhoneNumber = c.PhoneNumber
                })
                .ToListAsync();
        }

        private async Task<List<PhotoShootSelectionViewModel>> GetPhotoShootsAsync()
        {
            return await _context.PhotoShoots
                .Include(ps => ps.ClientProfile).ThenInclude(cp => cp.User)
                .OrderByDescending(ps => ps.ScheduledDate)
                .Select(ps => new PhotoShootSelectionViewModel
                {
                    Id = ps.Id,
                    Title = ps.Title,
                    ShootDate = ps.ScheduledDate,
                    ClientName = $"{ps.ClientProfile.User.FirstName} {ps.ClientProfile.User.LastName}"
                })
                .ToListAsync();
        }

        private async Task<List<BadgeSelectionViewModel>> GetBadgesAsync()
        {
            return await _context.Badges
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BadgeSelectionViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Icon = b.Icon,
                    Color = b.Color
                })
                .ToListAsync();
        }

        private async Task<string> SavePdfFileAsync(IFormFile pdfFile)
        {
            // Ensure directory exists
            var contractsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
            if (!Directory.Exists(contractsDir))
            {
                Directory.CreateDirectory(contractsDir);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(pdfFile.FileName)}";
            var filePath = Path.Combine(contractsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }

            return $"/uploads/contracts/{fileName}";
        }
    }
}
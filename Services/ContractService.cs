using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public class ContractService : IContractService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContractService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IActivityService _activityService;

        public ContractService(
            ApplicationDbContext context,
            ILogger<ContractService> logger,
            IWebHostEnvironment env,
            IActivityService activityService)
        {
            _context = context;
            _logger = logger;
            _env = env;
            _activityService = activityService;
        }

        public async Task<List<Contract>> GetAllContractsAsync()
        {
            return await _context.Contracts
                .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                .Include(c => c.PhotoShoot)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<Contract?> GetContractByIdAsync(int id)
        {
            return await _context.Contracts
                .Include(c => c.ClientProfile).ThenInclude(cp => cp.User)
                .Include(c => c.PhotoShoot)
                .Include(c => c.BadgeToAward)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Contract> CreateContractAsync(CreateContractViewModel model)
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

            _logger.LogInformation("Contract {ContractId} created with title {Title}", contract.Id, contract.Title);

            return contract;
        }

        public async Task<Contract> UpdateContractAsync(EditContractViewModel model)
        {
            var contract = await _context.Contracts.FindAsync(model.Id)
                ?? throw new KeyNotFoundException($"Contract with ID {model.Id} not found.");

            contract.Title = model.Title;
            contract.Content = model.Content;
            contract.ClientProfileId = model.ClientId;
            contract.PhotoShootId = model.PhotoShootId;
            contract.Status = model.Status;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Contract {ContractId} updated", contract.Id);

            return contract;
        }

        public async Task<bool> DeleteContractAsync(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
                return false;

            // Delete signature file if exists
            if (!string.IsNullOrEmpty(contract.SignatureImagePath))
            {
                var filePath = Path.Combine(_env.WebRootPath, contract.SignatureImagePath.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted signature file for contract {ContractId}", id);
                }
            }

            // Delete PDF file if exists
            if (!string.IsNullOrEmpty(contract.PdfFilePath))
            {
                var pdfPath = Path.Combine(_env.WebRootPath, contract.PdfFilePath.TrimStart('/'));
                if (File.Exists(pdfPath))
                {
                    File.Delete(pdfPath);
                    _logger.LogInformation("Deleted PDF file for contract {ContractId}", id);
                }
            }

            var contractTitle = contract.Title;
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contract {ContractId} deleted", id);

            // Audit log
            await _activityService.LogActivityAsync(
                "Deleted",
                "Contract",
                id,
                contractTitle,
                $"Contract '{contractTitle}' was deleted");

            return true;
        }

        public async Task<string?> SignContractAsync(int id, string signatureBase64)
        {
            var contract = await _context.Contracts
                .Include(c => c.BadgeToAward)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Contract with ID {id} not found.");

            if (contract.Status == ContractStatus.Signed)
                throw new InvalidOperationException("This contract has already been signed.");

            var signaturePath = SaveSignature(signatureBase64);
            contract.SignatureImagePath = signaturePath;
            contract.SignedDate = DateTime.UtcNow;
            contract.Status = ContractStatus.Signed;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Contract {ContractId} signed", id);

            // Award badge if configured
            string? badgeName = null;
            if (contract.AwardBadgeOnSign && contract.BadgeToAwardId.HasValue && contract.ClientProfileId.HasValue)
            {
                await AwardBadgeToClientAsync(contract.ClientProfileId.Value, contract.BadgeToAwardId.Value, contract.Id);
                badgeName = contract.BadgeToAward?.Name;
            }

            return badgeName;
        }

        public async Task<List<ClientSelectionViewModel>> GetClientsAsync()
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

        public async Task<List<PhotoShootSelectionViewModel>> GetPhotoShootsAsync()
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

        public async Task<List<BadgeSelectionViewModel>> GetBadgesAsync()
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

        private string SaveSignature(string base64)
        {
            var signaturesDir = Path.Combine(_env.WebRootPath, "signatures");
            if (!Directory.Exists(signaturesDir))
            {
                Directory.CreateDirectory(signaturesDir);
            }

            var base64Data = base64.Contains(',') ? base64.Split(',')[1] : base64;
            var bytes = Convert.FromBase64String(base64Data);
            var fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(signaturesDir, fileName);

            File.WriteAllBytes(filePath, bytes);

            _logger.LogInformation("Signature saved to {FileName}", fileName);

            return $"/signatures/{fileName}";
        }

        private async Task<string> SavePdfFileAsync(IFormFile pdfFile)
        {
            var contractsDir = Path.Combine(_env.WebRootPath, "uploads", "contracts");
            if (!Directory.Exists(contractsDir))
            {
                Directory.CreateDirectory(contractsDir);
            }

            // Generate unique filename with sanitized extension only
            var safeExt = Path.GetExtension(Path.GetFileName(pdfFile.FileName)) ?? ".pdf";
            var fileName = $"{Guid.NewGuid()}{safeExt}";
            var filePath = Path.Combine(contractsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }

            _logger.LogInformation("PDF file saved as {FileName}", fileName);

            return $"/uploads/contracts/{fileName}";
        }

        private async Task AwardBadgeToClientAsync(int clientProfileId, int badgeId, int? contractId = null)
        {
            var existingBadge = await _context.ClientBadges
                .FirstOrDefaultAsync(cb => cb.ClientProfileId == clientProfileId && cb.BadgeId == badgeId);

            if (existingBadge == null)
            {
                var clientBadge = new ClientBadge
                {
                    ClientProfileId = clientProfileId,
                    BadgeId = badgeId,
                    ContractId = contractId,
                    EarnedDate = DateTime.UtcNow,
                    Notes = contractId.HasValue ? "Awarded by contract signature" : null
                };

                _context.ClientBadges.Add(clientBadge);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Badge {BadgeId} awarded to client profile {ClientProfileId}", badgeId, clientProfileId);
            }
            else
            {
                _logger.LogInformation("Badge {BadgeId} already exists for client profile {ClientProfileId}, skipping", badgeId, clientProfileId);
            }
        }
    }
}

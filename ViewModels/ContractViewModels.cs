using System.ComponentModel.DataAnnotations;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.ViewModels
{
    public class CreateContractViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Contract Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Contract Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Upload PDF Contract")]
        public IFormFile? PdfFile { get; set; }

        [Display(Name = "Client")]
        public int? ClientId { get; set; }

        [Display(Name = "Photo Shoot")]
        public int? PhotoShootId { get; set; }

        [Display(Name = "Award Badge When Signed")]
        public bool AwardBadgeOnSign { get; set; } = false;

        [Display(Name = "Badge to Award")]
        public int? BadgeToAwardId { get; set; }

        public List<ClientSelectionViewModel> AvailableClients { get; set; } = new();
        public List<PhotoShootSelectionViewModel> AvailablePhotoShoots { get; set; } = new();
        public List<BadgeSelectionViewModel> AvailableBadges { get; set; } = new();
    }

    public class EditContractViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        [Display(Name = "Upload New PDF Contract")]
        public IFormFile? PdfFile { get; set; }

        public string? ExistingPdfPath { get; set; }

        public int? ClientId { get; set; }
        public int? PhotoShootId { get; set; }

        public ContractStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Award Badge When Signed")]
        public bool AwardBadgeOnSign { get; set; } = false;

        [Display(Name = "Badge to Award")]
        public int? BadgeToAwardId { get; set; }

        public List<ClientSelectionViewModel> AvailableClients { get; set; } = new();
        public List<PhotoShootSelectionViewModel> AvailablePhotoShoots { get; set; } = new();
        public List<BadgeSelectionViewModel> AvailableBadges { get; set; } = new();
    }

    public class ContractDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? PdfFilePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SignedDate { get; set; }
        public DateTime? SentDate { get; set; }
        public string? SignatureImagePath { get; set; }
        public ContractStatus Status { get; set; }

        public bool AwardBadgeOnSign { get; set; }
        public string? BadgeToAwardName { get; set; }

        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }

        public int? PhotoShootId { get; set; }
        public string? PhotoShootTitle { get; set; }
    }

    public class SignContractViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? PdfFilePath { get; set; }
        public string? ClientName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool WillAwardBadge { get; set; }
        public string? BadgeName { get; set; }
    }

    public class ClientSelectionViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Name => FullName; // Alias for FullName
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsSelected { get; set; }
    }

    public class PhotoShootSelectionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ShootDate { get; set; }
        public string? ClientName { get; set; }
    }

    public class BadgeSelectionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string Color { get; set; } = "#6c757d";
    }
}

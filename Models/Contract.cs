using System.ComponentModel.DataAnnotations;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Models
{
    public class Contract
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; } // HTML or Markdown

        [StringLength(500)]
        public string? PdfFilePath { get; set; } // Path to uploaded PDF file

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? SignedDate { get; set; }
        public DateTime? SentDate { get; set; }

        public string? SignatureImagePath { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        // Badge configuration
        public bool AwardBadgeOnSign { get; set; } = false;
        public int? BadgeToAwardId { get; set; }

        // Foreign keys
        public int? ClientProfileId { get; set; }
        public int? PhotoShootId { get; set; }

        // Navigation properties
        public virtual ClientProfile? ClientProfile { get; set; }
        public virtual PhotoShoot? PhotoShoot { get; set; }
        public virtual Badge? BadgeToAward { get; set; }
        public virtual ICollection<ClientBadge> ClientBadges { get; set; } = new List<ClientBadge>();
    }
}
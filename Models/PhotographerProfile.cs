using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models
{
    public class PhotographerProfile
    {
        public int Id { get; set; }

        // 1:1 relationship with ApplicationUser
        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Photographer-specific data
        [StringLength(2000)]
        public string? Bio { get; set; }

        [StringLength(500)]
        public string? Specialties { get; set; }

        [Url]
        [StringLength(500)]
        public string? PortfolioUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? HourlyRate { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PhotoShoot> AssignedPhotoShoots { get; set; } = new List<PhotoShoot>();

        // Computed properties for convenience
        public string FullName => User != null ? $"{User.FirstName} {User.LastName}" : string.Empty;
        public string Email => User?.Email ?? string.Empty;
    }
}


using System.ComponentModel.DataAnnotations;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Models
{
    public class PhotoShoot
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Range(0, 24, ErrorMessage = "Duration hours must be between 0 and 24")]
        public int DurationHours { get; set; } = 2;

        [Range(0, 59, ErrorMessage = "Duration minutes must be between 0 and 59")]
        public int DurationMinutes { get; set; } = 0;

        [Required]
        public required string Location { get; set; } = string.Empty;


        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public PhotoShootStatus Status { get; set; } = PhotoShootStatus.InProgress;

        // Foreign keys
        [Required]
        public int ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;

        public string? PhotographerId { get; set; }
        public virtual ApplicationUser? Photographer { get; set; }

        // Navigation properties
        public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
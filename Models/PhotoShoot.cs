
using System.ComponentModel.DataAnnotations;
using MyPhotoBiz.Models.ViewModels;
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

        public int DurationHours { get; set; } = 2;

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
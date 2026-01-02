
using System.ComponentModel.DataAnnotations;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Models
{
    // TODO: [HIGH] Dual photographer FKs cause data sync issues:
    //       - PhotographerId (string) references ApplicationUser.Id
    //       - PhotographerProfileId (int) references PhotographerProfile.Id
    //       Consolidate to single PhotographerProfileId
    // TODO: [MEDIUM] Add soft delete (IsDeleted flag) to preserve history
    // TODO: [MEDIUM] Add CreatedBy/UpdatedBy audit fields
    // TODO: [FEATURE] Add recurring shoot support (RecurrencePattern)
    // TODO: [FEATURE] Add shoot type categorization (Wedding, Portrait, Event, etc.)
    public class PhotoShoot
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Display(Name = "Start Date & Time")]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [Display(Name = "End Date & Time")]
        public DateTime EndTime { get; set; }

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

        // Foreign keys - Client relationship via ClientProfile
        [Required]
        public int ClientProfileId { get; set; }
        public virtual ClientProfile ClientProfile { get; set; } = null!;

        // Photographer can be assigned via ApplicationUser or PhotographerProfile
        public string? PhotographerId { get; set; }
        public virtual ApplicationUser? Photographer { get; set; }

        public int? PhotographerProfileId { get; set; }
        public virtual PhotographerProfile? PhotographerProfile { get; set; }

        // Navigation properties
        public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
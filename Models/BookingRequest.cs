using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Models
{
    /// <summary>
    /// Represents a client's request to book a photo shoot
    /// </summary>
    public class BookingRequest
    {
        public int Id { get; set; }

        // Unique booking reference number
        [Required]
        [StringLength(20)]
        [Display(Name = "Booking Reference")]
        public string BookingReference { get; set; } = string.Empty;

        // Client making the request
        [Required]
        public int ClientProfileId { get; set; }
        public virtual ClientProfile ClientProfile { get; set; } = null!;

        // Requested photographer (optional - can be assigned later)
        public int? PhotographerProfileId { get; set; }
        public virtual PhotographerProfile? PhotographerProfile { get; set; }

        // Optional: Link to service package
        public int? ServicePackageId { get; set; }
        public virtual ServicePackage? ServicePackage { get; set; }

        // Booking details
        [Required]
        [StringLength(200)]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Preferred Date")]
        public DateTime PreferredDate { get; set; }

        [Display(Name = "Alternative Date")]
        public DateTime? AlternativeDate { get; set; }

        [Required]
        [Display(Name = "Preferred Start Time")]
        public TimeSpan PreferredStartTime { get; set; }

        [Display(Name = "Estimated Duration (hours)")]
        [Range(0.5, 12)]
        public decimal EstimatedDurationHours { get; set; } = 2;

        [Required]
        [StringLength(500)]
        public string Location { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Special Requirements")]
        public string? SpecialRequirements { get; set; }

        // Contact information (for non-registered clients or quick bookings)
        [StringLength(100)]
        [Display(Name = "Contact Name")]
        public string? ContactName { get; set; }

        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        // Pricing
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Estimated Price")]
        public decimal? EstimatedPrice { get; set; }

        // Booking status
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [StringLength(1000)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        [StringLength(500)]
        [Display(Name = "Decline Reason")]
        public string? DeclineReason { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedDate { get; set; }
        public DateTime? DeclinedDate { get; set; }
        public DateTime? CancelledDate { get; set; }

        // Converted PhotoShoot (when booking is confirmed and converted)
        public int? PhotoShootId { get; set; }
        public virtual PhotoShoot? PhotoShoot { get; set; }

        // Navigation for availability slot
        public virtual ICollection<PhotographerAvailability> AvailabilitySlots { get; set; } = new List<PhotographerAvailability>();

        // Helper methods
        public static string GenerateBookingReference()
        {
            var timestamp = DateTime.UtcNow.ToString("yyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"BK-{timestamp}-{random}";
        }
    }
}

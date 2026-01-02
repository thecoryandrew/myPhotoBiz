using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models
{
    /// <summary>
    /// Represents a photographer's available time slot for booking
    /// </summary>
    public class PhotographerAvailability
    {
        public int Id { get; set; }

        // Photographer relationship
        [Required]
        public int PhotographerProfileId { get; set; }
        public virtual PhotographerProfile PhotographerProfile { get; set; } = null!;

        // Availability time slot
        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        // Recurring availability (optional)
        public bool IsRecurring { get; set; } = false;

        [Display(Name = "Day of Week")]
        public DayOfWeek? RecurringDayOfWeek { get; set; }

        // Slot status
        public bool IsBooked { get; set; } = false;
        public bool IsBlocked { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property - if this slot is booked
        public int? BookingRequestId { get; set; }
        public virtual BookingRequest? BookingRequest { get; set; }

        // Computed properties
        [NotMapped]
        public TimeSpan Duration => EndTime - StartTime;

        [NotMapped]
        public bool IsAvailable => !IsBooked && !IsBlocked && StartTime > DateTime.UtcNow;
    }
}

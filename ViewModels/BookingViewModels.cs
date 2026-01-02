using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyPhotoBiz.ViewModels
{
    public class CreateBookingViewModel
    {
        public int? ServicePackageId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Preferred Date")]
        [DataType(DataType.Date)]
        public DateTime PreferredDate { get; set; }

        [Display(Name = "Alternative Date")]
        [DataType(DataType.Date)]
        public DateTime? AlternativeDate { get; set; }

        [Required]
        [Display(Name = "Preferred Start Time")]
        [DataType(DataType.Time)]
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

        // Dropdown data
        public List<SelectListItem> Packages { get; set; } = new();
    }

    public class BookingDetailsViewModel
    {
        public int Id { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public TimeSpan PreferredStartTime { get; set; }
        public decimal EstimatedDurationHours { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? SpecialRequirements { get; set; }
        public string? PackageName { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ConfirmedDate { get; set; }
        public string? PhotographerName { get; set; }
        public string? AdminNotes { get; set; }
        public string? DeclineReason { get; set; }
        public int? PhotoShootId { get; set; }
    }

    public class AvailabilitySlotViewModel
    {
        public int PhotographerProfileId { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        public bool IsRecurring { get; set; }
        public DayOfWeek? RecurringDayOfWeek { get; set; }
        public DateTime? RecurringUntil { get; set; }

        public string? Notes { get; set; }
    }
}

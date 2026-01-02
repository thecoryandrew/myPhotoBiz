using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IBookingService
    {
        // Booking Requests
        Task<IEnumerable<BookingRequest>> GetAllBookingRequestsAsync();
        Task<IEnumerable<BookingRequest>> GetBookingRequestsByStatusAsync(BookingStatus status);
        Task<IEnumerable<BookingRequest>> GetBookingRequestsByClientAsync(int clientProfileId);
        Task<IEnumerable<BookingRequest>> GetPendingBookingRequestsAsync();
        Task<BookingRequest?> GetBookingRequestByIdAsync(int id);
        Task<BookingRequest?> GetBookingRequestByReferenceAsync(string reference);
        Task<BookingRequest> CreateBookingRequestAsync(BookingRequest request);
        Task<BookingRequest> UpdateBookingRequestAsync(BookingRequest request);
        Task<bool> DeleteBookingRequestAsync(int id);

        // Booking Status Management
        Task<BookingRequest> ConfirmBookingAsync(int id, int? photographerProfileId = null, string? adminNotes = null);
        Task<BookingRequest> DeclineBookingAsync(int id, string reason);
        Task<BookingRequest> CancelBookingAsync(int id);
        Task<PhotoShoot> ConvertToPhotoShootAsync(int bookingId);

        // Photographer Availability
        Task<IEnumerable<PhotographerAvailability>> GetPhotographerAvailabilityAsync(int photographerProfileId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<PhotographerAvailability>> GetAvailableSlotsAsync(DateTime date, int? photographerProfileId = null);
        Task<PhotographerAvailability> CreateAvailabilitySlotAsync(PhotographerAvailability slot);
        Task<IEnumerable<PhotographerAvailability>> CreateRecurringAvailabilityAsync(int photographerProfileId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, DateTime untilDate);
        Task<bool> DeleteAvailabilitySlotAsync(int id);
        Task<bool> BlockTimeSlotAsync(int photographerProfileId, DateTime startTime, DateTime endTime, string? notes = null);

        // Statistics
        Task<int> GetPendingBookingsCountAsync();
        Task<int> GetBookingsCountByStatusAsync(BookingStatus status);
    }
}

using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    // TODO: [CRITICAL] ConvertToPhotoShootAsync allows conversion without photographer assigned
    // TODO: [HIGH] Add past date validation in CreateBookingRequestAsync
    // TODO: [HIGH] ConfirmBookingAsync should require photographer assignment
    // TODO: [HIGH] Auto-generate draft Invoice when converting to PhotoShoot
    // TODO: [HIGH] Auto-generate draft Contract when converting to PhotoShoot
    // TODO: [MEDIUM] Status "Completed" is confusing - consider "Converted" for clarity
    // TODO: [MEDIUM] AlternativeDate field is never used in workflow
    // TODO: [MEDIUM] Availability slots not linked to PhotoShoot after conversion
    // TODO: [MEDIUM] Wrap conversion in database transaction for data integrity
    // TODO: [FEATURE] Add email notification on booking status changes
    // TODO: [FEATURE] Add calendar integration for photographer schedules
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityService _activityService;

        public BookingService(ApplicationDbContext context, IActivityService activityService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        }

        #region Booking Requests

        public async Task<IEnumerable<BookingRequest>> GetAllBookingRequestsAsync()
        {
            return await _context.BookingRequests
                .Include(br => br.ClientProfile)
                    .ThenInclude(cp => cp.User)
                .Include(br => br.PhotographerProfile)
                .Include(br => br.ServicePackage)
                .OrderByDescending(br => br.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingRequest>> GetBookingRequestsByStatusAsync(BookingStatus status)
        {
            return await _context.BookingRequests
                .Include(br => br.ClientProfile)
                    .ThenInclude(cp => cp.User)
                .Include(br => br.PhotographerProfile)
                .Include(br => br.ServicePackage)
                .Where(br => br.Status == status)
                .OrderByDescending(br => br.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingRequest>> GetBookingRequestsByClientAsync(int clientProfileId)
        {
            return await _context.BookingRequests
                .Include(br => br.PhotographerProfile)
                .Include(br => br.ServicePackage)
                .Where(br => br.ClientProfileId == clientProfileId)
                .OrderByDescending(br => br.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingRequest>> GetPendingBookingRequestsAsync()
        {
            return await GetBookingRequestsByStatusAsync(BookingStatus.Pending);
        }

        public async Task<BookingRequest?> GetBookingRequestByIdAsync(int id)
        {
            return await _context.BookingRequests
                .Include(br => br.ClientProfile)
                    .ThenInclude(cp => cp.User)
                .Include(br => br.PhotographerProfile)
                    .ThenInclude(pp => pp!.User)
                .Include(br => br.ServicePackage)
                .Include(br => br.PhotoShoot)
                .FirstOrDefaultAsync(br => br.Id == id);
        }

        public async Task<BookingRequest?> GetBookingRequestByReferenceAsync(string reference)
        {
            return await _context.BookingRequests
                .Include(br => br.ClientProfile)
                .Include(br => br.PhotographerProfile)
                .Include(br => br.ServicePackage)
                .FirstOrDefaultAsync(br => br.BookingReference == reference);
        }

        public async Task<BookingRequest> CreateBookingRequestAsync(BookingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Generate booking reference if not provided
            if (string.IsNullOrEmpty(request.BookingReference))
            {
                request.BookingReference = BookingRequest.GenerateBookingReference();
            }

            // Validate client exists
            var clientExists = await _context.ClientProfiles.AnyAsync(cp => cp.Id == request.ClientProfileId);
            if (!clientExists)
                throw new InvalidOperationException($"Client with Id {request.ClientProfileId} does not exist.");

            request.CreatedDate = DateTime.UtcNow;
            request.UpdatedDate = DateTime.UtcNow;
            request.Status = BookingStatus.Pending;

            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Created", "BookingRequest", request.Id,
                $"Booking {request.BookingReference}",
                $"New booking request for {request.EventType} on {request.PreferredDate:d}");

            return request;
        }

        public async Task<BookingRequest> UpdateBookingRequestAsync(BookingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var existing = await _context.BookingRequests.FindAsync(request.Id);
            if (existing == null)
                throw new InvalidOperationException("Booking request not found.");

            request.UpdatedDate = DateTime.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(request);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "BookingRequest", request.Id,
                $"Booking {request.BookingReference}",
                $"Updated booking request details");

            return existing;
        }

        public async Task<bool> DeleteBookingRequestAsync(int id)
        {
            var request = await _context.BookingRequests.FindAsync(id);
            if (request == null) return false;

            _context.BookingRequests.Remove(request);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Deleted", "BookingRequest", id,
                $"Booking {request.BookingReference}",
                "Booking request deleted");

            return true;
        }

        #endregion

        #region Booking Status Management

        public async Task<BookingRequest> ConfirmBookingAsync(int id, int? photographerProfileId = null, string? adminNotes = null)
        {
            var request = await _context.BookingRequests.FindAsync(id);
            if (request == null)
                throw new InvalidOperationException("Booking request not found.");

            if (request.Status != BookingStatus.Pending)
                throw new InvalidOperationException("Only pending bookings can be confirmed.");

            request.Status = BookingStatus.Confirmed;
            request.ConfirmedDate = DateTime.UtcNow;
            request.UpdatedDate = DateTime.UtcNow;

            if (photographerProfileId.HasValue)
                request.PhotographerProfileId = photographerProfileId.Value;

            if (!string.IsNullOrEmpty(adminNotes))
                request.AdminNotes = adminNotes;

            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "BookingRequest", id,
                $"Booking {request.BookingReference}",
                "Booking confirmed");

            return request;
        }

        public async Task<BookingRequest> DeclineBookingAsync(int id, string reason)
        {
            var request = await _context.BookingRequests.FindAsync(id);
            if (request == null)
                throw new InvalidOperationException("Booking request not found.");

            if (request.Status != BookingStatus.Pending)
                throw new InvalidOperationException("Only pending bookings can be declined.");

            request.Status = BookingStatus.Declined;
            request.DeclinedDate = DateTime.UtcNow;
            request.DeclineReason = reason;
            request.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "BookingRequest", id,
                $"Booking {request.BookingReference}",
                $"Booking declined: {reason}");

            return request;
        }

        public async Task<BookingRequest> CancelBookingAsync(int id)
        {
            var request = await _context.BookingRequests.FindAsync(id);
            if (request == null)
                throw new InvalidOperationException("Booking request not found.");

            if (request.Status == BookingStatus.Completed)
                throw new InvalidOperationException("Completed bookings cannot be cancelled.");

            request.Status = BookingStatus.Cancelled;
            request.CancelledDate = DateTime.UtcNow;
            request.UpdatedDate = DateTime.UtcNow;

            // Release any booked availability slots
            var slots = await _context.PhotographerAvailabilities
                .Where(pa => pa.BookingRequestId == id)
                .ToListAsync();

            foreach (var slot in slots)
            {
                slot.IsBooked = false;
                slot.BookingRequestId = null;
            }

            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "BookingRequest", id,
                $"Booking {request.BookingReference}",
                "Booking cancelled");

            return request;
        }

        public async Task<PhotoShoot> ConvertToPhotoShootAsync(int bookingId)
        {
            var request = await _context.BookingRequests
                .Include(br => br.ServicePackage)
                .FirstOrDefaultAsync(br => br.Id == bookingId);

            if (request == null)
                throw new InvalidOperationException("Booking request not found.");

            if (request.Status != BookingStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed bookings can be converted to photo shoots.");

            if (request.PhotoShootId.HasValue)
                throw new InvalidOperationException("This booking has already been converted to a photo shoot.");

            var durationHours = (int)request.EstimatedDurationHours;
            var durationMinutes = (int)((request.EstimatedDurationHours - durationHours) * 60);

            var photoShoot = new PhotoShoot
            {
                Title = $"{request.EventType} - {request.BookingReference}",
                Description = request.SpecialRequirements,
                ScheduledDate = request.PreferredDate.Add(request.PreferredStartTime),
                EndTime = request.PreferredDate.Add(request.PreferredStartTime).AddHours((double)request.EstimatedDurationHours),
                DurationHours = durationHours,
                DurationMinutes = durationMinutes,
                Location = request.Location,
                Price = request.EstimatedPrice ?? request.ServicePackage?.EffectivePrice ?? 0,
                ClientProfileId = request.ClientProfileId,
                PhotographerProfileId = request.PhotographerProfileId,
                Status = PhotoShootStatus.Scheduled,
                Notes = $"Converted from booking {request.BookingReference}",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.PhotoShoots.Add(photoShoot);
            await _context.SaveChangesAsync();

            // Update booking with PhotoShoot reference
            request.PhotoShootId = photoShoot.Id;
            request.Status = BookingStatus.Completed;
            request.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Created", "PhotoShoot", photoShoot.Id,
                photoShoot.Title,
                $"Created from booking {request.BookingReference}");

            return photoShoot;
        }

        #endregion

        #region Photographer Availability

        public async Task<IEnumerable<PhotographerAvailability>> GetPhotographerAvailabilityAsync(
            int photographerProfileId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.PhotographerAvailabilities
                .Include(pa => pa.PhotographerProfile)
                .Where(pa => pa.PhotographerProfileId == photographerProfileId);

            if (fromDate.HasValue)
                query = query.Where(pa => pa.StartTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(pa => pa.EndTime <= toDate.Value);

            return await query.OrderBy(pa => pa.StartTime).ToListAsync();
        }

        public async Task<IEnumerable<PhotographerAvailability>> GetAvailableSlotsAsync(
            DateTime date, int? photographerProfileId = null)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1);

            var query = _context.PhotographerAvailabilities
                .Include(pa => pa.PhotographerProfile)
                    .ThenInclude(pp => pp.User)
                .Where(pa => pa.StartTime >= startOfDay && pa.StartTime < endOfDay)
                .Where(pa => !pa.IsBooked && !pa.IsBlocked);

            if (photographerProfileId.HasValue)
                query = query.Where(pa => pa.PhotographerProfileId == photographerProfileId.Value);

            return await query.OrderBy(pa => pa.StartTime).ToListAsync();
        }

        public async Task<PhotographerAvailability> CreateAvailabilitySlotAsync(PhotographerAvailability slot)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));

            // Validate photographer exists
            var photographerExists = await _context.PhotographerProfiles.AnyAsync(pp => pp.Id == slot.PhotographerProfileId);
            if (!photographerExists)
                throw new InvalidOperationException($"Photographer with Id {slot.PhotographerProfileId} does not exist.");

            // Check for overlapping slots
            var hasOverlap = await _context.PhotographerAvailabilities
                .AnyAsync(pa => pa.PhotographerProfileId == slot.PhotographerProfileId &&
                               pa.StartTime < slot.EndTime && pa.EndTime > slot.StartTime);

            if (hasOverlap)
                throw new InvalidOperationException("This time slot overlaps with an existing availability slot.");

            slot.CreatedDate = DateTime.UtcNow;
            slot.UpdatedDate = DateTime.UtcNow;

            _context.PhotographerAvailabilities.Add(slot);
            await _context.SaveChangesAsync();

            return slot;
        }

        public async Task<IEnumerable<PhotographerAvailability>> CreateRecurringAvailabilityAsync(
            int photographerProfileId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, DateTime untilDate)
        {
            var slots = new List<PhotographerAvailability>();
            var currentDate = DateTime.UtcNow.Date;

            // Find the next occurrence of the specified day of week
            while (currentDate.DayOfWeek != dayOfWeek)
                currentDate = currentDate.AddDays(1);

            while (currentDate <= untilDate)
            {
                var slot = new PhotographerAvailability
                {
                    PhotographerProfileId = photographerProfileId,
                    StartTime = currentDate.Add(startTime),
                    EndTime = currentDate.Add(endTime),
                    IsRecurring = true,
                    RecurringDayOfWeek = dayOfWeek,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                // Check for overlapping slots
                var hasOverlap = await _context.PhotographerAvailabilities
                    .AnyAsync(pa => pa.PhotographerProfileId == photographerProfileId &&
                                   pa.StartTime < slot.EndTime && pa.EndTime > slot.StartTime);

                if (!hasOverlap)
                {
                    _context.PhotographerAvailabilities.Add(slot);
                    slots.Add(slot);
                }

                currentDate = currentDate.AddDays(7);
            }

            await _context.SaveChangesAsync();
            return slots;
        }

        public async Task<bool> DeleteAvailabilitySlotAsync(int id)
        {
            var slot = await _context.PhotographerAvailabilities.FindAsync(id);
            if (slot == null) return false;

            if (slot.IsBooked)
                throw new InvalidOperationException("Cannot delete a booked availability slot.");

            _context.PhotographerAvailabilities.Remove(slot);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BlockTimeSlotAsync(int photographerProfileId, DateTime startTime, DateTime endTime, string? notes = null)
        {
            var slot = new PhotographerAvailability
            {
                PhotographerProfileId = photographerProfileId,
                StartTime = startTime,
                EndTime = endTime,
                IsBlocked = true,
                Notes = notes,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.PhotographerAvailabilities.Add(slot);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Statistics

        public async Task<int> GetPendingBookingsCountAsync()
        {
            return await _context.BookingRequests.CountAsync(br => br.Status == BookingStatus.Pending);
        }

        public async Task<int> GetBookingsCountByStatusAsync(BookingStatus status)
        {
            return await _context.BookingRequests.CountAsync(br => br.Status == status);
        }

        #endregion
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IPackageService _packageService;
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityService _activityService;

        public BookingsController(
            IBookingService bookingService,
            IPackageService packageService,
            IClientService clientService,
            UserManager<ApplicationUser> userManager,
            IActivityService activityService)
        {
            _bookingService = bookingService;
            _packageService = packageService;
            _clientService = clientService;
            _userManager = userManager;
            _activityService = activityService;
        }

        #region Admin/Photographer Views

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Index(BookingStatus? status = null)
        {
            var bookings = status.HasValue
                ? await _bookingService.GetBookingRequestsByStatusAsync(status.Value)
                : await _bookingService.GetAllBookingRequestsAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = await _bookingService.GetPendingBookingsCountAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetBookingRequestByIdAsync(id);
            if (booking == null) return NotFound();

            return View(booking);
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, int? photographerProfileId, string? adminNotes)
        {
            try
            {
                await _bookingService.ConfirmBookingAsync(id, photographerProfileId, adminNotes);
                TempData["Success"] = "Booking confirmed successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id, string reason)
        {
            try
            {
                await _bookingService.DeclineBookingAsync(id, reason);
                TempData["Success"] = "Booking declined.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToShoot(int id)
        {
            try
            {
                var photoShoot = await _bookingService.ConvertToPhotoShootAsync(id);
                TempData["Success"] = "Booking converted to photo shoot successfully.";
                return RedirectToAction("Details", "PhotoShoots", new { id = photoShoot.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _bookingService.DeleteBookingRequestAsync(id);
                TempData["Success"] = "Booking deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Client Booking Portal

        [AllowAnonymous]
        public async Task<IActionResult> Book()
        {
            var packages = await _packageService.GetActivePackagesAsync();
            var model = new CreateBookingViewModel
            {
                PreferredDate = DateTime.Today.AddDays(7),
                PreferredStartTime = new TimeSpan(10, 0, 0),
                EstimatedDurationHours = 2,
                Packages = packages.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} - ${p.EffectivePrice:F2}"
                }).ToList()
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(CreateBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var packages = await _packageService.GetActivePackagesAsync();
                model.Packages = packages.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} - ${p.EffectivePrice:F2}"
                }).ToList();
                return View(model);
            }

            // Get or create client profile
            int clientProfileId;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var clientProfile = await _clientService.GetClientByUserIdAsync(userId!);
                if (clientProfile == null)
                {
                    TempData["Error"] = "Client profile not found.";
                    return RedirectToAction(nameof(Book));
                }
                clientProfileId = clientProfile.Id;
            }
            else
            {
                // For anonymous users, we need them to register or provide contact info
                TempData["Error"] = "Please log in or register to complete your booking.";
                return RedirectToAction(nameof(Book));
            }

            // Calculate estimated price from package if selected
            decimal? estimatedPrice = null;
            if (model.ServicePackageId.HasValue)
            {
                var package = await _packageService.GetPackageByIdAsync(model.ServicePackageId.Value);
                if (package != null)
                {
                    estimatedPrice = package.EffectivePrice;
                }
            }

            var booking = new BookingRequest
            {
                ClientProfileId = clientProfileId,
                ServicePackageId = model.ServicePackageId,
                EventType = model.EventType,
                PreferredDate = model.PreferredDate,
                AlternativeDate = model.AlternativeDate,
                PreferredStartTime = model.PreferredStartTime,
                EstimatedDurationHours = model.EstimatedDurationHours,
                Location = model.Location,
                SpecialRequirements = model.SpecialRequirements,
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                EstimatedPrice = estimatedPrice
            };

            await _bookingService.CreateBookingRequestAsync(booking);

            TempData["Success"] = $"Your booking request has been submitted! Reference: {booking.BookingReference}";
            return RedirectToAction(nameof(Confirmation), new { reference = booking.BookingReference });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Confirmation(string reference)
        {
            var booking = await _bookingService.GetBookingRequestByReferenceAsync(reference);
            if (booking == null) return NotFound();

            return View(booking);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var clientProfile = await _clientService.GetClientByUserIdAsync(userId!);

            if (clientProfile == null)
            {
                return View(Enumerable.Empty<BookingRequest>());
            }

            var bookings = await _bookingService.GetBookingRequestsByClientAsync(clientProfile.Id);
            return View(bookings);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                // Verify client owns this booking
                var userId = _userManager.GetUserId(User);
                var clientProfile = await _clientService.GetClientByUserIdAsync(userId!);
                var booking = await _bookingService.GetBookingRequestByIdAsync(id);

                if (booking == null || clientProfile == null || booking.ClientProfileId != clientProfile.Id)
                {
                    return Forbid();
                }

                await _bookingService.CancelBookingAsync(id);
                TempData["Success"] = "Booking cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(MyBookings));
        }

        #endregion

        #region Availability Management

        [Authorize(Roles = "Admin,Photographer")]
        public async Task<IActionResult> Availability(int? photographerId, DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var slots = await _bookingService.GetAvailableSlotsAsync(selectedDate, photographerId);

            ViewBag.SelectedDate = selectedDate;
            ViewBag.PhotographerId = photographerId;

            return View(slots);
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAvailability(int photographerProfileId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var slot = new PhotographerAvailability
                {
                    PhotographerProfileId = photographerProfileId,
                    StartTime = startTime,
                    EndTime = endTime
                };

                await _bookingService.CreateAvailabilitySlotAsync(slot);
                TempData["Success"] = "Availability slot created.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Availability));
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockTime(int photographerProfileId, DateTime startTime, DateTime endTime, string? notes)
        {
            try
            {
                await _bookingService.BlockTimeSlotAsync(photographerProfileId, startTime, endTime, notes);
                TempData["Success"] = "Time slot blocked.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Availability));
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            try
            {
                await _bookingService.DeleteAvailabilitySlotAsync(id);
                TempData["Success"] = "Availability slot deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Availability));
        }

        #endregion

        #region API Endpoints for Calendar

        [Authorize(Roles = "Admin,Photographer")]
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(DateTime date, int? photographerId)
        {
            var slots = await _bookingService.GetAvailableSlotsAsync(date, photographerId);
            return Json(slots.Select(s => new
            {
                s.Id,
                s.StartTime,
                s.EndTime,
                PhotographerName = s.PhotographerProfile?.FullName,
                s.IsAvailable
            }));
        }

        [Authorize(Roles = "Admin,Photographer")]
        [HttpGet]
        public async Task<IActionResult> GetBookingsCalendar(DateTime start, DateTime end)
        {
            var bookings = await _bookingService.GetAllBookingRequestsAsync();
            var filtered = bookings.Where(b => b.PreferredDate >= start && b.PreferredDate <= end);

            return Json(filtered.Select(b => new
            {
                id = b.Id,
                title = $"{b.EventType} - {b.ClientProfile?.User?.FirstName}",
                start = b.PreferredDate.Add(b.PreferredStartTime),
                end = b.PreferredDate.Add(b.PreferredStartTime).AddHours((double)b.EstimatedDurationHours),
                status = b.Status.ToString(),
                color = b.Status switch
                {
                    BookingStatus.Pending => "#ffc107",
                    BookingStatus.Confirmed => "#28a745",
                    BookingStatus.Declined => "#dc3545",
                    BookingStatus.Cancelled => "#6c757d",
                    BookingStatus.Completed => "#17a2b8",
                    _ => "#007bff"
                }
            }));
        }

        #endregion
    }
}

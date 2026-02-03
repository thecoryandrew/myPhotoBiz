using FluentAssertions;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for Booking system operations
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        _fixture = new TestFixture();
        _bookingService = _fixture.GetService<IBookingService>();
    }

    #region Create Booking Request

    [Fact]
    public async Task CreateBookingRequestAsync_WithValidData_CreatesBookingSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = CreateTestBookingRequest(clientProfile.Id);

        // Act
        var result = await _bookingService.CreateBookingRequestAsync(booking);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ClientProfileId.Should().Be(clientProfile.Id);
        result.Status.Should().Be(BookingStatus.Pending);
        result.BookingReference.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateBookingRequestAsync_WithoutBookingReference_GeneratesReference()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = CreateTestBookingRequest(clientProfile.Id);
        booking.BookingReference = string.Empty;

        // Act
        var result = await _bookingService.CreateBookingRequestAsync(booking);

        // Assert
        result.BookingReference.Should().NotBeNullOrEmpty();
        result.BookingReference.Should().StartWith("BK-");
    }

    [Fact]
    public async Task CreateBookingRequestAsync_WithNonExistentClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var booking = CreateTestBookingRequest(99999);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateBookingRequestAsync(booking));
    }

    [Fact]
    public async Task CreateBookingRequestAsync_SetsCreatedAndUpdatedDates()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = CreateTestBookingRequest(clientProfile.Id);

        // Act
        var result = await _bookingService.CreateBookingRequestAsync(booking);

        // Assert
        result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Read Booking Requests

    [Fact]
    public async Task GetAllBookingRequestsAsync_ReturnsAllBookings()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-001"));
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-002"));

        // Act
        var results = await _bookingService.GetAllBookingRequestsAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllBookingRequestsAsync_OrdersByCreatedDateDescending()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-FIRST"));
        await Task.Delay(10);
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-SECOND"));

        // Act
        var results = (await _bookingService.GetAllBookingRequestsAsync()).ToList();

        // Assert
        results[0].BookingReference.Should().Be("BK-SECOND");
        results[1].BookingReference.Should().Be("BK-FIRST");
    }

    [Fact]
    public async Task GetBookingRequestByIdAsync_WithExistingId_ReturnsBooking()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act
        var result = await _bookingService.GetBookingRequestByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetBookingRequestByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _bookingService.GetBookingRequestByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBookingRequestByReferenceAsync_WithExistingReference_ReturnsBooking()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-UNIQUE-REF"));

        // Act
        var result = await _bookingService.GetBookingRequestByReferenceAsync("BK-UNIQUE-REF");

        // Assert
        result.Should().NotBeNull();
        result!.BookingReference.Should().Be("BK-UNIQUE-REF");
    }

    [Fact]
    public async Task GetBookingRequestsByStatusAsync_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var pending = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-PENDING"));
        var confirmed = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-TO-CONFIRM"));
        await _bookingService.ConfirmBookingAsync(confirmed.Id);

        // Act
        var pendingResults = await _bookingService.GetBookingRequestsByStatusAsync(BookingStatus.Pending);
        var confirmedResults = await _bookingService.GetBookingRequestsByStatusAsync(BookingStatus.Confirmed);

        // Assert
        pendingResults.Should().HaveCount(1);
        pendingResults.First().BookingReference.Should().Be("BK-PENDING");
        confirmedResults.Should().HaveCount(1);
        confirmedResults.First().BookingReference.Should().Be("BK-TO-CONFIRM");
    }

    [Fact]
    public async Task GetBookingRequestsByClientAsync_ReturnsOnlyClientBookings()
    {
        // Arrange
        var client1 = await _fixture.CreateTestClientProfileAsync();
        var user2 = await _fixture.CreateTestUserAsync(email: "client2@example.com", firstName: "Client", lastName: "Two");
        var client2 = await _fixture.CreateTestClientProfileAsync(user2);

        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(client1.Id, "BK-CLIENT1"));
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(client2.Id, "BK-CLIENT2"));

        // Act
        var results = await _bookingService.GetBookingRequestsByClientAsync(client1.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().BookingReference.Should().Be("BK-CLIENT1");
    }

    [Fact]
    public async Task GetPendingBookingRequestsAsync_ReturnsOnlyPendingBookings()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-PENDING-1"));
        var toConfirm = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-TO-CONFIRM"));
        await _bookingService.ConfirmBookingAsync(toConfirm.Id);

        // Act
        var results = await _bookingService.GetPendingBookingRequestsAsync();

        // Assert
        results.Should().HaveCount(1);
        results.All(r => r.Status == BookingStatus.Pending).Should().BeTrue();
    }

    #endregion

    #region Update Booking Request

    [Fact]
    public async Task UpdateBookingRequestAsync_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        var originalUpdatedDate = booking.UpdatedDate;

        await Task.Delay(10);

        booking.Location = "Updated Location";
        booking.SpecialRequirements = "Updated requirements";

        // Act
        var result = await _bookingService.UpdateBookingRequestAsync(booking);

        // Assert
        result.Location.Should().Be("Updated Location");
        result.SpecialRequirements.Should().Be("Updated requirements");
        result.UpdatedDate.Should().BeAfter(originalUpdatedDate);
    }

    [Fact]
    public async Task UpdateBookingRequestAsync_WithNonExistingId_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = CreateTestBookingRequest(clientProfile.Id);
        booking.Id = 99999;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.UpdateBookingRequestAsync(booking));
    }

    #endregion

    #region Delete Booking Request

    [Fact]
    public async Task DeleteBookingRequestAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act
        var result = await _bookingService.DeleteBookingRequestAsync(booking.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _bookingService.GetBookingRequestByIdAsync(booking.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBookingRequestAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _bookingService.DeleteBookingRequestAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Confirm Booking

    [Fact]
    public async Task ConfirmBookingAsync_WithPendingBooking_ConfirmsSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act
        var result = await _bookingService.ConfirmBookingAsync(booking.Id);

        // Assert
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.ConfirmedDate.Should().NotBeNull();
        result.ConfirmedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ConfirmBookingAsync_WithPhotographer_AssignsPhotographer()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act
        var result = await _bookingService.ConfirmBookingAsync(booking.Id, photographerProfile.Id, "Test admin notes");

        // Assert
        result.PhotographerProfileId.Should().Be(photographerProfile.Id);
        result.AdminNotes.Should().Be("Test admin notes");
    }

    [Fact]
    public async Task ConfirmBookingAsync_WithNonPendingBooking_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);

        // Act & Assert - Try to confirm again
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.ConfirmBookingAsync(booking.Id));
    }

    [Fact]
    public async Task ConfirmBookingAsync_WithNonExistingId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.ConfirmBookingAsync(99999));
    }

    #endregion

    #region Decline Booking

    [Fact]
    public async Task DeclineBookingAsync_WithPendingBooking_DeclinesSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act
        var result = await _bookingService.DeclineBookingAsync(booking.Id, "Not available");

        // Assert
        result.Status.Should().Be(BookingStatus.Declined);
        result.DeclinedDate.Should().NotBeNull();
        result.DeclineReason.Should().Be("Not available");
    }

    [Fact]
    public async Task DeclineBookingAsync_WithNonPendingBooking_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.DeclineBookingAsync(booking.Id, "Too late"));
    }

    #endregion

    #region Cancel Booking

    [Fact]
    public async Task CancelBookingAsync_WithPendingBooking_CancelsSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act
        var result = await _bookingService.CancelBookingAsync(booking.Id);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled);
        result.CancelledDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelBookingAsync_WithConfirmedBooking_CancelsSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);

        // Act
        var result = await _bookingService.CancelBookingAsync(booking.Id);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBookingAsync_WithCompletedBooking_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);
        await _bookingService.ConvertToPhotoShootAsync(booking.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CancelBookingAsync(booking.Id));
    }

    #endregion

    #region Convert to PhotoShoot

    [Fact]
    public async Task ConvertToPhotoShootAsync_WithConfirmedBooking_CreatesPhotoShoot()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);

        // Act
        var photoShoot = await _bookingService.ConvertToPhotoShootAsync(booking.Id);

        // Assert
        photoShoot.Should().NotBeNull();
        photoShoot.Id.Should().BeGreaterThan(0);
        photoShoot.ClientProfileId.Should().Be(clientProfile.Id);
        photoShoot.Status.Should().Be(PhotoShootStatus.Scheduled);
        photoShoot.Title.Should().Contain(booking.BookingReference);
    }

    [Fact]
    public async Task ConvertToPhotoShootAsync_UpdatesBookingStatus()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);

        // Act
        await _bookingService.ConvertToPhotoShootAsync(booking.Id);

        // Assert
        var updatedBooking = await _bookingService.GetBookingRequestByIdAsync(booking.Id);
        updatedBooking!.Status.Should().Be(BookingStatus.Completed);
        updatedBooking.PhotoShootId.Should().NotBeNull();
    }

    [Fact]
    public async Task ConvertToPhotoShootAsync_WithPendingBooking_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));

        // Act & Assert - Booking is pending, not confirmed
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.ConvertToPhotoShootAsync(booking.Id));
    }

    [Fact]
    public async Task ConvertToPhotoShootAsync_AlreadyConverted_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        await _bookingService.ConfirmBookingAsync(booking.Id);
        await _bookingService.ConvertToPhotoShootAsync(booking.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.ConvertToPhotoShootAsync(booking.Id));
    }

    #endregion

    #region Photographer Availability

    [Fact]
    public async Task CreateAvailabilitySlotAsync_WithValidData_CreatesSlot()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var slot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(17)
        };

        // Act
        var result = await _bookingService.CreateAvailabilitySlotAsync(slot);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.PhotographerProfileId.Should().Be(photographerProfile.Id);
    }

    [Fact]
    public async Task CreateAvailabilitySlotAsync_WithNonExistentPhotographer_ThrowsInvalidOperationException()
    {
        // Arrange
        var slot = new PhotographerAvailability
        {
            PhotographerProfileId = 99999,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(17)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateAvailabilitySlotAsync(slot));
    }

    [Fact]
    public async Task CreateAvailabilitySlotAsync_WithOverlappingSlot_ThrowsInvalidOperationException()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var existingSlot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(17)
        };
        await _bookingService.CreateAvailabilitySlotAsync(existingSlot);

        var overlappingSlot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(12),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(18)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateAvailabilitySlotAsync(overlappingSlot));
    }

    [Fact]
    public async Task GetPhotographerAvailabilityAsync_ReturnsPhotographerSlots()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var slot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(17)
        };
        await _bookingService.CreateAvailabilitySlotAsync(slot);

        // Act
        var results = await _bookingService.GetPhotographerAvailabilityAsync(photographerProfile.Id);

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesBookedAndBlockedSlots()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var targetDate = DateTime.UtcNow.AddDays(1).Date;

        var availableSlot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = targetDate.AddHours(9),
            EndTime = targetDate.AddHours(11),
            IsBooked = false,
            IsBlocked = false
        };

        var bookedSlot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = targetDate.AddHours(13),
            EndTime = targetDate.AddHours(15),
            IsBooked = true,
            IsBlocked = false
        };

        var blockedSlot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = targetDate.AddHours(16),
            EndTime = targetDate.AddHours(18),
            IsBooked = false,
            IsBlocked = true
        };

        _fixture.DbContext.PhotographerAvailabilities.AddRange(availableSlot, bookedSlot, blockedSlot);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var results = await _bookingService.GetAvailableSlotsAsync(targetDate, photographerProfile.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().StartTime.Should().Be(targetDate.AddHours(9));
    }

    [Fact]
    public async Task DeleteAvailabilitySlotAsync_WithUnbookedSlot_DeletesSuccessfully()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var slot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(17)
        };
        var created = await _bookingService.CreateAvailabilitySlotAsync(slot);

        // Act
        var result = await _bookingService.DeleteAvailabilitySlotAsync(created.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAvailabilitySlotAsync_WithBookedSlot_ThrowsInvalidOperationException()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var slot = new PhotographerAvailability
        {
            PhotographerProfileId = photographerProfile.Id,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(17),
            IsBooked = true
        };
        _fixture.DbContext.PhotographerAvailabilities.Add(slot);
        await _fixture.DbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.DeleteAvailabilitySlotAsync(slot.Id));
    }

    [Fact]
    public async Task BlockTimeSlotAsync_CreatesBlockedSlot()
    {
        // Arrange
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();
        var startTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
        var endTime = DateTime.UtcNow.AddDays(1).Date.AddHours(12);

        // Act
        var result = await _bookingService.BlockTimeSlotAsync(photographerProfile.Id, startTime, endTime, "Vacation");

        // Assert
        result.Should().BeTrue();

        var availability = await _bookingService.GetPhotographerAvailabilityAsync(photographerProfile.Id);
        availability.Should().ContainSingle(s => s.IsBlocked && s.Notes == "Vacation");
    }

    #endregion

    #region Statistics

    [Fact]
    public async Task GetPendingBookingsCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-1"));
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-2"));
        var toConfirm = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-3"));
        await _bookingService.ConfirmBookingAsync(toConfirm.Id);

        // Act
        var count = await _bookingService.GetPendingBookingsCountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetBookingsCountByStatusAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-P1"));
        await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-P2"));

        var toConfirm = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-C1"));
        await _bookingService.ConfirmBookingAsync(toConfirm.Id);

        var toDecline = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id, "BK-D1"));
        await _bookingService.DeclineBookingAsync(toDecline.Id, "Test");

        // Act
        var pendingCount = await _bookingService.GetBookingsCountByStatusAsync(BookingStatus.Pending);
        var confirmedCount = await _bookingService.GetBookingsCountByStatusAsync(BookingStatus.Confirmed);
        var declinedCount = await _bookingService.GetBookingsCountByStatusAsync(BookingStatus.Declined);

        // Assert
        pendingCount.Should().Be(2);
        confirmedCount.Should().Be(1);
        declinedCount.Should().Be(1);
    }

    #endregion

    #region Booking Lifecycle

    [Fact]
    public async Task BookingLifecycle_PendingToConfirmedToPhotoShoot()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photographerProfile = await _fixture.CreateTestPhotographerProfileAsync();

        // Act - Create booking
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        booking.Status.Should().Be(BookingStatus.Pending);

        // Act - Confirm booking
        var confirmed = await _bookingService.ConfirmBookingAsync(booking.Id, photographerProfile.Id);
        confirmed.Status.Should().Be(BookingStatus.Confirmed);
        confirmed.PhotographerProfileId.Should().Be(photographerProfile.Id);

        // Act - Convert to PhotoShoot
        var photoShoot = await _bookingService.ConvertToPhotoShootAsync(booking.Id);
        photoShoot.Should().NotBeNull();
        photoShoot.ClientProfileId.Should().Be(clientProfile.Id);
        photoShoot.PhotographerProfileId.Should().Be(photographerProfile.Id);

        // Assert - Booking is now completed
        var completedBooking = await _bookingService.GetBookingRequestByIdAsync(booking.Id);
        completedBooking!.Status.Should().Be(BookingStatus.Completed);
        completedBooking.PhotoShootId.Should().Be(photoShoot.Id);
    }

    [Fact]
    public async Task BookingLifecycle_PendingToDeclined()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act - Create and decline booking
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        var declined = await _bookingService.DeclineBookingAsync(booking.Id, "Photographer unavailable");

        // Assert
        declined.Status.Should().Be(BookingStatus.Declined);
        declined.DeclineReason.Should().Be("Photographer unavailable");
        declined.DeclinedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingLifecycle_PendingToCancelled()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var booking = await _bookingService.CreateBookingRequestAsync(CreateTestBookingRequest(clientProfile.Id));
        var cancelled = await _bookingService.CancelBookingAsync(booking.Id);

        // Assert
        cancelled.Status.Should().Be(BookingStatus.Cancelled);
        cancelled.CancelledDate.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private static BookingRequest CreateTestBookingRequest(int clientProfileId, string reference = "BK-TEST-001")
    {
        return new BookingRequest
        {
            BookingReference = reference,
            ClientProfileId = clientProfileId,
            EventType = "Portrait Session",
            PreferredDate = DateTime.Today.AddDays(14),
            PreferredStartTime = new TimeSpan(10, 0, 0),
            EstimatedDurationHours = 2,
            Location = "Studio A",
            SpecialRequirements = "Need outdoor shots",
            ContactName = "Test Client",
            ContactEmail = "test@example.com",
            ContactPhone = "555-1234",
            EstimatedPrice = 500m
        };
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

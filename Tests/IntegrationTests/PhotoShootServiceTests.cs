using FluentAssertions;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for PhotoShoot lifecycle operations
/// </summary>
public class PhotoShootServiceTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly IPhotoShootService _photoShootService;

    public PhotoShootServiceTests()
    {
        _fixture = new TestFixture();
        _photoShootService = _fixture.GetService<IPhotoShootService>();
    }

    #region Create Operations

    [Fact]
    public async Task CreatePhotoShootAsync_WithValidData_CreatesPhotoShootSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photoShoot = CreateTestPhotoShoot(clientProfile.Id);

        // Act
        var result = await _photoShootService.CreatePhotoShootAsync(photoShoot);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ClientProfileId.Should().Be(clientProfile.Id);
        result.Title.Should().Be("Test Photo Shoot");
    }

    [Fact]
    public async Task CreatePhotoShootAsync_WithPhotographer_CreatesPhotoShootWithPhotographer()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photographer = await _fixture.CreateTestUserAsync(
            email: "photographer@example.com",
            firstName: "Test",
            lastName: "Photographer",
            userType: UserType.Photographer);

        var photoShoot = CreateTestPhotoShoot(clientProfile.Id);
        photoShoot.PhotographerId = photographer.Id;

        // Act
        var result = await _photoShootService.CreatePhotoShootAsync(photoShoot);

        // Assert
        result.Should().NotBeNull();
        result.PhotographerId.Should().Be(photographer.Id);
    }

    [Fact]
    public async Task CreatePhotoShootAsync_WithNonExistentClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var photoShoot = CreateTestPhotoShoot(99999);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _photoShootService.CreatePhotoShootAsync(photoShoot));
    }

    [Fact]
    public async Task CreatePhotoShootAsync_WithNonExistentPhotographer_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photoShoot = CreateTestPhotoShoot(clientProfile.Id);
        photoShoot.PhotographerId = "non-existent-photographer-id";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _photoShootService.CreatePhotoShootAsync(photoShoot));
    }

    [Fact]
    public async Task CreatePhotoShootAsync_WithNullPhotoShoot_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _photoShootService.CreatePhotoShootAsync(null!));
    }

    #endregion

    #region Read Operations

    [Fact]
    public async Task GetAllPhotoShootsAsync_ReturnsAllPhotoShoots()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id, "Shoot 1"));
        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id, "Shoot 2"));

        // Act
        var results = await _photoShootService.GetAllPhotoShootsAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllPhotoShootsAsync_OrdersByScheduledDateDescending()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var olderShoot = CreateTestPhotoShoot(clientProfile.Id, "Older Shoot");
        olderShoot.ScheduledDate = DateTime.Today.AddDays(-10);

        var newerShoot = CreateTestPhotoShoot(clientProfile.Id, "Newer Shoot");
        newerShoot.ScheduledDate = DateTime.Today;

        await _photoShootService.CreatePhotoShootAsync(olderShoot);
        await _photoShootService.CreatePhotoShootAsync(newerShoot);

        // Act
        var results = (await _photoShootService.GetAllPhotoShootsAsync()).ToList();

        // Assert
        results[0].Title.Should().Be("Newer Shoot");
        results[1].Title.Should().Be("Older Shoot");
    }

    [Fact]
    public async Task GetPhotoShootByIdAsync_WithExistingId_ReturnsPhotoShoot()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));

        // Act
        var result = await _photoShootService.GetPhotoShootByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetPhotoShootByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _photoShootService.GetPhotoShootByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhotoShootByIdAsync_IncludesClientProfile()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));

        // Act
        var result = await _photoShootService.GetPhotoShootByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ClientProfile.Should().NotBeNull();
        result.ClientProfile.Id.Should().Be(clientProfile.Id);
    }

    [Fact]
    public async Task GetUpcomingPhotoShootsAsync_ReturnsShootsWithinDateRange()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var upcoming = CreateTestPhotoShoot(clientProfile.Id, "Upcoming");
        upcoming.ScheduledDate = DateTime.Today.AddDays(3);

        var tooFar = CreateTestPhotoShoot(clientProfile.Id, "Too Far");
        tooFar.ScheduledDate = DateTime.Today.AddDays(15);

        var past = CreateTestPhotoShoot(clientProfile.Id, "Past");
        past.ScheduledDate = DateTime.Today.AddDays(-1);

        await _photoShootService.CreatePhotoShootAsync(upcoming);
        await _photoShootService.CreatePhotoShootAsync(tooFar);
        await _photoShootService.CreatePhotoShootAsync(past);

        // Act
        var results = await _photoShootService.GetUpcomingPhotoShootsAsync(7);

        // Assert
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Upcoming");
    }

    [Fact]
    public async Task GetUpcomingPhotoShootsAsync_OrdersByScheduledDateAscending()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var day5 = CreateTestPhotoShoot(clientProfile.Id, "Day 5");
        day5.ScheduledDate = DateTime.Today.AddDays(5);

        var day1 = CreateTestPhotoShoot(clientProfile.Id, "Day 1");
        day1.ScheduledDate = DateTime.Today.AddDays(1);

        var day3 = CreateTestPhotoShoot(clientProfile.Id, "Day 3");
        day3.ScheduledDate = DateTime.Today.AddDays(3);

        await _photoShootService.CreatePhotoShootAsync(day5);
        await _photoShootService.CreatePhotoShootAsync(day1);
        await _photoShootService.CreatePhotoShootAsync(day3);

        // Act
        var results = (await _photoShootService.GetUpcomingPhotoShootsAsync(7)).ToList();

        // Assert
        results[0].Title.Should().Be("Day 1");
        results[1].Title.Should().Be("Day 3");
        results[2].Title.Should().Be("Day 5");
    }

    [Fact]
    public async Task GetPhotoShootsByClientIdAsync_ReturnsOnlyClientPhotoShoots()
    {
        // Arrange
        var clientProfile1 = await _fixture.CreateTestClientProfileAsync();
        var user2 = await _fixture.CreateTestUserAsync(email: "client2@example.com", firstName: "Client", lastName: "Two");
        var clientProfile2 = await _fixture.CreateTestClientProfileAsync(user2);

        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile1.Id, "Client 1 Shoot"));
        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile2.Id, "Client 2 Shoot"));

        // Act
        var results = await _photoShootService.GetPhotoShootsByClientIdAsync(clientProfile1.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Client 1 Shoot");
    }

    #endregion

    #region Update Operations

    [Fact]
    public async Task UpdatePhotoShootAsync_WithValidData_UpdatesPhotoShootSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));

        created.Title = "Updated Title";
        created.Location = "Updated Location";
        created.Status = PhotoShootStatus.Completed;

        // Act
        var result = await _photoShootService.UpdatePhotoShootAsync(created);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Location.Should().Be("Updated Location");
        result.Status.Should().Be(PhotoShootStatus.Completed);
    }

    [Fact]
    public async Task UpdatePhotoShootAsync_WithNonExistingId_ThrowsInvalidOperationException()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photoShoot = CreateTestPhotoShoot(clientProfile.Id);
        photoShoot.Id = 99999;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _photoShootService.UpdatePhotoShootAsync(photoShoot));
    }

    [Fact]
    public async Task UpdatePhotoShootAsync_WithNullPhotoShoot_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _photoShootService.UpdatePhotoShootAsync(null!));
    }

    #endregion

    #region Delete Operations

    [Fact]
    public async Task DeletePhotoShootAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));

        // Act
        var result = await _photoShootService.DeletePhotoShootAsync(created.Id);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        var deleted = await _photoShootService.GetPhotoShootByIdAsync(created.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeletePhotoShootAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _photoShootService.DeletePhotoShootAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Count Operations

    [Fact]
    public async Task GetPhotoShootsCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));
        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));
        await _photoShootService.CreatePhotoShootAsync(CreateTestPhotoShoot(clientProfile.Id));

        // Act
        var count = await _photoShootService.GetPhotoShootsCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetPendingPhotoShootsCountAsync_CountsOnlyScheduledShoots()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var scheduled = CreateTestPhotoShoot(clientProfile.Id, "Scheduled");
        scheduled.Status = PhotoShootStatus.Scheduled;

        var inProgress = CreateTestPhotoShoot(clientProfile.Id, "In Progress");
        inProgress.Status = PhotoShootStatus.InProgress;

        var completed = CreateTestPhotoShoot(clientProfile.Id, "Completed");
        completed.Status = PhotoShootStatus.Completed;

        await _photoShootService.CreatePhotoShootAsync(scheduled);
        await _photoShootService.CreatePhotoShootAsync(inProgress);
        await _photoShootService.CreatePhotoShootAsync(completed);

        // Act
        var count = await _photoShootService.GetPendingPhotoShootsCountAsync();

        // Assert
        count.Should().Be(1);
    }

    #endregion

    #region Status Lifecycle Tests

    [Fact]
    public async Task PhotoShootLifecycle_ScheduledToInProgressToCompleted()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photoShoot = CreateTestPhotoShoot(clientProfile.Id);
        photoShoot.Status = PhotoShootStatus.Scheduled;

        var created = await _photoShootService.CreatePhotoShootAsync(photoShoot);
        created.Status.Should().Be(PhotoShootStatus.Scheduled);

        // Act - Move to InProgress
        created.Status = PhotoShootStatus.InProgress;
        var inProgress = await _photoShootService.UpdatePhotoShootAsync(created);
        inProgress.Status.Should().Be(PhotoShootStatus.InProgress);

        // Act - Move to Completed
        created.Status = PhotoShootStatus.Completed;
        var completed = await _photoShootService.UpdatePhotoShootAsync(created);
        completed.Status.Should().Be(PhotoShootStatus.Completed);
    }

    [Fact]
    public async Task PhotoShootLifecycle_ScheduledToCancelled()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var photoShoot = CreateTestPhotoShoot(clientProfile.Id);
        photoShoot.Status = PhotoShootStatus.Scheduled;

        var created = await _photoShootService.CreatePhotoShootAsync(photoShoot);

        // Act - Cancel
        created.Status = PhotoShootStatus.Cancelled;
        var cancelled = await _photoShootService.UpdatePhotoShootAsync(created);

        // Assert
        cancelled.Status.Should().Be(PhotoShootStatus.Cancelled);
    }

    #endregion

    #region Helper Methods

    private static PhotoShoot CreateTestPhotoShoot(int clientProfileId, string title = "Test Photo Shoot")
    {
        return new PhotoShoot
        {
            Title = title,
            Description = "Test description",
            Location = "Test Location",
            ScheduledDate = DateTime.Today.AddDays(7),
            EndTime = DateTime.Today.AddDays(7).AddHours(2),
            DurationHours = 2,
            DurationMinutes = 0,
            Price = 500m,
            Status = PhotoShootStatus.InProgress,
            ClientProfileId = clientProfileId,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

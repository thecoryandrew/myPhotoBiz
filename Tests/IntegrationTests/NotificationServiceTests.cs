using FluentAssertions;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for Notification system operations
/// </summary>
public class NotificationServiceTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly INotificationService _notificationService;

    public NotificationServiceTests()
    {
        _fixture = new TestFixture();
        _notificationService = _fixture.GetService<INotificationService>();
    }

    #region Create Notification

    [Fact]
    public async Task CreateNotificationAsync_WithValidData_CreatesNotificationSuccessfully()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = CreateTestNotification(user.Id);

        // Act
        await _notificationService.CreateNotificationAsync(notification);

        // Assert
        notification.Id.Should().BeGreaterThan(0);

        var retrieved = await _notificationService.GetNotificationByIdAsync(notification.Id);
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be(user.Id);
        retrieved.Message.Should().Be("Test notification message");
    }

    [Fact]
    public async Task CreateNotificationAsync_DefaultsToUnread()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = CreateTestNotification(user.Id);

        // Act
        await _notificationService.CreateNotificationAsync(notification);

        // Assert
        var retrieved = await _notificationService.GetNotificationByIdAsync(notification.Id);
        retrieved!.IsRead.Should().BeFalse();
        retrieved.ReadDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateNotificationAsync_WithAllFields_PersistsAllFields()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = new Notification
        {
            UserId = user.Id,
            Title = "Important Alert",
            Message = "Your invoice is due",
            Type = NotificationType.Invoice,
            Link = "/Invoices/123",
            Icon = "bell-icon",
            CreatedDate = DateTime.Now
        };

        // Act
        await _notificationService.CreateNotificationAsync(notification);

        // Assert
        var retrieved = await _notificationService.GetNotificationByIdAsync(notification.Id);
        retrieved!.Title.Should().Be("Important Alert");
        retrieved.Type.Should().Be(NotificationType.Invoice);
        retrieved.Link.Should().Be("/Invoices/123");
        retrieved.Icon.Should().Be("bell-icon");
    }

    #endregion

    #region Read Notifications

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsUserNotifications()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "Notification 1"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "Notification 2"));

        // Act
        var results = await _notificationService.GetUserNotificationsAsync(user.Id);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_RespectsLimit()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        for (int i = 0; i < 15; i++)
        {
            await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, $"Notification {i}"));
        }

        // Act
        var results = await _notificationService.GetUserNotificationsAsync(user.Id, 5);

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_OrdersByCreatedDateDescending()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        var notification1 = CreateTestNotification(user.Id, "First");
        notification1.CreatedDate = DateTime.Now.AddHours(-2);
        await _notificationService.CreateNotificationAsync(notification1);

        var notification2 = CreateTestNotification(user.Id, "Second");
        notification2.CreatedDate = DateTime.Now;
        await _notificationService.CreateNotificationAsync(notification2);

        // Act
        var results = (await _notificationService.GetUserNotificationsAsync(user.Id)).ToList();

        // Assert
        results[0].Message.Should().Be("Second");
        results[1].Message.Should().Be("First");
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsEmptyForNewUser()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        // Act
        var results = await _notificationService.GetUserNotificationsAsync(user.Id);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ExcludesOtherUsersNotifications()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "user1@example.com");
        var user2 = await _fixture.CreateTestUserAsync(email: "user2@example.com");

        await _notificationService.CreateNotificationAsync(CreateTestNotification(user1.Id, "User 1 notification"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user2.Id, "User 2 notification"));

        // Act
        var user1Notifications = await _notificationService.GetUserNotificationsAsync(user1.Id);
        var user2Notifications = await _notificationService.GetUserNotificationsAsync(user2.Id);

        // Assert
        user1Notifications.Should().HaveCount(1);
        user1Notifications.First().Message.Should().Be("User 1 notification");

        user2Notifications.Should().HaveCount(1);
        user2Notifications.First().Message.Should().Be("User 2 notification");
    }

    [Fact]
    public async Task GetNotificationByIdAsync_WithExistingId_ReturnsNotification()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = CreateTestNotification(user.Id);
        await _notificationService.CreateNotificationAsync(notification);

        // Act
        var result = await _notificationService.GetNotificationByIdAsync(notification.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task GetNotificationByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _notificationService.GetNotificationByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Unread Notifications

    [Fact]
    public async Task GetUnreadNotificationsAsync_ReturnsOnlyUnreadNotifications()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        var unread = CreateTestNotification(user.Id, "Unread");
        await _notificationService.CreateNotificationAsync(unread);

        var toRead = CreateTestNotification(user.Id, "To be read");
        await _notificationService.CreateNotificationAsync(toRead);
        await _notificationService.MarkAsReadAsync(toRead.Id);

        // Act
        var results = await _notificationService.GetUnreadNotificationsAsync(user.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().Message.Should().Be("Unread");
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_OrdersByCreatedDateDescending()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        var older = CreateTestNotification(user.Id, "Older");
        older.CreatedDate = DateTime.Now.AddHours(-2);
        await _notificationService.CreateNotificationAsync(older);

        var newer = CreateTestNotification(user.Id, "Newer");
        newer.CreatedDate = DateTime.Now;
        await _notificationService.CreateNotificationAsync(newer);

        // Act
        var results = (await _notificationService.GetUnreadNotificationsAsync(user.Id)).ToList();

        // Assert
        results[0].Message.Should().Be("Newer");
        results[1].Message.Should().Be("Older");
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "Unread 1"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "Unread 2"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "Unread 3"));

        var toRead = CreateTestNotification(user.Id, "To be read");
        await _notificationService.CreateNotificationAsync(toRead);
        await _notificationService.MarkAsReadAsync(toRead.Id);

        // Act
        var count = await _notificationService.GetUnreadCountAsync(user.Id);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsZeroForNoUnreadNotifications()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        // Act
        var count = await _notificationService.GetUnreadCountAsync(user.Id);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region Mark As Read

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationAsRead()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = CreateTestNotification(user.Id);
        await _notificationService.CreateNotificationAsync(notification);

        // Act
        await _notificationService.MarkAsReadAsync(notification.Id);

        // Assert
        var updated = await _notificationService.GetNotificationByIdAsync(notification.Id);
        updated!.IsRead.Should().BeTrue();
        updated.ReadDate.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_DoesNotChangeReadDate()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = CreateTestNotification(user.Id);
        await _notificationService.CreateNotificationAsync(notification);

        // Mark as read first time
        await _notificationService.MarkAsReadAsync(notification.Id);
        var firstRead = await _notificationService.GetNotificationByIdAsync(notification.Id);
        var originalReadDate = firstRead!.ReadDate;

        await Task.Delay(50);

        // Mark as read second time
        await _notificationService.MarkAsReadAsync(notification.Id);

        // Assert - Read date should not change
        var updated = await _notificationService.GetNotificationByIdAsync(notification.Id);
        updated!.ReadDate.Should().Be(originalReadDate);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistingId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(() => _notificationService.MarkAsReadAsync(99999));
        exception.Should().BeNull();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUserNotificationsAsRead()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "N1"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "N2"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user.Id, "N3"));

        // Verify initial state
        var unreadBefore = await _notificationService.GetUnreadCountAsync(user.Id);
        unreadBefore.Should().Be(3);

        // Act
        await _notificationService.MarkAllAsReadAsync(user.Id);

        // Assert
        var unreadAfter = await _notificationService.GetUnreadCountAsync(user.Id);
        unreadAfter.Should().Be(0);

        var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
        notifications.All(n => n.IsRead).Should().BeTrue();
        notifications.All(n => n.ReadDate.HasValue).Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_DoesNotAffectOtherUsers()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "user1@example.com");
        var user2 = await _fixture.CreateTestUserAsync(email: "user2@example.com");

        await _notificationService.CreateNotificationAsync(CreateTestNotification(user1.Id, "User 1"));
        await _notificationService.CreateNotificationAsync(CreateTestNotification(user2.Id, "User 2"));

        // Act - Only mark user1's notifications as read
        await _notificationService.MarkAllAsReadAsync(user1.Id);

        // Assert
        var user1Count = await _notificationService.GetUnreadCountAsync(user1.Id);
        var user2Count = await _notificationService.GetUnreadCountAsync(user2.Id);

        user1Count.Should().Be(0);
        user2Count.Should().Be(1);
    }

    #endregion

    #region Delete Notification

    [Fact]
    public async Task DeleteNotificationAsync_WithExistingId_DeletesNotification()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = CreateTestNotification(user.Id);
        await _notificationService.CreateNotificationAsync(notification);

        // Act
        await _notificationService.DeleteNotificationAsync(notification.Id);

        // Assert
        var deleted = await _notificationService.GetNotificationByIdAsync(notification.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNotificationAsync_WithNonExistingId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(() => _notificationService.DeleteNotificationAsync(99999));
        exception.Should().BeNull();
    }

    #endregion

    #region Delete Old Notifications

    [Fact]
    public async Task DeleteOldNotificationsAsync_DeletesOnlyOldReadNotifications()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        // Old and read - should be deleted
        var oldRead = CreateTestNotification(user.Id, "Old Read");
        oldRead.CreatedDate = DateTime.Now.AddDays(-45);
        oldRead.IsRead = true;
        oldRead.ReadDate = DateTime.Now.AddDays(-40);
        await _notificationService.CreateNotificationAsync(oldRead);

        // Old but unread - should NOT be deleted
        var oldUnread = CreateTestNotification(user.Id, "Old Unread");
        oldUnread.CreatedDate = DateTime.Now.AddDays(-45);
        oldUnread.IsRead = false;
        await _notificationService.CreateNotificationAsync(oldUnread);

        // Recent and read - should NOT be deleted
        var recentRead = CreateTestNotification(user.Id, "Recent Read");
        recentRead.CreatedDate = DateTime.Now.AddDays(-5);
        recentRead.IsRead = true;
        recentRead.ReadDate = DateTime.Now.AddDays(-4);
        await _notificationService.CreateNotificationAsync(recentRead);

        // Act
        await _notificationService.DeleteOldNotificationsAsync(30);

        // Assert
        var remaining = await _notificationService.GetUserNotificationsAsync(user.Id, 100);
        remaining.Should().HaveCount(2);
        remaining.Select(n => n.Message).Should().Contain("Old Unread");
        remaining.Select(n => n.Message).Should().Contain("Recent Read");
        remaining.Select(n => n.Message).Should().NotContain("Old Read");
    }

    [Fact]
    public async Task DeleteOldNotificationsAsync_RespectsCustomDaysParameter()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();

        var notification = CreateTestNotification(user.Id, "15 Days Old Read");
        notification.CreatedDate = DateTime.Now.AddDays(-15);
        notification.IsRead = true;
        notification.ReadDate = DateTime.Now.AddDays(-14);
        await _notificationService.CreateNotificationAsync(notification);

        // Act - Delete notifications older than 10 days
        await _notificationService.DeleteOldNotificationsAsync(10);

        // Assert - The 15-day-old notification should be deleted
        var deleted = await _notificationService.GetNotificationByIdAsync(notification.Id);
        deleted.Should().BeNull();
    }

    #endregion

    #region Notification Types

    [Theory]
    [InlineData(NotificationType.Info)]
    [InlineData(NotificationType.Success)]
    [InlineData(NotificationType.Warning)]
    [InlineData(NotificationType.Error)]
    [InlineData(NotificationType.Invoice)]
    [InlineData(NotificationType.PhotoShoot)]
    [InlineData(NotificationType.Client)]
    [InlineData(NotificationType.Album)]
    public async Task CreateNotificationAsync_WithDifferentTypes_PersistsCorrectly(NotificationType type)
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        var notification = new Notification
        {
            UserId = user.Id,
            Message = $"Test {type} notification",
            Type = type,
            CreatedDate = DateTime.Now
        };

        // Act
        await _notificationService.CreateNotificationAsync(notification);

        // Assert
        var retrieved = await _notificationService.GetNotificationByIdAsync(notification.Id);
        retrieved!.Type.Should().Be(type);
    }

    #endregion

    #region Helper Methods

    private static Notification CreateTestNotification(string userId, string message = "Test notification message")
    {
        return new Notification
        {
            UserId = userId,
            Message = message,
            Type = NotificationType.Info,
            IsRead = false,
            CreatedDate = DateTime.Now
        };
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

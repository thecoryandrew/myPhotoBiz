using FluentAssertions;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for Gallery access control operations
/// </summary>
public class GalleryServiceTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly IGalleryService _galleryService;

    public GalleryServiceTests()
    {
        _fixture = new TestFixture();
        _galleryService = _fixture.GetService<IGalleryService>();
    }

    #region Create Gallery

    [Fact]
    public async Task CreateGalleryAsync_WithValidData_CreatesGallerySuccessfully()
    {
        // Arrange
        var model = CreateTestGalleryViewModel("Test Gallery");

        // Act
        var result = await _galleryService.CreateGalleryAsync(model);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Test Gallery");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateGalleryAsync_WithClientAccess_GrantsAccessToClients()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var model = CreateTestGalleryViewModel("Gallery With Access");
        model.SelectedClientProfileIds = new List<int> { clientProfile.Id };

        // Act
        var gallery = await _galleryService.CreateGalleryAsync(model);

        // Assert
        var accesses = await _galleryService.GetGalleryAccessesAsync(gallery.Id);
        accesses.Should().HaveCount(1);
        accesses.First().ClientProfileId.Should().Be(clientProfile.Id);
        accesses.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateGalleryAsync_SetsCreatedDateAndDefaults()
    {
        // Arrange
        var model = CreateTestGalleryViewModel("New Gallery");

        // Act
        var result = await _galleryService.CreateGalleryAsync(model);

        // Assert
        result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.BrandColor.Should().Be("#2c3e50");
    }

    #endregion

    #region Read Gallery

    [Fact]
    public async Task GetAllGalleriesAsync_ReturnsAllGalleries()
    {
        // Arrange
        await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Gallery 1"));
        await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Gallery 2"));

        // Act
        var results = await _galleryService.GetAllGalleriesAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllGalleriesAsync_OrdersByCreatedDateDescending()
    {
        // Arrange
        await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("First Gallery"));
        await Task.Delay(10);
        await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Second Gallery"));

        // Act
        var results = (await _galleryService.GetAllGalleriesAsync()).ToList();

        // Assert
        results[0].Name.Should().Be("Second Gallery");
        results[1].Name.Should().Be("First Gallery");
    }

    [Fact]
    public async Task GetGalleryByIdAsync_WithExistingId_ReturnsGallery()
    {
        // Arrange
        var created = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Test Gallery"));

        // Act
        var result = await _galleryService.GetGalleryByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Test Gallery");
    }

    [Fact]
    public async Task GetGalleryByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _galleryService.GetGalleryByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGalleryDetailsAsync_WithExistingId_ReturnsDetails()
    {
        // Arrange
        var created = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Detail Test Gallery"));

        // Act
        var result = await _galleryService.GetGalleryDetailsAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Detail Test Gallery");
        result.PhotoCount.Should().Be(0);
        result.TotalSessions.Should().Be(0);
    }

    [Fact]
    public async Task GetGalleryDetailsAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _galleryService.GetGalleryDetailsAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Update Gallery

    [Fact]
    public async Task UpdateGalleryAsync_WithValidData_UpdatesGallerySuccessfully()
    {
        // Arrange
        var created = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Original Name"));

        var updateModel = new EditGalleryViewModel
        {
            Id = created.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            BrandColor = "#ff0000",
            IsActive = true,
            SelectedAlbumIds = new List<int>()
        };

        // Act
        var result = await _galleryService.UpdateGalleryAsync(updateModel);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.BrandColor.Should().Be("#ff0000");
    }

    [Fact]
    public async Task UpdateGalleryAsync_WithNonExistingId_ThrowsInvalidOperationException()
    {
        // Arrange
        var updateModel = new EditGalleryViewModel
        {
            Id = 99999,
            Name = "Updated",
            Description = "Updated",
            ExpiryDate = DateTime.UtcNow.AddMonths(3),
            BrandColor = "#2c3e50",
            IsActive = true,
            SelectedAlbumIds = new List<int>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _galleryService.UpdateGalleryAsync(updateModel));
    }

    #endregion

    #region Delete Gallery

    [Fact]
    public async Task DeleteGalleryAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var created = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("To Delete"));

        // Act
        var result = await _galleryService.DeleteGalleryAsync(created.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _galleryService.GetGalleryByIdAsync(created.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGalleryAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _galleryService.DeleteGalleryAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Toggle Gallery Status

    [Fact]
    public async Task ToggleGalleryStatusAsync_DeactivatesActiveGallery()
    {
        // Arrange
        var model = CreateTestGalleryViewModel("Active Gallery");
        model.IsActive = true;
        var gallery = await _galleryService.CreateGalleryAsync(model);

        // Act
        var result = await _galleryService.ToggleGalleryStatusAsync(gallery.Id, false);

        // Assert
        result.Should().BeTrue();

        var updated = await _galleryService.GetGalleryByIdAsync(gallery.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleGalleryStatusAsync_ActivatesInactiveGallery()
    {
        // Arrange
        var model = CreateTestGalleryViewModel("Inactive Gallery");
        model.IsActive = false;
        var gallery = await _galleryService.CreateGalleryAsync(model);

        // Act
        var result = await _galleryService.ToggleGalleryStatusAsync(gallery.Id, true);

        // Assert
        result.Should().BeTrue();

        var updated = await _galleryService.GetGalleryByIdAsync(gallery.Id);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleGalleryStatusAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _galleryService.ToggleGalleryStatusAsync(99999, true);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Grant Access

    [Fact]
    public async Task GrantAccessAsync_WithValidData_GrantsAccessSuccessfully()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Access Test Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var access = await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);

        // Assert
        access.Should().NotBeNull();
        access.GalleryId.Should().Be(gallery.Id);
        access.ClientProfileId.Should().Be(clientProfile.Id);
        access.IsActive.Should().BeTrue();
        access.CanDownload.Should().BeTrue();
        access.CanProof.Should().BeTrue();
        access.CanOrder.Should().BeTrue();
    }

    [Fact]
    public async Task GrantAccessAsync_WithExpiryDate_SetsExpiryDate()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Expiry Test Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var expiryDate = DateTime.UtcNow.AddDays(30);

        // Act
        var access = await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id, expiryDate);

        // Assert
        access.ExpiryDate.Should().BeCloseTo(expiryDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GrantAccessAsync_WithExistingAccess_ReactivatesAccess()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Reactivate Test Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Grant initial access
        var initialAccess = await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);
        // Revoke it
        await _galleryService.RevokeAccessAsync(gallery.Id, clientProfile.Id);
        // Re-grant access
        var reactivatedAccess = await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);

        // Assert
        reactivatedAccess.Id.Should().Be(initialAccess.Id);
        reactivatedAccess.IsActive.Should().BeTrue();
    }

    #endregion

    #region Revoke Access

    [Fact]
    public async Task RevokeAccessAsync_WithExistingAccess_RevokesSuccessfully()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Revoke Test Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);

        // Act
        var result = await _galleryService.RevokeAccessAsync(gallery.Id, clientProfile.Id);

        // Assert
        result.Should().BeTrue();

        // Verify access is revoked (deactivated, not deleted)
        var accesses = await _galleryService.GetGalleryAccessesAsync(gallery.Id);
        accesses.First(a => a.ClientProfileId == clientProfile.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAccessAsync_WithNonExistingAccess_ReturnsFalse()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("No Access Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var result = await _galleryService.RevokeAccessAsync(gallery.Id, clientProfile.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Validate User Access

    [Fact]
    public async Task ValidateUserAccessAsync_WithActiveAccess_ReturnsTrue()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Validate Test Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);

        // Act
        var result = await _galleryService.ValidateUserAccessAsync(gallery.Id, clientProfile.UserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateUserAccessAsync_WithRevokedAccess_ReturnsFalse()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Revoked Access Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);
        await _galleryService.RevokeAccessAsync(gallery.Id, clientProfile.Id);

        // Act
        var result = await _galleryService.ValidateUserAccessAsync(gallery.Id, clientProfile.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateUserAccessAsync_WithExpiredAccess_ReturnsFalse()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Expired Access Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Create access with past expiry date directly in DB
        var access = new GalleryAccess
        {
            GalleryId = gallery.Id,
            ClientProfileId = clientProfile.Id,
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };
        _fixture.DbContext.GalleryAccesses.Add(access);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var result = await _galleryService.ValidateUserAccessAsync(gallery.Id, clientProfile.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateUserAccessAsync_WithNoAccess_ReturnsFalse()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("No Access Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var result = await _galleryService.ValidateUserAccessAsync(gallery.Id, clientProfile.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateUserAccessAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Test Gallery"));

        // Act
        var result = await _galleryService.ValidateUserAccessAsync(gallery.Id, "non-existent-user-id");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Get Gallery Accesses

    [Fact]
    public async Task GetGalleryAccessesAsync_ReturnsAllAccesses()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Multi Access Gallery"));
        var client1 = await _fixture.CreateTestClientProfileAsync();
        var user2 = await _fixture.CreateTestUserAsync(email: "client2@example.com", firstName: "Client", lastName: "Two");
        var client2 = await _fixture.CreateTestClientProfileAsync(user2);

        await _galleryService.GrantAccessAsync(gallery.Id, client1.Id);
        await _galleryService.GrantAccessAsync(gallery.Id, client2.Id);

        // Act
        var accesses = await _galleryService.GetGalleryAccessesAsync(gallery.Id);

        // Assert
        accesses.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGalleryAccessesAsync_IncludesClientProfile()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Client Info Gallery"));
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _galleryService.GrantAccessAsync(gallery.Id, clientProfile.Id);

        // Act
        var accesses = await _galleryService.GetGalleryAccessesAsync(gallery.Id);

        // Assert
        accesses.First().ClientProfile.Should().NotBeNull();
        accesses.First().ClientProfile.User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGalleryAccessesAsync_OrdersByGrantedDateDescending()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Ordered Access Gallery"));
        var client1 = await _fixture.CreateTestClientProfileAsync();
        var user2 = await _fixture.CreateTestUserAsync(email: "client2@example.com", firstName: "Client", lastName: "Two");
        var client2 = await _fixture.CreateTestClientProfileAsync(user2);

        await _galleryService.GrantAccessAsync(gallery.Id, client1.Id);
        await Task.Delay(10);
        await _galleryService.GrantAccessAsync(gallery.Id, client2.Id);

        // Act
        var accesses = (await _galleryService.GetGalleryAccessesAsync(gallery.Id)).ToList();

        // Assert
        accesses[0].ClientProfileId.Should().Be(client2.Id);
        accesses[1].ClientProfileId.Should().Be(client1.Id);
    }

    #endregion

    #region Gallery Statistics

    [Fact]
    public async Task GetGalleryStatsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var activeGallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Active Gallery"));

        var expiredModel = CreateTestGalleryViewModel("Expired Gallery");
        expiredModel.ExpiryDate = DateTime.UtcNow.AddDays(-1);
        await _galleryService.CreateGalleryAsync(expiredModel);

        // Act
        var stats = await _galleryService.GetGalleryStatsAsync();

        // Assert
        stats.TotalGalleries.Should().Be(2);
        stats.ActiveGalleries.Should().Be(1);
        stats.ExpiredGalleries.Should().Be(1);
    }

    #endregion

    #region Session Management

    [Fact]
    public async Task GetGallerySessionsAsync_ReturnsEmptyForNewGallery()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("New Gallery"));

        // Act
        var sessions = await _galleryService.GetGallerySessionsAsync(gallery.Id);

        // Assert
        sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task EndAllSessionsAsync_RemovesAllSessions()
    {
        // Arrange
        var gallery = await _galleryService.CreateGalleryAsync(CreateTestGalleryViewModel("Session Gallery"));
        var user = await _fixture.CreateTestUserAsync();

        // Add sessions directly
        _fixture.DbContext.GallerySessions.Add(new GallerySession
        {
            GalleryId = gallery.Id,
            UserId = user.Id,
            SessionToken = "token1",
            CreatedDate = DateTime.UtcNow,
            LastAccessDate = DateTime.UtcNow
        });
        _fixture.DbContext.GallerySessions.Add(new GallerySession
        {
            GalleryId = gallery.Id,
            UserId = user.Id,
            SessionToken = "token2",
            CreatedDate = DateTime.UtcNow,
            LastAccessDate = DateTime.UtcNow
        });
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var result = await _galleryService.EndAllSessionsAsync(gallery.Id);

        // Assert
        result.Should().BeTrue();

        var sessions = await _galleryService.GetGallerySessionsAsync(gallery.Id);
        sessions.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static CreateGalleryViewModel CreateTestGalleryViewModel(string name)
    {
        return new CreateGalleryViewModel
        {
            Name = name,
            Description = $"Description for {name}",
            ExpiryDate = DateTime.UtcNow.AddMonths(3),
            BrandColor = "#2c3e50",
            IsActive = true,
            SelectedAlbumIds = new List<int>(),
            SelectedClientProfileIds = new List<int>()
        };
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

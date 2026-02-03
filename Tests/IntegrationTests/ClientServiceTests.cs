using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for Client management operations
/// </summary>
public class ClientServiceTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly IClientService _clientService;

    public ClientServiceTests()
    {
        _fixture = new TestFixture();
        _clientService = _fixture.GetService<IClientService>();
    }

    #region Create Operations

    [Fact]
    public async Task CreateClientAsync_WithValidData_CreatesClientSuccessfully()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync(
            email: "newclient@example.com",
            firstName: "New",
            lastName: "Client");

        var clientProfile = new ClientProfile
        {
            UserId = user.Id,
            User = user,
            PhoneNumber = "555-9876",
            Address = "456 New Street",
            Notes = "New client notes",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _clientService.CreateClientAsync(clientProfile);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(user.Id);
        result.PhoneNumber.Should().Be("555-9876");
        result.Address.Should().Be("456 New Street");
    }

    [Fact]
    public async Task CreateClientAsync_WithNullProfile_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _clientService.CreateClientAsync(null!));
    }

    #endregion

    #region Read Operations

    [Fact]
    public async Task GetAllClientsAsync_ReturnsAllClients()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "client1@example.com", firstName: "Alice", lastName: "Smith");
        var user2 = await _fixture.CreateTestUserAsync(email: "client2@example.com", firstName: "Bob", lastName: "Jones");

        await _fixture.CreateTestClientProfileAsync(user1);
        await _fixture.CreateTestClientProfileAsync(user2);

        // Act
        var clients = await _clientService.GetAllClientsAsync();

        // Assert
        clients.Should().HaveCount(2);
        clients.Select(c => c.User.Email).Should().Contain("client1@example.com");
        clients.Select(c => c.User.Email).Should().Contain("client2@example.com");
    }

    [Fact]
    public async Task GetAllClientsAsync_OrdersByLastNameThenFirstName()
    {
        // Arrange
        var userA = await _fixture.CreateTestUserAsync(email: "a@example.com", firstName: "Zoe", lastName: "Adams");
        var userB = await _fixture.CreateTestUserAsync(email: "b@example.com", firstName: "Alice", lastName: "Brown");
        var userC = await _fixture.CreateTestUserAsync(email: "c@example.com", firstName: "Bob", lastName: "Adams");

        await _fixture.CreateTestClientProfileAsync(userA);
        await _fixture.CreateTestClientProfileAsync(userB);
        await _fixture.CreateTestClientProfileAsync(userC);

        // Act
        var clients = (await _clientService.GetAllClientsAsync()).ToList();

        // Assert
        clients.Should().HaveCount(3);
        clients[0].User.LastName.Should().Be("Adams");
        clients[0].User.FirstName.Should().Be("Bob"); // Bob Adams comes before Zoe Adams
        clients[1].User.LastName.Should().Be("Adams");
        clients[1].User.FirstName.Should().Be("Zoe");
        clients[2].User.LastName.Should().Be("Brown");
    }

    [Fact]
    public async Task GetClientByIdAsync_WithExistingId_ReturnsClient()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var result = await _clientService.GetClientByIdAsync(clientProfile.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(clientProfile.Id);
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClientByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _clientService.GetClientByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClientByUserIdAsync_WithExistingUserId_ReturnsClient()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var result = await _clientService.GetClientByUserIdAsync(clientProfile.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(clientProfile.UserId);
    }

    [Fact]
    public async Task GetClientByUserIdAsync_WithNonExistingUserId_ReturnsNull()
    {
        // Act
        var result = await _clientService.GetClientByUserIdAsync("non-existent-user-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClientByIdAsync_IncludesRelatedData()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var result = await _clientService.GetClientByIdAsync(clientProfile.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Invoices.Should().NotBeNull();
        result.PhotoShoots.Should().NotBeNull();
        result.ClientBadges.Should().NotBeNull();
        result.GalleryAccesses.Should().NotBeNull();
    }

    #endregion

    #region Update Operations

    [Fact]
    public async Task UpdateClientAsync_WithValidData_UpdatesClientSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var originalUpdatedDate = clientProfile.UpdatedDate;

        // Allow some time to pass for updated date comparison
        await Task.Delay(10);

        clientProfile.PhoneNumber = "555-UPDATED";
        clientProfile.Address = "Updated Address";
        clientProfile.Notes = "Updated Notes";

        // Act
        var result = await _clientService.UpdateClientAsync(clientProfile);

        // Assert
        result.Should().NotBeNull();
        result.PhoneNumber.Should().Be("555-UPDATED");
        result.Address.Should().Be("Updated Address");
        result.Notes.Should().Be("Updated Notes");
        result.UpdatedDate.Should().BeAfter(originalUpdatedDate);
    }

    [Fact]
    public async Task UpdateClientAsync_WithNonExistingClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentProfile = new ClientProfile
        {
            Id = 99999,
            UserId = "non-existent"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _clientService.UpdateClientAsync(nonExistentProfile));
    }

    [Fact]
    public async Task UpdateClientAsync_WithNullProfile_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _clientService.UpdateClientAsync(null!));
    }

    #endregion

    #region Delete Operations

    [Fact]
    public async Task DeleteClientAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        // Act
        var result = await _clientService.DeleteClientAsync(clientProfile.Id);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        var deleted = await _clientService.GetClientByIdAsync(clientProfile.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteClientAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _clientService.DeleteClientAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Search Operations

    [Fact]
    public async Task SearchClientsAsync_ByFirstName_ReturnsMatchingClients()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "john@example.com", firstName: "John", lastName: "Doe");
        var user2 = await _fixture.CreateTestUserAsync(email: "jane@example.com", firstName: "Jane", lastName: "Doe");
        var user3 = await _fixture.CreateTestUserAsync(email: "bob@example.com", firstName: "Bob", lastName: "Smith");

        await _fixture.CreateTestClientProfileAsync(user1);
        await _fixture.CreateTestClientProfileAsync(user2);
        await _fixture.CreateTestClientProfileAsync(user3);

        // Act
        var results = await _clientService.SearchClientsAsync("John");

        // Assert
        results.Should().HaveCount(1);
        results.First().User.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task SearchClientsAsync_ByLastName_ReturnsMatchingClients()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "john@example.com", firstName: "John", lastName: "Doe");
        var user2 = await _fixture.CreateTestUserAsync(email: "jane@example.com", firstName: "Jane", lastName: "Doe");
        var user3 = await _fixture.CreateTestUserAsync(email: "bob@example.com", firstName: "Bob", lastName: "Smith");

        await _fixture.CreateTestClientProfileAsync(user1);
        await _fixture.CreateTestClientProfileAsync(user2);
        await _fixture.CreateTestClientProfileAsync(user3);

        // Act
        var results = await _clientService.SearchClientsAsync("Doe");

        // Assert
        results.Should().HaveCount(2);
        results.All(c => c.User.LastName == "Doe").Should().BeTrue();
    }

    [Fact]
    public async Task SearchClientsAsync_ByEmail_ReturnsMatchingClients()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "unique.email@example.com", firstName: "Test", lastName: "User");
        var user2 = await _fixture.CreateTestUserAsync(email: "other@example.com", firstName: "Other", lastName: "User");

        await _fixture.CreateTestClientProfileAsync(user1);
        await _fixture.CreateTestClientProfileAsync(user2);

        // Act
        var results = await _clientService.SearchClientsAsync("unique.email");

        // Assert
        results.Should().HaveCount(1);
        results.First().User.Email.Should().Be("unique.email@example.com");
    }

    [Fact]
    public async Task SearchClientsAsync_WithEmptySearchTerm_ReturnsAllClients()
    {
        // Arrange
        var user1 = await _fixture.CreateTestUserAsync(email: "a@example.com", firstName: "A", lastName: "User");
        var user2 = await _fixture.CreateTestUserAsync(email: "b@example.com", firstName: "B", lastName: "User");

        await _fixture.CreateTestClientProfileAsync(user1);
        await _fixture.CreateTestClientProfileAsync(user2);

        // Act
        var results = await _clientService.SearchClientsAsync("");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchClientsAsync_WithWhitespaceSearchTerm_ReturnsAllClients()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync();
        await _fixture.CreateTestClientProfileAsync(user);

        // Act
        var results = await _clientService.SearchClientsAsync("   ");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchClientsAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var user = await _fixture.CreateTestUserAsync(email: "test@example.com", firstName: "Test", lastName: "User");
        await _fixture.CreateTestClientProfileAsync(user);

        // Act
        var results = await _clientService.SearchClientsAsync("NonExistentName");

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Gallery Access Operations

    [Fact]
    public async Task GetClientGalleryAccessesAsync_ReturnsActiveAccesses()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var gallery = new Gallery
        {
            Name = "Test Gallery",
            Description = "Test Description",
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(30)
        };
        _fixture.DbContext.Galleries.Add(gallery);
        await _fixture.DbContext.SaveChangesAsync();

        var access = new GalleryAccess
        {
            GalleryId = gallery.Id,
            ClientProfileId = clientProfile.Id,
            IsActive = true,
            GrantedDate = DateTime.UtcNow
        };
        _fixture.DbContext.GalleryAccesses.Add(access);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var results = await _clientService.GetClientGalleryAccessesAsync(clientProfile.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().Gallery.Should().NotBeNull();
        results.First().Gallery!.Name.Should().Be("Test Gallery");
    }

    [Fact]
    public async Task GetClientGalleryAccessesAsync_ExcludesInactiveAccesses()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var gallery = new Gallery
        {
            Name = "Test Gallery",
            Description = "Test Description",
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(30)
        };
        _fixture.DbContext.Galleries.Add(gallery);
        await _fixture.DbContext.SaveChangesAsync();

        var access = new GalleryAccess
        {
            GalleryId = gallery.Id,
            ClientProfileId = clientProfile.Id,
            IsActive = false, // Inactive access
            GrantedDate = DateTime.UtcNow
        };
        _fixture.DbContext.GalleryAccesses.Add(access);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var results = await _clientService.GetClientGalleryAccessesAsync(clientProfile.Id);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClientGalleryAccessesAsync_OrdersByGrantedDateDescending()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var gallery1 = new Gallery { Name = "Gallery 1", Description = "Desc 1", ExpiryDate = DateTime.UtcNow.AddDays(30) };
        var gallery2 = new Gallery { Name = "Gallery 2", Description = "Desc 2", ExpiryDate = DateTime.UtcNow.AddDays(30) };
        _fixture.DbContext.Galleries.AddRange(gallery1, gallery2);
        await _fixture.DbContext.SaveChangesAsync();

        var olderAccess = new GalleryAccess
        {
            GalleryId = gallery1.Id,
            ClientProfileId = clientProfile.Id,
            IsActive = true,
            GrantedDate = DateTime.UtcNow.AddDays(-10)
        };
        var newerAccess = new GalleryAccess
        {
            GalleryId = gallery2.Id,
            ClientProfileId = clientProfile.Id,
            IsActive = true,
            GrantedDate = DateTime.UtcNow
        };
        _fixture.DbContext.GalleryAccesses.AddRange(olderAccess, newerAccess);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var results = (await _clientService.GetClientGalleryAccessesAsync(clientProfile.Id)).ToList();

        // Assert
        results.Should().HaveCount(2);
        results[0].Gallery!.Name.Should().Be("Gallery 2"); // Newer first
        results[1].Gallery!.Name.Should().Be("Gallery 1");
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

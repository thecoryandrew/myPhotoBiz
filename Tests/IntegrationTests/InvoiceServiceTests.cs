using FluentAssertions;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for Invoice workflow operations
/// </summary>
public class InvoiceServiceTests : IDisposable
{
    private readonly TestFixture _fixture;
    private readonly IInvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        _fixture = new TestFixture();
        _invoiceService = _fixture.GetService<IInvoiceService>();
    }

    #region Create Operations

    [Fact]
    public async Task CreateInvoiceAsync_WithValidData_CreatesInvoiceSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice = CreateTestInvoice(clientProfile.Id);

        // Act
        var result = await _invoiceService.CreateInvoiceAsync(invoice);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ClientProfileId.Should().Be(clientProfile.Id);
        result.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithoutInvoiceNumber_GeneratesInvoiceNumber()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice = CreateTestInvoice(clientProfile.Id);
        invoice.InvoiceNumber = string.Empty;

        // Act
        var result = await _invoiceService.CreateInvoiceAsync(invoice);

        // Assert
        result.InvoiceNumber.Should().NotBeNullOrEmpty();
        result.InvoiceNumber.Should().StartWith("INV-");
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithNullInvoice_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _invoiceService.CreateInvoiceAsync(null!));
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithInvoiceItems_CreatesWithItems()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice = CreateTestInvoice(clientProfile.Id);
        invoice.InvoiceItems = new List<InvoiceItem>
        {
            new() { Description = "Photography Session", Quantity = 1, UnitPrice = 300m },
            new() { Description = "Photo Prints", Quantity = 10, UnitPrice = 20m }
        };

        // Act
        var result = await _invoiceService.CreateInvoiceAsync(invoice);

        // Assert
        result.InvoiceItems.Should().HaveCount(2);
    }

    #endregion

    #region Read Operations

    [Fact]
    public async Task GetAllInvoicesAsync_ReturnsAllInvoices()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-001"));
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-002"));

        // Act
        var results = await _invoiceService.GetAllInvoicesAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_OrdersByInvoiceDateDescending()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var older = CreateTestInvoice(clientProfile.Id, "INV-OLDER");
        older.InvoiceDate = DateTime.Today.AddDays(-10);

        var newer = CreateTestInvoice(clientProfile.Id, "INV-NEWER");
        newer.InvoiceDate = DateTime.Today;

        await _invoiceService.CreateInvoiceAsync(older);
        await _invoiceService.CreateInvoiceAsync(newer);

        // Act
        var results = (await _invoiceService.GetAllInvoicesAsync()).ToList();

        // Assert
        results[0].InvoiceNumber.Should().Be("INV-NEWER");
        results[1].InvoiceNumber.Should().Be("INV-OLDER");
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_WithExistingId_ReturnsInvoice()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id));

        // Act
        var result = await _invoiceService.GetInvoiceByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _invoiceService.GetInvoiceByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoiceByNumberAsync_WithExistingNumber_ReturnsInvoice()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-UNIQUE-123"));

        // Act
        var result = await _invoiceService.GetInvoiceByNumberAsync("INV-UNIQUE-123");

        // Assert
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().Be("INV-UNIQUE-123");
    }

    [Fact]
    public async Task GetInvoiceByNumberAsync_WithNonExistingNumber_ReturnsNull()
    {
        // Act
        var result = await _invoiceService.GetInvoiceByNumberAsync("NON-EXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFilteredInvoicesAsync_ByClientId_ReturnsClientInvoices()
    {
        // Arrange
        var clientProfile1 = await _fixture.CreateTestClientProfileAsync();
        var user2 = await _fixture.CreateTestUserAsync(email: "client2@example.com", firstName: "Client", lastName: "Two");
        var clientProfile2 = await _fixture.CreateTestClientProfileAsync(user2);

        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile1.Id, "INV-C1"));
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile2.Id, "INV-C2"));

        // Act
        var results = await _invoiceService.GetFilteredInvoicesAsync(null, clientProfile1.Id, null, 1, 20);

        // Assert
        results.Should().HaveCount(1);
        results.First().InvoiceNumber.Should().Be("INV-C1");
    }

    [Fact]
    public async Task GetFilteredInvoicesAsync_ByStatus_ReturnsMatchingInvoices()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var pending = CreateTestInvoice(clientProfile.Id, "INV-PENDING");
        pending.Status = InvoiceStatus.Pending;

        var paid = CreateTestInvoice(clientProfile.Id, "INV-PAID");
        paid.Status = InvoiceStatus.Paid;

        await _invoiceService.CreateInvoiceAsync(pending);
        await _invoiceService.CreateInvoiceAsync(paid);

        // Act
        var results = await _invoiceService.GetFilteredInvoicesAsync(null, null, InvoiceStatus.Pending, 1, 20);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task GetFilteredInvoicesAsync_BySearchTerm_ReturnsMatchingInvoices()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-SEARCH-ME"));
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-OTHER"));

        // Act
        var results = await _invoiceService.GetFilteredInvoicesAsync("SEARCH", null, null, 1, 20);

        // Assert
        results.Should().HaveCount(1);
        results.First().InvoiceNumber.Should().Contain("SEARCH");
    }

    [Fact]
    public async Task GetRecentInvoicesAsync_ReturnsRequestedCount()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        for (int i = 0; i < 10; i++)
        {
            await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, $"INV-{i:D3}"));
        }

        // Act
        var results = await _invoiceService.GetRecentInvoicesAsync(5);

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetOverdueInvoicesAsync_ReturnsOnlyOverdueUnpaidInvoices()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var overdue = CreateTestInvoice(clientProfile.Id, "INV-OVERDUE");
        overdue.DueDate = DateTime.Today.AddDays(-5);
        overdue.Status = InvoiceStatus.Pending;

        var overduePaid = CreateTestInvoice(clientProfile.Id, "INV-OVERDUE-PAID");
        overduePaid.DueDate = DateTime.Today.AddDays(-5);
        overduePaid.Status = InvoiceStatus.Paid;

        var notDue = CreateTestInvoice(clientProfile.Id, "INV-NOT-DUE");
        notDue.DueDate = DateTime.Today.AddDays(10);
        notDue.Status = InvoiceStatus.Pending;

        await _invoiceService.CreateInvoiceAsync(overdue);
        await _invoiceService.CreateInvoiceAsync(overduePaid);
        await _invoiceService.CreateInvoiceAsync(notDue);

        // Act
        var results = await _invoiceService.GetOverdueInvoicesAsync();

        // Assert
        results.Should().HaveCount(1);
        results.First().InvoiceNumber.Should().Be("INV-OVERDUE");
    }

    [Fact]
    public async Task GetInvoicesDueSoonAsync_ReturnsInvoicesDueWithinDays()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();

        var dueSoon = CreateTestInvoice(clientProfile.Id, "INV-DUE-SOON");
        dueSoon.DueDate = DateTime.Today.AddDays(3);
        dueSoon.Status = InvoiceStatus.Pending;

        var dueLater = CreateTestInvoice(clientProfile.Id, "INV-DUE-LATER");
        dueLater.DueDate = DateTime.Today.AddDays(20);
        dueLater.Status = InvoiceStatus.Pending;

        await _invoiceService.CreateInvoiceAsync(dueSoon);
        await _invoiceService.CreateInvoiceAsync(dueLater);

        // Act
        var results = await _invoiceService.GetInvoicesDueSoonAsync(7);

        // Assert
        results.Should().HaveCount(1);
        results.First().InvoiceNumber.Should().Be("INV-DUE-SOON");
    }

    [Fact]
    public async Task GetClientInvoicesAsync_ReturnsOnlyClientInvoices()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var user2 = await _fixture.CreateTestUserAsync(email: "other@example.com", firstName: "Other", lastName: "Client");
        var otherClient = await _fixture.CreateTestClientProfileAsync(user2);

        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-MINE"));
        await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(otherClient.Id, "INV-OTHER"));

        // Act
        var results = await _invoiceService.GetClientInvoicesAsync(clientProfile.Id, null, 1, 20);

        // Assert
        results.Should().HaveCount(1);
        results.First().InvoiceNumber.Should().Be("INV-MINE");
    }

    #endregion

    #region Update Operations

    [Fact]
    public async Task UpdateInvoiceAsync_WithValidData_UpdatesInvoiceSuccessfully()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id));
        var originalUpdatedDate = created.UpdatedDate;

        await Task.Delay(10);

        created.Amount = 750m;
        created.Notes = "Updated notes";

        // Act
        await _invoiceService.UpdateInvoiceAsync(created);

        // Assert
        var updated = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        updated.Should().NotBeNull();
        updated!.Amount.Should().Be(750m);
        updated.Notes.Should().Be("Updated notes");
        updated.UpdatedDate.Should().BeAfter(originalUpdatedDate);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_UpdatesStatusCorrectly()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id));
        created.Status.Should().Be(InvoiceStatus.Draft);

        // Act
        await _invoiceService.UpdateInvoiceStatusAsync(created.Id, InvoiceStatus.Pending);

        // Assert
        var updated = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        updated!.Status.Should().Be(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_WithPaidStatus_SetsPaidDate()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id));
        var paidDate = DateTime.Today;

        // Act
        await _invoiceService.UpdateInvoiceStatusAsync(created.Id, InvoiceStatus.Paid, paidDate);

        // Assert
        var updated = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        updated!.Status.Should().Be(InvoiceStatus.Paid);
        updated.PaidDate.Should().Be(paidDate);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_WithNonExistingId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _invoiceService.UpdateInvoiceStatusAsync(99999, InvoiceStatus.Paid));
    }

    [Fact]
    public async Task BulkUpdateInvoiceStatusAsync_UpdatesMultipleInvoices()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice1 = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-BULK-1"));
        var invoice2 = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-BULK-2"));
        var invoice3 = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id, "INV-BULK-3"));

        var idsToUpdate = new[] { invoice1.Id, invoice2.Id };

        // Act
        await _invoiceService.BulkUpdateInvoiceStatusAsync(idsToUpdate, InvoiceStatus.Cancelled);

        // Assert
        var updated1 = await _invoiceService.GetInvoiceByIdAsync(invoice1.Id);
        var updated2 = await _invoiceService.GetInvoiceByIdAsync(invoice2.Id);
        var unchanged = await _invoiceService.GetInvoiceByIdAsync(invoice3.Id);

        updated1!.Status.Should().Be(InvoiceStatus.Cancelled);
        updated2!.Status.Should().Be(InvoiceStatus.Cancelled);
        unchanged!.Status.Should().Be(InvoiceStatus.Draft); // Should remain unchanged
    }

    [Fact]
    public async Task MarkInvoiceAsPaidAsync_MarksInvoiceAsPaid()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice = CreateTestInvoice(clientProfile.Id);
        invoice.Status = InvoiceStatus.Pending;
        var created = await _invoiceService.CreateInvoiceAsync(invoice);
        var paidDate = DateTime.Today;

        // Act
        await _invoiceService.MarkInvoiceAsPaidAsync(created.Id, paidDate);

        // Assert
        var updated = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        updated!.Status.Should().Be(InvoiceStatus.Paid);
        updated.PaidDate.Should().Be(paidDate);
    }

    [Fact]
    public async Task ApplyPaymentAsync_UpdatesAmountAndStatus()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var created = await _invoiceService.CreateInvoiceAsync(CreateTestInvoice(clientProfile.Id));
        var paidDate = DateTime.Today;
        var paidAmount = 450m;

        // Act
        await _invoiceService.ApplyPaymentAsync(created.Id, paidAmount, paidDate);

        // Assert
        var updated = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        updated!.Status.Should().Be(InvoiceStatus.Paid);
        updated.Amount.Should().Be(paidAmount);
        updated.PaidDate.Should().Be(paidDate);
    }

    #endregion

    #region Duplicate Operations

    [Fact]
    public async Task DuplicateInvoiceAsync_CreatesCopyWithNewInvoiceNumber()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var original = CreateTestInvoice(clientProfile.Id, "INV-ORIGINAL");
        original.Amount = 1000m;
        original.Tax = 100m;
        original.Notes = "Original notes";
        await _invoiceService.CreateInvoiceAsync(original);

        // Act
        var copy = await _invoiceService.DuplicateInvoiceAsync(original.Id);

        // Assert
        copy.Should().NotBeNull();
        copy!.Id.Should().NotBe(original.Id);
        copy.InvoiceNumber.Should().NotBe(original.InvoiceNumber);
        copy.Amount.Should().Be(original.Amount);
        copy.Tax.Should().Be(original.Tax);
        copy.Notes.Should().Be(original.Notes);
        copy.Status.Should().Be(InvoiceStatus.Draft);
        copy.ClientProfileId.Should().Be(original.ClientProfileId);
    }

    [Fact]
    public async Task DuplicateInvoiceAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _invoiceService.DuplicateInvoiceAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateInvoiceAsync_CopiesInvoiceItems()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var original = CreateTestInvoice(clientProfile.Id);
        original.InvoiceItems = new List<InvoiceItem>
        {
            new() { Description = "Item 1", Quantity = 2, UnitPrice = 100m },
            new() { Description = "Item 2", Quantity = 1, UnitPrice = 200m }
        };
        await _invoiceService.CreateInvoiceAsync(original);

        // Act
        var copy = await _invoiceService.DuplicateInvoiceAsync(original.Id);

        // Assert
        copy.Should().NotBeNull();
        copy!.InvoiceItems.Should().HaveCount(2);
        copy.InvoiceItems!.Select(i => i.Description).Should().Contain("Item 1");
        copy.InvoiceItems!.Select(i => i.Description).Should().Contain("Item 2");
    }

    #endregion

    #region Invoice Lifecycle Tests

    [Fact]
    public async Task InvoiceLifecycle_DraftToPendingToPaid()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice = CreateTestInvoice(clientProfile.Id);
        invoice.Status = InvoiceStatus.Draft;
        var created = await _invoiceService.CreateInvoiceAsync(invoice);

        // Act & Assert - Draft
        created.Status.Should().Be(InvoiceStatus.Draft);

        // Act - Move to Pending
        await _invoiceService.UpdateInvoiceStatusAsync(created.Id, InvoiceStatus.Pending);
        var pending = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        pending!.Status.Should().Be(InvoiceStatus.Pending);

        // Act - Move to Paid
        await _invoiceService.MarkInvoiceAsPaidAsync(created.Id, DateTime.Today);
        var paid = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        paid!.Status.Should().Be(InvoiceStatus.Paid);
        paid.PaidDate.Should().NotBeNull();
    }

    [Fact]
    public async Task InvoiceLifecycle_DraftToCancelled()
    {
        // Arrange
        var clientProfile = await _fixture.CreateTestClientProfileAsync();
        var invoice = CreateTestInvoice(clientProfile.Id);
        var created = await _invoiceService.CreateInvoiceAsync(invoice);

        // Act - Cancel
        await _invoiceService.UpdateInvoiceStatusAsync(created.Id, InvoiceStatus.Cancelled);

        // Assert
        var cancelled = await _invoiceService.GetInvoiceByIdAsync(created.Id);
        cancelled!.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    #endregion

    #region Helper Methods

    private static Invoice CreateTestInvoice(int clientProfileId, string invoiceNumber = "INV-TEST-001")
    {
        return new Invoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Status = InvoiceStatus.Draft,
            Amount = 500m,
            Tax = 50m,
            Notes = "Test invoice",
            ClientProfileId = clientProfileId,
            UpdatedDate = DateTime.Now
        };
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

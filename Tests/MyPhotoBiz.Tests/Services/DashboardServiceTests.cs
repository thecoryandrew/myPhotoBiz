using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using Xunit;

namespace MyPhotoBiz.Tests.Services
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _contextOptions;

        public DashboardServiceTests()
        {
            // Create and open a connection to in-memory SQLite database
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // Configure context to use the in-memory SQLite database
            _contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create test database
            using var context = new ApplicationDbContext(_contextOptions);
            context.Database.EnsureCreated();

            SeedTestData(context);
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            // Add test invoices
            var invoices = new[]
            {
                new Invoice { InvoiceNumber = "INV-001", Status = InvoiceStatus.Paid, Amount = 100m, Tax = 10m, InvoiceDate = DateTime.Today.AddMonths(-1) },
                new Invoice { InvoiceNumber = "INV-002", Status = InvoiceStatus.Paid, Amount = 200m, Tax = 20m, InvoiceDate = DateTime.Today.AddMonths(-1) },
                new Invoice { InvoiceNumber = "INV-003", Status = InvoiceStatus.Pending, Amount = 300m, Tax = 30m, InvoiceDate = DateTime.Today },
                new Invoice { InvoiceNumber = "INV-004", Status = InvoiceStatus.Overdue, Amount = 400m, Tax = 40m, InvoiceDate = DateTime.Today.AddMonths(-2) }
            };

            context.Invoices.AddRange(invoices);

            // Add test photo shoots
            var photoShoots = new[]
            {
                new PhotoShoot { Title = "Test Shoot 1", Status = PhotoShootStatus.Scheduled, ScheduledDate = DateTime.Today.AddDays(1) },
                new PhotoShoot { Title = "Test Shoot 2", Status = PhotoShootStatus.Completed, ScheduledDate = DateTime.Today.AddDays(-1) }
            };

            context.PhotoShoots.AddRange(photoShoots);

            // Add test clients
            var clients = new[]
            {
                new Client { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
                new Client { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
            };

            context.Clients.AddRange(clients);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetDashboardDataAsync_CalculatesCorrectTotals()
        {
            // Arrange
            using var context = new ApplicationDbContext(_contextOptions);
            var service = new DashboardService(context);

            // Act
            var result = await service.GetDashboardDataAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(330m, await service.GetTotalRevenueAsync()); // 100+10 + 200+20
            Assert.Equal(770m, await service.GetOutstandingInvoicesAsync()); // 300+30 + 400+40
            Assert.Equal(2, await service.GetClientsCountAsync());
            Assert.Equal(1, await service.GetPendingPhotoShootsCountAsync());
        }

        [Fact]
        public async Task GetMonthlyRevenue_HandlesEmptyMonth()
        {
            // Arrange
            using var context = new ApplicationDbContext(_contextOptions);
            var service = new DashboardService(context);

            // Act
            var result = await service.GetDashboardDataAsync();

            // Assert
            Assert.NotNull(result.MonthlyRevenueData);
            // Current month should be 0 since test data is from previous months
            Assert.Equal(0m, result.MonthlyRevenueData[DateTime.Now.ToString("MMM yyyy")]);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
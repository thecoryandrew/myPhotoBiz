using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;

namespace IntegrationTests;

/// <summary>
/// Test fixture that provides a fresh database context and services for each test
/// </summary>
public class TestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    public ApplicationDbContext DbContext { get; }
    public IServiceProvider ServiceProvider => _serviceProvider;

    public TestFixture()
    {
        var services = new ServiceCollection();

        // Use unique database name per fixture instance to avoid test interference
        var dbName = $"TestDb_{Guid.NewGuid()}";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        // Add Identity services
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 4;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // Register application services
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IPhotoShootService, PhotoShootService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityService, ActivityService>();

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        DbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a test user and returns the ApplicationUser
    /// </summary>
    public async Task<ApplicationUser> CreateTestUserAsync(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        UserType userType = UserType.Client)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            UserType = userType,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Creates a test client profile linked to a user
    /// </summary>
    public async Task<ClientProfile> CreateTestClientProfileAsync(ApplicationUser? user = null)
    {
        user ??= await CreateTestUserAsync();

        var clientProfile = new ClientProfile
        {
            UserId = user.Id,
            User = user,
            PhoneNumber = "555-1234",
            Address = "123 Test Street",
            Notes = "Test client",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        DbContext.ClientProfiles.Add(clientProfile);
        await DbContext.SaveChangesAsync();

        return clientProfile;
    }

    /// <summary>
    /// Creates a test photographer profile linked to a user
    /// </summary>
    public async Task<PhotographerProfile> CreateTestPhotographerProfileAsync(ApplicationUser? user = null)
    {
        user ??= await CreateTestUserAsync(
            email: "photographer@example.com",
            firstName: "Photo",
            lastName: "Grapher",
            userType: UserType.Photographer);

        var profile = new PhotographerProfile
        {
            UserId = user.Id,
            User = user,
            Bio = "Test photographer bio",
            Specialties = "Portraits, Weddings",
            HourlyRate = 100m,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        DbContext.PhotographerProfiles.Add(profile);
        await DbContext.SaveChangesAsync();

        return profile;
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

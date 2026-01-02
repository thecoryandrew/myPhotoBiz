using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class PackageService : IPackageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityService _activityService;

        public PackageService(ApplicationDbContext context, IActivityService activityService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        }

        #region Service Packages

        public async Task<IEnumerable<ServicePackage>> GetAllPackagesAsync()
        {
            return await _context.ServicePackages
                .Include(sp => sp.AddOns.Where(a => a.IsActive))
                .OrderBy(sp => sp.DisplayOrder)
                .ThenBy(sp => sp.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<ServicePackage>> GetActivePackagesAsync()
        {
            return await _context.ServicePackages
                .Include(sp => sp.AddOns.Where(a => a.IsActive))
                .Where(sp => sp.IsActive)
                .OrderBy(sp => sp.DisplayOrder)
                .ThenBy(sp => sp.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<ServicePackage>> GetPackagesByCategoryAsync(string category)
        {
            return await _context.ServicePackages
                .Include(sp => sp.AddOns.Where(a => a.IsActive))
                .Where(sp => sp.IsActive && sp.Category == category)
                .OrderBy(sp => sp.DisplayOrder)
                .ThenBy(sp => sp.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<ServicePackage>> GetFeaturedPackagesAsync()
        {
            return await _context.ServicePackages
                .Include(sp => sp.AddOns.Where(a => a.IsActive))
                .Where(sp => sp.IsActive && sp.IsFeatured)
                .OrderBy(sp => sp.DisplayOrder)
                .ThenBy(sp => sp.Name)
                .ToListAsync();
        }

        public async Task<ServicePackage?> GetPackageByIdAsync(int id)
        {
            return await _context.ServicePackages
                .Include(sp => sp.AddOns)
                .FirstOrDefaultAsync(sp => sp.Id == id);
        }

        public async Task<ServicePackage> CreatePackageAsync(ServicePackage package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            package.CreatedDate = DateTime.UtcNow;
            package.UpdatedDate = DateTime.UtcNow;

            _context.ServicePackages.Add(package);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Created", "ServicePackage", package.Id,
                package.Name,
                $"Created package: {package.Name} ({package.Category}) - ${package.BasePrice}");

            return package;
        }

        public async Task<ServicePackage> UpdatePackageAsync(ServicePackage package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            var existing = await _context.ServicePackages.FindAsync(package.Id);
            if (existing == null)
                throw new InvalidOperationException("Package not found.");

            package.UpdatedDate = DateTime.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(package);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "ServicePackage", package.Id,
                package.Name,
                "Package details updated");

            return existing;
        }

        public async Task<bool> DeletePackageAsync(int id)
        {
            var package = await _context.ServicePackages
                .Include(sp => sp.BookingRequests)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (package == null) return false;

            // Check if package has any booking requests
            if (package.BookingRequests.Any())
                throw new InvalidOperationException("Cannot delete a package that has booking requests. Consider deactivating it instead.");

            _context.ServicePackages.Remove(package);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Deleted", "ServicePackage", id,
                package.Name,
                "Package deleted");

            return true;
        }

        public async Task<bool> TogglePackageActiveAsync(int id)
        {
            var package = await _context.ServicePackages.FindAsync(id);
            if (package == null) return false;

            package.IsActive = !package.IsActive;
            package.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "ServicePackage", id,
                package.Name,
                $"Package {(package.IsActive ? "activated" : "deactivated")}");

            return true;
        }

        public async Task<bool> TogglePackageFeaturedAsync(int id)
        {
            var package = await _context.ServicePackages.FindAsync(id);
            if (package == null) return false;

            package.IsFeatured = !package.IsFeatured;
            package.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Updated", "ServicePackage", id,
                package.Name,
                $"Package {(package.IsFeatured ? "marked as featured" : "removed from featured")}");

            return true;
        }

        #endregion

        #region Package Add-ons

        public async Task<IEnumerable<PackageAddOn>> GetPackageAddOnsAsync(int packageId)
        {
            return await _context.PackageAddOns
                .Where(pa => pa.ServicePackageId == packageId)
                .OrderBy(pa => pa.DisplayOrder)
                .ThenBy(pa => pa.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<PackageAddOn>> GetStandaloneAddOnsAsync()
        {
            return await _context.PackageAddOns
                .Where(pa => pa.IsStandalone && pa.IsActive)
                .OrderBy(pa => pa.DisplayOrder)
                .ThenBy(pa => pa.Name)
                .ToListAsync();
        }

        public async Task<PackageAddOn?> GetAddOnByIdAsync(int id)
        {
            return await _context.PackageAddOns
                .Include(pa => pa.ServicePackage)
                .FirstOrDefaultAsync(pa => pa.Id == id);
        }

        public async Task<PackageAddOn> CreateAddOnAsync(PackageAddOn addOn)
        {
            if (addOn == null) throw new ArgumentNullException(nameof(addOn));

            addOn.CreatedDate = DateTime.UtcNow;
            addOn.UpdatedDate = DateTime.UtcNow;

            _context.PackageAddOns.Add(addOn);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Created", "PackageAddOn", addOn.Id,
                addOn.Name,
                $"Created add-on: {addOn.Name} - ${addOn.Price}");

            return addOn;
        }

        public async Task<PackageAddOn> UpdateAddOnAsync(PackageAddOn addOn)
        {
            if (addOn == null) throw new ArgumentNullException(nameof(addOn));

            var existing = await _context.PackageAddOns.FindAsync(addOn.Id);
            if (existing == null)
                throw new InvalidOperationException("Add-on not found.");

            addOn.UpdatedDate = DateTime.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(addOn);
            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteAddOnAsync(int id)
        {
            var addOn = await _context.PackageAddOns.FindAsync(id);
            if (addOn == null) return false;

            _context.PackageAddOns.Remove(addOn);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync(
                "Deleted", "PackageAddOn", id,
                addOn.Name,
                "Add-on deleted");

            return true;
        }

        #endregion

        #region Categories

        public async Task<IEnumerable<string>> GetAllCategoriesAsync()
        {
            return await _context.ServicePackages
                .Where(sp => sp.IsActive)
                .Select(sp => sp.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        #endregion

        #region Statistics

        public async Task<int> GetActivePackagesCountAsync()
        {
            return await _context.ServicePackages.CountAsync(sp => sp.IsActive);
        }

        public async Task<decimal> GetAveragePackagePriceAsync()
        {
            var hasPackages = await _context.ServicePackages.AnyAsync(sp => sp.IsActive);
            if (!hasPackages) return 0;

            return await _context.ServicePackages
                .Where(sp => sp.IsActive)
                .AverageAsync(sp => sp.BasePrice);
        }

        #endregion
    }
}

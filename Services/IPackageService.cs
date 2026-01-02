using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IPackageService
    {
        // Service Packages
        Task<IEnumerable<ServicePackage>> GetAllPackagesAsync();
        Task<IEnumerable<ServicePackage>> GetActivePackagesAsync();
        Task<IEnumerable<ServicePackage>> GetPackagesByCategoryAsync(string category);
        Task<IEnumerable<ServicePackage>> GetFeaturedPackagesAsync();
        Task<ServicePackage?> GetPackageByIdAsync(int id);
        Task<ServicePackage> CreatePackageAsync(ServicePackage package);
        Task<ServicePackage> UpdatePackageAsync(ServicePackage package);
        Task<bool> DeletePackageAsync(int id);
        Task<bool> TogglePackageActiveAsync(int id);
        Task<bool> TogglePackageFeaturedAsync(int id);

        // Package Add-ons
        Task<IEnumerable<PackageAddOn>> GetPackageAddOnsAsync(int packageId);
        Task<IEnumerable<PackageAddOn>> GetStandaloneAddOnsAsync();
        Task<PackageAddOn?> GetAddOnByIdAsync(int id);
        Task<PackageAddOn> CreateAddOnAsync(PackageAddOn addOn);
        Task<PackageAddOn> UpdateAddOnAsync(PackageAddOn addOn);
        Task<bool> DeleteAddOnAsync(int id);

        // Categories
        Task<IEnumerable<string>> GetAllCategoriesAsync();

        // Statistics
        Task<int> GetActivePackagesCountAsync();
        Task<decimal> GetAveragePackagePriceAsync();
    }
}

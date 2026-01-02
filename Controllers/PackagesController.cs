using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    public class PackagesController : Controller
    {
        private readonly IPackageService _packageService;
        private readonly IActivityService _activityService;

        public PackagesController(IPackageService packageService, IActivityService activityService)
        {
            _packageService = packageService;
            _activityService = activityService;
        }

        #region Public Views

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? category = null)
        {
            var packages = string.IsNullOrEmpty(category)
                ? await _packageService.GetActivePackagesAsync()
                : await _packageService.GetPackagesByCategoryAsync(category);

            var categories = await _packageService.GetAllCategoriesAsync();

            ViewBag.Categories = categories;
            ViewBag.CurrentCategory = category;

            return View(packages);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var package = await _packageService.GetPackageByIdAsync(id);
            if (package == null || !package.IsActive) return NotFound();

            return View(package);
        }

        #endregion

        #region Admin Management

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var packages = await _packageService.GetAllPackagesAsync();
            return View(packages);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var model = new ServicePackageViewModel
            {
                DurationHours = 2,
                IncludedPhotos = 50,
                NumberOfLocations = 1,
                IsActive = true
            };
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicePackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var package = new ServicePackage
            {
                Name = model.Name,
                Description = model.Description,
                DetailedDescription = model.DetailedDescription,
                Category = model.Category,
                BasePrice = model.BasePrice,
                DiscountedPrice = model.DiscountedPrice,
                DurationHours = model.DurationHours,
                IncludedPhotos = model.IncludedPhotos,
                IncludesPrints = model.IncludesPrints,
                NumberOfPrints = model.NumberOfPrints,
                IncludesAlbum = model.IncludesAlbum,
                IncludesDigitalGallery = model.IncludesDigitalGallery,
                NumberOfLocations = model.NumberOfLocations,
                OutfitChanges = model.OutfitChanges,
                IncludedFeatures = model.IncludedFeatures,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                IsFeatured = model.IsFeatured,
                CoverImagePath = model.CoverImagePath
            };

            await _packageService.CreatePackageAsync(package);
            TempData["Success"] = "Package created successfully.";

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var package = await _packageService.GetPackageByIdAsync(id);
            if (package == null) return NotFound();

            var model = new ServicePackageViewModel
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                DetailedDescription = package.DetailedDescription,
                Category = package.Category,
                BasePrice = package.BasePrice,
                DiscountedPrice = package.DiscountedPrice,
                DurationHours = package.DurationHours,
                IncludedPhotos = package.IncludedPhotos,
                IncludesPrints = package.IncludesPrints,
                NumberOfPrints = package.NumberOfPrints,
                IncludesAlbum = package.IncludesAlbum,
                IncludesDigitalGallery = package.IncludesDigitalGallery,
                NumberOfLocations = package.NumberOfLocations,
                OutfitChanges = package.OutfitChanges,
                IncludedFeatures = package.IncludedFeatures,
                DisplayOrder = package.DisplayOrder,
                IsActive = package.IsActive,
                IsFeatured = package.IsFeatured,
                CoverImagePath = package.CoverImagePath
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServicePackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _packageService.GetPackageByIdAsync(model.Id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.DetailedDescription = model.DetailedDescription;
            existing.Category = model.Category;
            existing.BasePrice = model.BasePrice;
            existing.DiscountedPrice = model.DiscountedPrice;
            existing.DurationHours = model.DurationHours;
            existing.IncludedPhotos = model.IncludedPhotos;
            existing.IncludesPrints = model.IncludesPrints;
            existing.NumberOfPrints = model.NumberOfPrints;
            existing.IncludesAlbum = model.IncludesAlbum;
            existing.IncludesDigitalGallery = model.IncludesDigitalGallery;
            existing.NumberOfLocations = model.NumberOfLocations;
            existing.OutfitChanges = model.OutfitChanges;
            existing.IncludedFeatures = model.IncludedFeatures;
            existing.DisplayOrder = model.DisplayOrder;
            existing.IsActive = model.IsActive;
            existing.IsFeatured = model.IsFeatured;
            existing.CoverImagePath = model.CoverImagePath;

            await _packageService.UpdatePackageAsync(existing);
            TempData["Success"] = "Package updated successfully.";

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _packageService.DeletePackageAsync(id);
                TempData["Success"] = "Package deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            await _packageService.TogglePackageActiveAsync(id);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            await _packageService.TogglePackageFeaturedAsync(id);
            return RedirectToAction(nameof(Manage));
        }

        #endregion

        #region Add-ons Management

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOns(int? packageId = null)
        {
            IEnumerable<PackageAddOn> addOns;

            if (packageId.HasValue)
            {
                addOns = await _packageService.GetPackageAddOnsAsync(packageId.Value);
                var package = await _packageService.GetPackageByIdAsync(packageId.Value);
                ViewBag.Package = package;
            }
            else
            {
                addOns = await _packageService.GetStandaloneAddOnsAsync();
            }

            ViewBag.PackageId = packageId;
            return View(addOns);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateAddOn(int? packageId = null)
        {
            var model = new PackageAddOnViewModel
            {
                ServicePackageId = packageId,
                IsActive = true
            };
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddOn(PackageAddOnViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var addOn = new PackageAddOn
            {
                ServicePackageId = model.ServicePackageId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Category = model.Category,
                IsStandalone = model.IsStandalone,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder
            };

            await _packageService.CreateAddOnAsync(addOn);
            TempData["Success"] = "Add-on created successfully.";

            return RedirectToAction(nameof(AddOns), new { packageId = model.ServicePackageId });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditAddOn(int id)
        {
            var addOn = await _packageService.GetAddOnByIdAsync(id);
            if (addOn == null) return NotFound();

            var model = new PackageAddOnViewModel
            {
                Id = addOn.Id,
                ServicePackageId = addOn.ServicePackageId,
                Name = addOn.Name,
                Description = addOn.Description,
                Price = addOn.Price,
                Category = addOn.Category,
                IsStandalone = addOn.IsStandalone,
                IsActive = addOn.IsActive,
                DisplayOrder = addOn.DisplayOrder
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddOn(PackageAddOnViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _packageService.GetAddOnByIdAsync(model.Id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.Category = model.Category;
            existing.IsStandalone = model.IsStandalone;
            existing.IsActive = model.IsActive;
            existing.DisplayOrder = model.DisplayOrder;

            await _packageService.UpdateAddOnAsync(existing);
            TempData["Success"] = "Add-on updated successfully.";

            return RedirectToAction(nameof(AddOns), new { packageId = model.ServicePackageId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddOn(int id, int? packageId)
        {
            await _packageService.DeleteAddOnAsync(id);
            TempData["Success"] = "Add-on deleted successfully.";

            return RedirectToAction(nameof(AddOns), new { packageId });
        }

        #endregion

        #region API Endpoints

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPackages(string? category = null)
        {
            var packages = string.IsNullOrEmpty(category)
                ? await _packageService.GetActivePackagesAsync()
                : await _packageService.GetPackagesByCategoryAsync(category);

            return Json(packages.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Category,
                p.BasePrice,
                p.DiscountedPrice,
                p.EffectivePrice,
                p.HasDiscount,
                p.DiscountPercentage,
                p.DurationHours,
                p.IncludedPhotos,
                p.IsFeatured,
                Features = p.FeaturesList
            }));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _packageService.GetAllCategoriesAsync();
            return Json(categories);
        }

        #endregion
    }
}

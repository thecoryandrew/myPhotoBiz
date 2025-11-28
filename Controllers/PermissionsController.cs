using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{

    public class PermissionsController : Controller
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(IPermissionService permissionService, ILogger<PermissionsController> logger)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Permissions
        public async Task<IActionResult> Index()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();
                var model = new PermissionsIndexViewModel
                {
                    Permissions = permissions
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading permissions index");
                TempData["ErrorMessage"] = "An error occurred while loading permissions.";
                return View(new PermissionsIndexViewModel());
            }
        }

        // GET: Permissions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var permission = await _permissionService.GetPermissionByIdAsync(id);
                if (permission == null)
                {
                    return NotFound();
                }
                return PartialView("_PermissionDetailsModal", permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading permission details for ID: {PermissionId}", id);
                return StatusCode(500, "Error loading permission details");
            }
        }

        // GET: Permissions/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var roles = await _permissionService.GetAllRolesAsync();
                var model = new CreatePermissionViewModel
                {
                    AvailableRoles = roles
                };
                return PartialView("_CreatePermissionModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create permission form");
                return StatusCode(500, "Error loading create form");
            }
        }

        // POST: Permissions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePermissionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var roles = await _permissionService.GetAllRolesAsync();
                    model.AvailableRoles = roles;
                    return PartialView("_CreatePermissionModal", model);
                }

                var permission = await _permissionService.CreatePermissionAsync(
                    model.Name,
                    model.Description,
                    model.SelectedRoles ?? new List<string>());

                TempData["SuccessMessage"] = "Permission created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the permission.");
                var roles = await _permissionService.GetAllRolesAsync();
                model.AvailableRoles = roles;
                return PartialView("_CreatePermissionModal", model);
            }
        }

        // GET: Permissions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var permission = await _permissionService.GetPermissionByIdAsync(id);
                if (permission == null)
                {
                    return NotFound();
                }

                var roles = await _permissionService.GetAllRolesAsync();
                var model = new EditPermissionViewModel
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    Description = permission.Description,
                    SelectedRoles = permission.AssignedRoles.Select(r => r.RoleId).ToList(),
                    AvailableRoles = roles
                };

                return PartialView("_EditPermissionModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit permission form for ID: {PermissionId}", id);
                return StatusCode(500, "Error loading edit form");
            }
        }

        // POST: Permissions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditPermissionViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    var roles = await _permissionService.GetAllRolesAsync();
                    model.AvailableRoles = roles;
                    return PartialView("_EditPermissionModal", model);
                }

                await _permissionService.UpdatePermissionAsync(
                    model.Id,
                    model.Name,
                    model.Description,
                    model.SelectedRoles ?? new List<string>());

                TempData["SuccessMessage"] = "Permission updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission with ID: {PermissionId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the permission.");
                var roles = await _permissionService.GetAllRolesAsync();
                model.AvailableRoles = roles;
                return PartialView("_EditPermissionModal", model);
            }
        }

        // POST: Permissions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _permissionService.DeletePermissionAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Permission deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Permission not found." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission with ID: {PermissionId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the permission." });
            }
        }
    }
}

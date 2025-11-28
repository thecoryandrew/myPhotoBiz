using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;
using System.IO;

namespace MyPhotoBiz.Controllers
{
    public class RolesController : Controller
    {
        private readonly IRolesService _rolesService;
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;

        public RolesController(
            IRolesService rolesService,
            IRazorViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataDictionaryFactory)
        {
            _rolesService = rolesService ?? throw new ArgumentNullException(nameof(rolesService));
            _viewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
            _tempDataDictionaryFactory = tempDataDictionaryFactory ?? throw new ArgumentNullException(nameof(tempDataDictionaryFactory));
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
            var model = await _rolesService.GetRolesIndexViewModelAsync();
            return View(model);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            var vm = new CreateRoleViewModel
            {
                AvailablePermissions = _rolesService.GetAllAvailablePermissionsAsync().GetAwaiter().GetResult()
            };
            return PartialView("_CreateRoleModal", vm);
        }

        // POST: Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var result = await _rolesService.CreateRoleAsync(model.RoleName);
            
            if (result.Succeeded)
            {
                var role = await _rolesService.GetRoleByNameAsync(model.RoleName);
                if (role != null)
                {
                    var vm = await _rolesService.GetRoleViewModelAsync(role.Id);
                    if (vm != null)
                    {
                        var html = await RenderViewAsync("_RoleCard", vm);
                        return Json(new { success = true, html, roleId = role.Id });
                    }
                }
                
                // Persist permissions if provided
                if (role != null && model.Permissions?.Any() == true)
                {
                    await _rolesService.SetRolePermissionsAsync(role.Id, model.Permissions);
                }

                return Json(new { success = true, message = "Role created successfully" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) 
                return NotFound();
            
            var role = await _rolesService.GetRoleByIdAsync(id);
            if (role == null) 
                return NotFound();

            var model = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name ?? string.Empty,
                Permissions = await _rolesService.GetPersistedRolePermissionsAsync(role.Id),
                AvailablePermissions = await _rolesService.GetAllAvailablePermissionsAsync()
            };
            
            return PartialView("_EditRoleModal", model);
        }

        // POST: Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rolesService.UpdateRoleAsync(model.Id, model.RoleName);

            if (result.Succeeded)
            {
                // Persist permissions first
                await _rolesService.SetRolePermissionsAsync(model.Id, model.Permissions ?? new List<string>());

                // Get updated role view model
                var vm = await _rolesService.GetRoleViewModelAsync(model.Id);
                if (vm != null)
                {
                    var html = await RenderViewAsync("_RoleCard", vm);
                    return Json(new { success = true, html, roleId = model.Id });
                }

                return Json(new { success = true, message = "Role updated successfully" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        // POST: Roles/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _rolesService.DeleteRoleAsync(id);
            
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Role deleted successfully" });
            }

            var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Error deleting role";
            return Json(new { success = false, message = errorMessage });
        }

        // GET: Roles/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) 
                return NotFound();
            
            var model = await _rolesService.GetRoleDetailsViewModelAsync(id);
            if (model == null) 
                return NotFound();

            return PartialView("_RoleDetailsModal", model);
        }

        // POST: Roles/AssignUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUser(string userId, string roleName)
        {
            var result = await _rolesService.AssignUserToRoleAsync(userId, roleName);
            
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User assigned to role successfully" });
            }

            var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Error assigning user to role";
            return Json(new { success = false, message = errorMessage });
        }

        // POST: Roles/RemoveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUser(string userId, string roleName)
        {
            var result = await _rolesService.RemoveUserFromRoleAsync(userId, roleName);
            
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User removed from role successfully" });
            }

            var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Error removing user from role";
            return Json(new { success = false, message = errorMessage });
        }

        // GET: Roles/GetUserRoles
        public async Task<IActionResult> GetUserRoles()
        {
            var userRoles = await _rolesService.GetUserRolesViewModelAsync();
            return Json(userRoles);
        }

        // Render a Razor partial view to string
        private async Task<string> RenderViewAsync(string viewName, object model)
        {
            var actionContext = ControllerContext;
            var viewResult = _viewEngine.FindView(actionContext, viewName, false);
            
            if (!viewResult.Success)
            {
                viewResult = _viewEngine.GetView(null, viewName, false);
                if (!viewResult.Success)
                {
                    throw new InvalidOperationException($"The view '{viewName}' was not found.");
                }
            }

            ViewData.Model = model;
            await using var sw = new StringWriter();
            var tempData = _tempDataDictionaryFactory.GetTempData(HttpContext);
            var viewContext = new ViewContext(
                actionContext, 
                viewResult.View, 
                ViewData, 
                tempData, 
                sw, 
                new HtmlHelperOptions());
            
            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
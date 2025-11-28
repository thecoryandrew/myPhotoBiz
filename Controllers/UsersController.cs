using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserManagementService userManagementService, ILogger<UsersController> logger)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManagementService.GetAllUsersAsync();
                var roles = await _userManagementService.GetAllRolesAsync();

                var model = new UsersIndexViewModel
                {
                    Users = users,
                    AvailableRoles = roles
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users index");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View(new UsersIndexViewModel());
            }
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserDetailsAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return PartialView("_UserDetailsModal", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                return StatusCode(500, "Error loading user details");
            }
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var roles = await _userManagementService.GetAllRolesAsync();
                var model = new CreateUserViewModel
                {
                    AvailableRoles = roles
                };
                return PartialView("_CreateUserModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user form");
                return StatusCode(500, "Error loading create form");
            }
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var roles = await _userManagementService.GetAllRolesAsync();
                    model.AvailableRoles = roles;
                    return PartialView("_CreateUserModal", model);
                }

                var result = await _userManagementService.CreateUserAsync(model);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var availableRoles = await _userManagementService.GetAllRolesAsync();
                model.AvailableRoles = availableRoles;
                return PartialView("_CreateUserModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the user.");
                var roles = await _userManagementService.GetAllRolesAsync();
                model.AvailableRoles = roles;
                return PartialView("_CreateUserModal", model);
            }
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserDetailsAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var roles = await _userManagementService.GetAllRolesAsync();

                // Convert user's role names to role IDs
                var selectedRoleIds = new List<string>();
                foreach (var roleName in user.Roles)
                {
                    var role = roles.FirstOrDefault(r => r.Name == roleName);
                    if (role != null)
                    {
                        selectedRoleIds.Add(role.Id);
                    }
                }

                var model = new EditUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    IsPhotographer = user.IsPhotographer,
                    IsActive = user.IsActive,
                    SelectedRoleIds = selectedRoleIds,
                    AvailableRoles = roles
                };

                return PartialView("_EditUserModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit user form for ID: {UserId}", id);
                return StatusCode(500, "Error loading edit form");
            }
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    var roles = await _userManagementService.GetAllRolesAsync();
                    model.AvailableRoles = roles;
                    return PartialView("_EditUserModal", model);
                }

                var result = await _userManagementService.UpdateUserAsync(model);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var availableRoles = await _userManagementService.GetAllRolesAsync();
                model.AvailableRoles = availableRoles;
                return PartialView("_EditUserModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the user.");
                var roles = await _userManagementService.GetAllRolesAsync();
                model.AvailableRoles = roles;
                return PartialView("_EditUserModal", model);
            }
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _userManagementService.DeleteUserAsync(id);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = result.Errors.FirstOrDefault()?.Description ?? "Error deleting user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the user." });
            }
        }

        // POST: Users/Lock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            try
            {
                var result = await _userManagementService.LockUserAsync(id, DateTimeOffset.Now.AddYears(100));
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User locked successfully." });
                }
                else
                {
                    return Json(new { success = false, message = result.Errors.FirstOrDefault()?.Description ?? "Error locking user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user with ID: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while locking the user." });
            }
        }

        // POST: Users/Unlock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            try
            {
                var result = await _userManagementService.UnlockUserAsync(id);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User unlocked successfully." });
                }
                else
                {
                    return Json(new { success = false, message = result.Errors.FirstOrDefault()?.Description ?? "Error unlocking user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user with ID: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while unlocking the user." });
            }
        }

        // GET: Users/ChangePassword/5
        public async Task<IActionResult> ChangePassword(string id)
        {
            try
            {
                var user = await _userManagementService.GetUserDetailsAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var model = new ChangePasswordViewModel
                {
                    UserId = id,
                    UserFullName = user.FullName,
                    UserEmail = user.Email
                };
                return PartialView("_ChangePasswordModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading change password form for user: {UserId}", id);
                return StatusCode(500, "Error loading change password form");
            }
        }

        // POST: Users/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return PartialView("_ChangePasswordModal", model);
                }

                var result = await _userManagementService.ChangePasswordAsync(model.UserId, model.NewPassword);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Password changed successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return PartialView("_ChangePasswordModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", model.UserId);
                ModelState.AddModelError(string.Empty, "An error occurred while changing the password.");
                return PartialView("_ChangePasswordModal", model);
            }
        }

        // Legacy actions (kept for compatibility)
        public IActionResult Permissions() => RedirectToAction("Index", "Permissions");
        public IActionResult Roles() => RedirectToAction("Index", "Roles");
    }
}

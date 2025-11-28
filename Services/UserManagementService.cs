using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<UserListViewModel>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");

                var users = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserListViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var permissions = await GetUserPermissionsAsync(user.Id);

                    userViewModels.Add(new UserListViewModel
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        ProfilePicture = user.ProfilePicture,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        LockoutEnabled = user.LockoutEnabled,
                        LockoutEnd = user.LockoutEnd,
                        Roles = roles.ToList(),
                        Permissions = permissions,
                        CreatedDate = DateTime.UtcNow // You might want to add this to ApplicationUser model
                    });
                }

                return userViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Retrieving user details for ID: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return null;

                var roles = await _userManager.GetRolesAsync(user);

                // Get permissions with full details
                var permissionNames = new HashSet<string>();
                foreach (var roleName in roles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var rolePermissions = await _context.Set<RolePermission>()
                            .Where(rp => rp.RoleId == role.Id)
                            .Select(rp => rp.Permission)
                            .ToListAsync();

                        foreach (var permName in rolePermissions)
                        {
                            if (!string.IsNullOrEmpty(permName))
                            {
                                permissionNames.Add(permName);
                            }
                        }
                    }
                }

                // Get full Permission objects from the permission names
                var permissions = await _context.Set<Permission>()
                    .Where(p => permissionNames.Contains(p.Name))
                    .ToListAsync();

                // Convert to PermissionViewModel
                var uniquePermissions = permissions
                    .Select(p => new PermissionViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        CreatedDate = p.CreatedDate
                    })
                    .ToList();

                // Get user statistics
                var clientCount = await _context.Clients.CountAsync(c => c.UserId == userId);
                var photoShootCount = await _context.Set<PhotoShoot>().CountAsync(ps => ps.PhotographerId == userId);
                var invoiceCount = await _context.Set<Invoice>()
                    .Where(i => i.Client != null && i.Client.UserId == userId)
                    .CountAsync();

                return new UserDetailsViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    IsPhotographer = user.IsPhotographer,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    Permissions = uniquePermissions,
                    ClientCount = clientCount,
                    PhotoShootCount = photoShootCount,
                    InvoiceCount = invoiceCount,
                    CreatedDate = DateTime.UtcNow,
                    LastLoginDate = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user details for ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityResult> CreateUserAsync(CreateUserViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating new user: {Email}", model.Email);

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = model.EmailConfirmed,
                    IsPhotographer = model.IsPhotographer,
                    IsActive = model.IsActive
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded && model.SelectedRoleIds?.Any() == true)
                {
                    // Convert role IDs to role names
                    var roleNames = new List<string>();
                    foreach (var roleId in model.SelectedRoleIds)
                    {
                        var role = await _roleManager.FindByIdAsync(roleId);
                        if (role != null)
                        {
                            roleNames.Add(role.Name!);
                        }
                    }

                    if (roleNames.Any())
                    {
                        await _userManager.AddToRolesAsync(user, roleNames);
                        _logger.LogInformation("Successfully created user with ID: {UserId} and assigned {RoleCount} roles", user.Id, roleNames.Count);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", model.Email);
                throw;
            }
        }

        public async Task<IdentityResult> UpdateUserAsync(EditUserViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating user with ID: {UserId}", model.Id);

                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found" });
                }

                user.Email = model.Email;
                user.UserName = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.EmailConfirmed = model.EmailConfirmed;
                user.PhoneNumberConfirmed = model.PhoneNumberConfirmed;
                user.TwoFactorEnabled = model.TwoFactorEnabled;
                user.LockoutEnabled = model.LockoutEnabled;
                user.IsPhotographer = model.IsPhotographer;
                user.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (model.SelectedRoleIds?.Any() == true)
                    {
                        // Convert role IDs to role names
                        var roleNames = new List<string>();
                        foreach (var roleId in model.SelectedRoleIds)
                        {
                            var role = await _roleManager.FindByIdAsync(roleId);
                            if (role != null)
                            {
                                roleNames.Add(role.Name!);
                            }
                        }

                        if (roleNames.Any())
                        {
                            await _userManager.AddToRolesAsync(user, roleNames);
                        }
                    }

                    _logger.LogInformation("Successfully updated user with ID: {UserId}", model.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", model.Id);
                throw;
            }
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Deleting user with ID: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found" });
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted user with ID: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, string newPassword)
        {
            try
            {
                _logger.LogInformation("Changing password for user: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found" });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully changed password for user: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityResult> AssignRolesToUserAsync(string userId, List<string> roleNames)
        {
            try
            {
                _logger.LogInformation("Assigning roles to user: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found" });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (roleNames?.Any() == true)
                {
                    var result = await _userManager.AddToRolesAsync(user, roleNames);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Successfully assigned roles to user: {UserId}", userId);
                    }
                    return result;
                }

                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning roles to user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return new List<string>();

                var roles = await _userManager.GetRolesAsync(user);
                var permissions = new HashSet<string>();

                foreach (var roleName in roles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var rolePermissions = await _context.Set<RolePermission>()
                            .Where(rp => rp.RoleId == role.Id && !string.IsNullOrEmpty(rp.Permission))
                            .Select(rp => rp.Permission)
                            .ToListAsync();

                        foreach (var permission in rolePermissions)
                        {
                            permissions.Add(permission);
                        }
                    }
                }

                return permissions.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityResult> LockUserAsync(string userId, DateTimeOffset? lockoutEnd)
        {
            try
            {
                _logger.LogInformation("Locking user: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found" });
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd ?? DateTimeOffset.MaxValue);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully locked user: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityResult> UnlockUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Unlocking user: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found" });
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, null);

                if (result.Succeeded)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    _logger.LogInformation("Successfully unlocked user: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }
    }
}

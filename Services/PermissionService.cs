using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<PermissionService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PermissionViewModel>> GetAllPermissionsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all permissions");

                var permissions = await _context.Set<Permission>()
                    .Include(p => p.RolePermissions)
                    .ToListAsync();

                var roleIds = permissions
                    .SelectMany(p => p.RolePermissions.Select(rp => rp.RoleId))
                    .Distinct()
                    .ToList();

                var roles = await _context.Roles
                    .Where(r => roleIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.Name ?? string.Empty);

                var permissionViewModels = new List<PermissionViewModel>();

                foreach (var permission in permissions)
                {
                    var assignedRoles = permission.RolePermissions
                        .Select(rp => roles.GetValueOrDefault(rp.RoleId, string.Empty))
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    var userCount = 0;
                    foreach (var roleName in assignedRoles)
                    {
                        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                        userCount += usersInRole.Count;
                    }

                    permissionViewModels.Add(new PermissionViewModel
                    {
                        Id = permission.Id,
                        Name = permission.Name,
                        Description = permission.Description,
                        CreatedDate = permission.CreatedDate,
                        UpdatedDate = permission.UpdatedDate,
                        AssignedRoles = assignedRoles,
                        UserCount = userCount
                    });
                }

                return permissionViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                throw;
            }
        }

        public async Task<PermissionDetailsViewModel?> GetPermissionByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving permission with ID: {PermissionId}", id);

                var permission = await _context.Set<Permission>()
                    .Include(p => p.RolePermissions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (permission == null)
                    return null;

                var rolePermissionInfos = new List<RolePermissionInfo>();

                foreach (var rp in permission.RolePermissions)
                {
                    var role = await _roleManager.FindByIdAsync(rp.RoleId);
                    if (role != null)
                    {
                        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                        rolePermissionInfos.Add(new RolePermissionInfo
                        {
                            RoleId = role.Id,
                            RoleName = role.Name ?? string.Empty,
                            UserCount = usersInRole.Count
                        });
                    }
                }

                return new PermissionDetailsViewModel
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    Description = permission.Description,
                    CreatedDate = permission.CreatedDate,
                    UpdatedDate = permission.UpdatedDate,
                    AssignedRoles = rolePermissionInfos,
                    TotalUsers = rolePermissionInfos.Sum(r => r.UserCount)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permission with ID: {PermissionId}", id);
                throw;
            }
        }

        public async Task<Permission> CreatePermissionAsync(string name, string? description, List<string> roleIds)
        {
            try
            {
                _logger.LogInformation("Creating permission: {PermissionName}", name);

                var permission = new Permission
                {
                    Name = name,
                    Description = description,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.Set<Permission>().Add(permission);
                await _context.SaveChangesAsync();

                if (roleIds?.Any() == true)
                {
                    foreach (var roleId in roleIds)
                    {
                        _context.Set<RolePermission>().Add(new RolePermission
                        {
                            RoleId = roleId,
                            Permission = permission.Name
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully created permission with ID: {PermissionId}", permission.Id);
                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission: {PermissionName}", name);
                throw;
            }
        }

        public async Task<Permission> UpdatePermissionAsync(int id, string name, string? description, List<string> roleIds)
        {
            try
            {
                _logger.LogInformation("Updating permission with ID: {PermissionId}", id);

                var permission = await _context.Set<Permission>().FindAsync(id);
                if (permission == null)
                {
                    throw new InvalidOperationException($"Permission with ID {id} not found");
                }

                var oldName = permission.Name;
                permission.Name = name;
                permission.Description = description;
                permission.UpdatedDate = DateTime.UtcNow;

                // Update RolePermissions table if name changed
                if (oldName != name)
                {
                    var oldRolePermissions = await _context.Set<RolePermission>()
                        .Where(rp => rp.Permission == oldName)
                        .ToListAsync();

                    foreach (var rp in oldRolePermissions)
                    {
                        rp.Permission = name;
                    }
                }

                // Update role assignments
                var existingRolePermissions = await _context.Set<RolePermission>()
                    .Where(rp => rp.Permission == name)
                    .ToListAsync();

                _context.Set<RolePermission>().RemoveRange(existingRolePermissions);

                if (roleIds?.Any() == true)
                {
                    foreach (var roleId in roleIds)
                    {
                        _context.Set<RolePermission>().Add(new RolePermission
                        {
                            RoleId = roleId,
                            Permission = name
                        });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated permission with ID: {PermissionId}", id);
                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission with ID: {PermissionId}", id);
                throw;
            }
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting permission with ID: {PermissionId}", id);

                var permission = await _context.Set<Permission>().FindAsync(id);
                if (permission == null)
                {
                    _logger.LogWarning("Permission with ID: {PermissionId} not found", id);
                    return false;
                }

                // Remove associated role permissions
                var rolePermissions = await _context.Set<RolePermission>()
                    .Where(rp => rp.Permission == permission.Name)
                    .ToListAsync();

                _context.Set<RolePermission>().RemoveRange(rolePermissions);
                _context.Set<Permission>().Remove(permission);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted permission with ID: {PermissionId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission with ID: {PermissionId}", id);
                throw;
            }
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }
    }
}

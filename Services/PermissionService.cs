using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public PermissionService(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<List<PermissionViewModel>> GetAllPermissionsAsync()
        {
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

            var result = new List<PermissionViewModel>(permissions.Count);

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

                result.Add(new PermissionViewModel
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

            return result;
        }

        public async Task<PermissionDetailsViewModel?> GetPermissionByIdAsync(int id)
        {
            var permission = await _context.Set<Permission>()
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permission == null) return null;

            var rolePermissionInfos = new List<RolePermissionInfo>();

            foreach (var rp in permission.RolePermissions)
            {
                var role = await _roleManager.FindByIdAsync(rp.RoleId);
                if (role == null) continue;

                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                rolePermissionInfos.Add(new RolePermissionInfo
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    UserCount = usersInRole.Count
                });
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

        public async Task<Permission> CreatePermissionAsync(string name, string? description, List<string> roleIds)
        {
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

            return permission;
        }

        public async Task<Permission> UpdatePermissionAsync(int id, string name, string? description, List<string> roleIds)
        {
            var permission = await _context.Set<Permission>().FindAsync(id)
                ?? throw new InvalidOperationException($"Permission with ID {id} not found");

            var oldName = permission.Name;
            permission.Name = name;
            permission.Description = description;
            permission.UpdatedDate = DateTime.UtcNow;

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
            return permission;
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            var permission = await _context.Set<Permission>().FindAsync(id);
            if (permission == null) return false;

            var rolePermissions = await _context.Set<RolePermission>()
                .Where(rp => rp.Permission == permission.Name)
                .ToListAsync();

            _context.Set<RolePermission>().RemoveRange(rolePermissions);
            _context.Set<Permission>().Remove(permission);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }
    }
}

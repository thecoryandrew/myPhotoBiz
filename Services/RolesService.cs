using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;


namespace MyPhotoBiz.Services
{
    public class RolesService : IRolesService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public RolesService(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Role Management

        public async Task<IEnumerable<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        public async Task<IdentityRole?> GetRoleByIdAsync(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
                return null;

            return await _roleManager.FindByIdAsync(roleId);
        }

        public async Task<IdentityRole?> GetRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return null;

            return await _roleManager.FindByNameAsync(roleName);
        }

        public async Task<IdentityResult> CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return IdentityResult.Failed(new IdentityError { Description = "Role name is required." });

            var role = new IdentityRole(roleName);
            return await _roleManager.CreateAsync(role);
        }

        public async Task<IdentityResult> UpdateRoleAsync(string roleId, string newRoleName)
        {
            var role = await GetRoleByIdAsync(roleId);
            if (role == null)
                return IdentityResult.Failed(new IdentityError { Description = "Role not found." });

            role.Name = newRoleName;
            role.NormalizedName = newRoleName?.ToUpperInvariant();
            
            return await _roleManager.UpdateAsync(role);
        }

        public async Task<IdentityResult> DeleteRoleAsync(string roleId)
        {
            var role = await GetRoleByIdAsync(roleId);
            if (role == null)
                return IdentityResult.Failed(new IdentityError { Description = "Role not found." });

            // Check if any users are in this role
            var usersInRole = await GetUsersInRoleAsync(role.Name ?? string.Empty);
            if (usersInRole.Any())
            {
                return IdentityResult.Failed(new IdentityError 
                { 
                    Description = $"Cannot delete role. {usersInRole.Count()} user(s) are assigned to this role." 
                });
            }

            return await _roleManager.DeleteAsync(role);
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return false;

            return await _roleManager.RoleExistsAsync(roleName);
        }

        #endregion

        #region User-Role Management

        public async Task<IEnumerable<ApplicationUser>> GetUsersInRoleAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return Enumerable.Empty<ApplicationUser>();

            return await _userManager.GetUsersInRoleAsync(roleName);
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Enumerable.Empty<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> AssignUserToRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            if (!await RoleExistsAsync(roleName))
                return IdentityResult.Failed(new IdentityError { Description = "Role not found." });

            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            return await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<bool> IsUserInRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<int> GetRoleUserCountAsync(string roleName)
        {
            var users = await GetUsersInRoleAsync(roleName);
            return users.Count();
        }

        #endregion

        #region ViewModel Methods

        public async Task<ViewModels.RolesIndexViewModel> GetRolesIndexViewModelAsync()
        {
            var rolesViewModels = await GetAllRoleViewModelsAsync();
            var userRoleViewModels = await GetUserRolesViewModelAsync();
            var availablePermissions = await GetAllAvailablePermissionsAsync();

            return new RolesIndexViewModel
            {
                Roles = rolesViewModels,
                UserRoles = userRoleViewModels,
                CreateRoleModel = new CreateRoleViewModel 
                { 
                    AvailablePermissions = availablePermissions 
                }
            };
        }

        public async Task<List<RoleViewModel>> GetAllRoleViewModelsAsync()
        {
            var roles = await GetAllRolesAsync();
            var rolesList = roles.ToList();

            var roleIds = rolesList.Select(r => r.Id).ToList();
            var allPermissions = await _context.Set<RolePermission>()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .GroupBy(rp => rp.RoleId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(rp => rp.Permission).Distinct().ToList());

            var allUsers = await _userManager.Users.ToListAsync();
            var userRoleLookup = new Dictionary<string, List<ApplicationUser>>();

            foreach (var role in rolesList)
            {
                var roleName = role?.Name ?? string.Empty;
                var usersInRole = new List<ApplicationUser>();

                foreach (var user in allUsers)
                {
                    if (await _userManager.IsInRoleAsync(user, roleName))
                    {
                        usersInRole.Add(user);
                    }
                }

                userRoleLookup[roleName] = usersInRole;
            }

            var roleViewModels = new List<RoleViewModel>();

            foreach (var role in rolesList)
            {
                var roleName = role?.Name ?? string.Empty;
                var usersInRole = userRoleLookup.GetValueOrDefault(roleName) ?? new List<ApplicationUser>();
                var persistedPermissions = allPermissions.GetValueOrDefault(role.Id) ?? new List<string>();
                var permissions = persistedPermissions.Count > 0 ? persistedPermissions : GetRolePermissions(roleName);

                roleViewModels.Add(new RoleViewModel
                {
                    Id = role?.Id ?? string.Empty,
                    RoleId = role?.Id ?? string.Empty,
                    Name = roleName,
                    RoleName = roleName,
                    Description = GetRoleDescription(roleName),
                    UserCount = usersInRole.Count,
                    Users = usersInRole.Take(5).ToList(),
                    Icon = GetRoleIcon(roleName),
                    ColorClass = GetRoleColorClass(roleName),
                    Permissions = permissions,
                    CreatedDate = DateTime.Now
                });
            }

            return roleViewModels;
        }

        public async Task<RoleViewModel?> GetRoleViewModelAsync(string roleId)
        {
            var role = await GetRoleByIdAsync(roleId);
            if (role == null)
                return null;

            var roleName = role.Name ?? string.Empty;
            var usersInRole = await GetUsersInRoleAsync(roleName);
            var persistedPermissions = await GetPersistedRolePermissionsAsync(role.Id);
            var permissions = persistedPermissions.Count > 0 ? persistedPermissions : GetRolePermissions(roleName);

            return new RoleViewModel
            {
                Id = role.Id,
                RoleId = role.Id,
                Name = roleName,
                RoleName = roleName,
                Description = GetRoleDescription(roleName),
                UserCount = usersInRole?.Count() ?? 0,
                Users = usersInRole?.Take(5).ToList() ?? new List<ApplicationUser>(),
                Icon = GetRoleIcon(roleName),
                ColorClass = GetRoleColorClass(roleName),
                Permissions = permissions,
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now
            };
        }

        public async Task<RoleDetailsViewModel?> GetRoleDetailsViewModelAsync(string roleId)
        {
            var role = await GetRoleByIdAsync(roleId);
            if (role == null)
                return null;

            var roleName = role.Name ?? string.Empty;
            var usersInRole = await GetUsersInRoleAsync(roleName);
            var persistedPermissions = await GetPersistedRolePermissionsAsync(role.Id);
            var permissions = persistedPermissions.Count > 0 ? persistedPermissions : GetRolePermissions(roleName);

            return new RoleDetailsViewModel
            {
                RoleId = role.Id,
                RoleName = roleName,
                Description = GetRoleDescription(roleName),
                UserCount = usersInRole?.Count() ?? 0,
                Users = usersInRole?.ToList() ?? new List<ApplicationUser>(),
                Permissions = permissions
            };
        }

        public async Task<List<UserRoleViewModel>> GetUserRolesViewModelAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "No Role";

                userRoles.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}",
                    RoleName = roleName,
                    LastUpdated = user.LastModified ?? DateTime.Now,
                    Status = GetUserStatus(user),
                    Avatar = user.ProfilePicture ?? "~/images/users/default-avatar.jpg"
                });
            }

            return userRoles.OrderByDescending(u => u.LastUpdated).ToList();
        }

        #endregion

        #region Helper Methods

        public string GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                "Security Officer" => "Handles platform safety and protocol reviews.",
                "Project Manager" => "Coordinates planning and team delivery timelines.",
                "Developer" => "Builds and maintains the platform core features.",
                "Support Lead" => "Oversees customer support and service quality.",
                "Administrator" => "Full system access and management capabilities.",
                _ => "Custom role with specific permissions."
            };
        }

        public string GetRoleIcon(string roleName)
        {
            return roleName switch
            {
                "Security Officer" => "ti-shield-lock",
                "Project Manager" => "ti-briefcase",
                "Developer" => "ti-code",
                "Support Lead" => "ti-headset",
                "Administrator" => "ti-crown",
                _ => "ti-user"
            };
        }

        public string GetRoleColorClass(string roleName)
        {
            return roleName switch
            {
                "Security Officer" => "primary",
                "Project Manager" => "success",
                "Developer" => "primary",
                "Support Lead" => "primary",
                "Administrator" => "danger",
                _ => "secondary"
            };
        }

        public List<string> GetRolePermissions(string roleName)
        {
            return roleName switch
            {
                "Security Officer" => new List<string> 
                { 
                    "Daily Risk Assessment", 
                    "Manage Security Logs", 
                    "Control Access Rights", 
                    "Emergency Protocols" 
                },
                "Project Manager" => new List<string> 
                { 
                    "Timeline Tracking", 
                    "Task Assignments", 
                    "Budget Control", 
                    "Stakeholder Reporting" 
                },
                "Developer" => new List<string> 
                { 
                    "Codebase Maintenance", 
                    "API Integration", 
                    "Unit Testing", 
                    "Feature Deployment" 
                },
                "Support Lead" => new List<string> 
                { 
                    "Respond to Tickets", 
                    "Live Chat Supervision", 
                    "FAQ Updates", 
                    "Support Metrics Review" 
                },
                "Administrator" => new List<string> 
                { 
                    "Full System Access", 
                    "User Management", 
                    "Role Management", 
                    "System Configuration" 
                },
                _ => new List<string> { "View Dashboard", "Edit Profile" }
            };
        }

        public async Task SetRolePermissionsAsync(string roleId, List<string> permissions)
        {
            if (string.IsNullOrEmpty(roleId))
                throw new ArgumentNullException(nameof(roleId));

            // Remove existing permissions
            var existing = _context.Set<RolePermission>().Where(rp => rp.RoleId == roleId);
            _context.RemoveRange(existing);
            await _context.SaveChangesAsync();

            // Insert new ones
            if (permissions == null || !permissions.Any())
                return;

            var rolePermissions = permissions.Select(p => new RolePermission { RoleId = roleId, Permission = p.Trim() }).ToList();
            await _context.Set<RolePermission>().AddRangeAsync(rolePermissions);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetPersistedRolePermissionsAsync(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
                return new List<string>();

            return await _context.Set<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<string>> GetAllAvailablePermissionsAsync()
        {
            // Return union of predefined default permissions + persisted values in the DB
            var defaultPermissions = new List<string>();
            var roleNames = new[] { "Security Officer", "Project Manager", "Developer", "Support Lead", "Administrator" };
            foreach (var rn in roleNames)
            {
                defaultPermissions.AddRange(GetRolePermissions(rn));
            }

            var persisted = await _context.Set<RolePermission>().Select(rp => rp.Permission).ToListAsync();
            var all = defaultPermissions.Concat(persisted).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().OrderBy(p => p).ToList();
            return all;
        }

        private string GetUserStatus(ApplicationUser user)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
                return "Suspended";
            
            return user.IsActive ? "Active" : "Inactive";
        }

        #endregion
    }
}
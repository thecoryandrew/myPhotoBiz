using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<List<UserListViewModel>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<UserListViewModel>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var permissions = await GetUserPermissionsAsync(user.Id);

                result.Add(new UserListViewModel
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
                    CreatedDate = DateTime.UtcNow
                });
            }

            return result;
        }

        public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            var permissionNames = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;

                var rolePermissions = await _context.Set<RolePermission>()
                    .Where(rp => rp.RoleId == role.Id && !string.IsNullOrEmpty(rp.Permission))
                    .Select(rp => rp.Permission)
                    .ToListAsync();

                foreach (var permName in rolePermissions)
                {
                    permissionNames.Add(permName);
                }
            }

            var uniquePermissions = await _context.Set<Permission>()
                .Where(p => permissionNames.Contains(p.Name))
                .Select(p => new PermissionViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CreatedDate = p.CreatedDate
                })
                .ToListAsync();

            var clientCount = await _context.ClientProfiles.CountAsync(c => c.UserId == userId);
            var photoShootCount = await _context.PhotoShoots.CountAsync(ps => ps.PhotographerId == userId);
            var invoiceCount = await _context.Invoices
                .CountAsync(i => i.ClientProfile != null && i.ClientProfile.UserId == userId);

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

        public async Task<IdentityResult> CreateUserAsync(CreateUserViewModel model)
        {
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
            if (!result.Succeeded) return result;

            var roleNames = await ResolveRoleNamesAsync(model.SelectedRoleIds);
            if (roleNames.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, roleNames);
            }

            return result;
        }

        public async Task<IdentityResult> UpdateUserAsync(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

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
            if (!result.Succeeded) return result;

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var roleNames = await ResolveRoleNamesAsync(model.SelectedRoleIds);
            if (roleNames.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, roleNames);
            }

            return result;
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            return await _userManager.DeleteAsync(user);
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return await _userManager.ResetPasswordAsync(user, token, newPassword);
        }

        public async Task<IdentityResult> AssignRolesToUserAsync(string userId, List<string> roleNames)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (roleNames?.Any() != true)
                return IdentityResult.Success;

            return await _userManager.AddToRolesAsync(user, roleNames);
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = new HashSet<string>();

            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;

                var rolePermissions = await _context.Set<RolePermission>()
                    .Where(rp => rp.RoleId == role.Id && !string.IsNullOrEmpty(rp.Permission))
                    .Select(rp => rp.Permission)
                    .ToListAsync();

                foreach (var permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
            }

            return permissions.ToList();
        }

        public async Task<IdentityResult> LockUserAsync(string userId, DateTimeOffset? lockoutEnd)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            return await _userManager.SetLockoutEndDateAsync(user, lockoutEnd ?? DateTimeOffset.MaxValue);
        }

        public async Task<IdentityResult> UnlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            return result;
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        private async Task<List<string>> ResolveRoleNamesAsync(List<string>? roleIds)
        {
            if (roleIds == null || roleIds.Count == 0) return new List<string>();

            var names = new List<string>(roleIds.Count);
            foreach (var roleId in roleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role?.Name != null) names.Add(role.Name);
            }
            return names;
        }
    }
}

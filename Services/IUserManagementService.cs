using Microsoft.AspNetCore.Identity;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public interface IUserManagementService
    {
        Task<List<UserListViewModel>> GetAllUsersAsync();
        Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId);
        Task<IdentityResult> CreateUserAsync(CreateUserViewModel model);
        Task<IdentityResult> UpdateUserAsync(EditUserViewModel model);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IdentityResult> ChangePasswordAsync(string userId, string newPassword);
        Task<IdentityResult> AssignRolesToUserAsync(string userId, List<string> roleNames);
        Task<List<string>> GetUserPermissionsAsync(string userId);
        Task<IdentityResult> LockUserAsync(string userId, DateTimeOffset? lockoutEnd);
        Task<IdentityResult> UnlockUserAsync(string userId);
        Task<List<IdentityRole>> GetAllRolesAsync();
    }
}

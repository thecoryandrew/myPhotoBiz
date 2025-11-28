using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public interface IPermissionService
    {
        Task<List<PermissionViewModel>> GetAllPermissionsAsync();
        Task<PermissionDetailsViewModel?> GetPermissionByIdAsync(int id);
        Task<Permission> CreatePermissionAsync(string name, string? description, List<string> roleIds);
        Task<Permission> UpdatePermissionAsync(int id, string name, string? description, List<string> roleIds);
        Task<bool> DeletePermissionAsync(int id);
        Task<List<Microsoft.AspNetCore.Identity.IdentityRole>> GetAllRolesAsync();
    }
}

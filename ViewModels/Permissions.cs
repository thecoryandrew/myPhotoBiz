using MyPhotoBiz.Models;

namespace MyPhotoBiz.ViewModels
{
    public class PermissionsIndexViewModel
    {
        public List<PermissionViewModel> Permissions { get; set; } = new List<PermissionViewModel>();
    }

    public class PermissionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public List<string> AssignedRoles { get; set; } = new List<string>();
        public int UserCount { get; set; }
    }

    public class CreatePermissionViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Permission name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string? Description { get; set; }

        public List<string> SelectedRoles { get; set; } = new List<string>();
        public List<Microsoft.AspNetCore.Identity.IdentityRole> AvailableRoles { get; set; } = new List<Microsoft.AspNetCore.Identity.IdentityRole>();
    }

    public class EditPermissionViewModel
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Permission name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string? Description { get; set; }

        public List<string> SelectedRoles { get; set; } = new List<string>();
        public List<Microsoft.AspNetCore.Identity.IdentityRole> AvailableRoles { get; set; } = new List<Microsoft.AspNetCore.Identity.IdentityRole>();
    }

    public class PermissionDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public List<RolePermissionInfo> AssignedRoles { get; set; } = new List<RolePermissionInfo>();
        public int TotalUsers { get; set; }
    }

    public class RolePermissionInfo
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }
}

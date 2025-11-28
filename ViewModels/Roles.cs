using System.ComponentModel.DataAnnotations;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.ViewModels
{
    public class RolesIndexViewModel
    {
        public List<RoleViewModel> Roles { get; set; } = new();
        public List<UserRoleViewModel> UserRoles { get; set; } = new();
        public CreateRoleViewModel CreateRoleModel { get; set; } = new();
    }

    public class RoleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public List<ApplicationUser> Users { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime LastUpdated { get; set; }
        public string Avatar { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string DisplayId => $"#USR{Math.Abs(UserId.GetHashCode()).ToString().PadLeft(5, '0').Substring(0, 5)}";
    }

    public class CreateRoleViewModel
    {
        [Required]
        [Display(Name = "Role Name")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        public string RoleName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<string> AvailablePermissions { get; set; } = new();
    }

    public class EditRoleViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role Name")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        public string RoleName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<string> AvailablePermissions { get; set; } = new();
    }

    public class RoleDetailsViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public List<ApplicationUser> Users { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class AssignRoleViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}
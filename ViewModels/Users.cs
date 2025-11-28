using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.ViewModels
{
    public class UsersIndexViewModel
    {
        public List<UserListViewModel> Users { get; set; } = new();
        public List<IdentityRole> AvailableRoles { get; set; } = new();
    }

    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string? ProfilePicture { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; } = true;
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string Status => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.Now ? "Locked" :
                                !EmailConfirmed ? "Pending" :
                                IsActive ? "Active" : "Inactive";
    }

    public class UserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public bool IsPhotographer { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastLogin => LastLoginDate;
        public List<string> Roles { get; set; } = new();
        public List<PermissionViewModel> Permissions { get; set; } = new();
        public int ClientCount { get; set; }
        public int PhotoShootCount { get; set; }
        public int InvoiceCount { get; set; }
        public int TotalClients => ClientCount;
        public int TotalPhotoShoots => PhotoShootCount;
        public int TotalInvoices => InvoiceCount;
        public string Status => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.Now ? "Locked" :
                                !EmailConfirmed ? "Pending" :
                                IsActive ? "Active" : "Inactive";
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool IsPhotographer { get; set; } = false;
        public List<string> SelectedRoleIds { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();
        public List<IdentityRole> AvailableRoles { get; set; } = new();
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? ProfilePicture { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPhotographer { get; set; }
        public List<string> SelectedRoleIds { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();
        public List<IdentityRole> AvailableRoles { get; set; } = new();
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserRoleAssignmentViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> CurrentRoles { get; set; } = new();
        public List<IdentityRole> AvailableRoles { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();
    }
}

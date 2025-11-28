// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public enum UserType
    {
        Client,
        Photographer,
        Admin
    }

    public class ApplicationUser : IdentityUser
    {
        // Additional properties beyond default IdentityUser
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastModified { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public UserType UserType { get; set; } = UserType.Client;

        // Compatibility property: older code referenced IsPhotographer bool
        // Map this to UserType to avoid refactoring all usages immediately.
        public bool IsPhotographer
        {
            get => UserType == UserType.Photographer;
            set => UserType = value ? UserType.Photographer : UserType.Client;
        }   
        // Full name property for convenience
        public string FullName => $"{FirstName} {LastName}";
    }
}

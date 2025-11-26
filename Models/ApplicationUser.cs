// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string? ProfilePicture { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsPhotographer { get; set; }

        public virtual string FullName => FirstName + " " + LastName;

        // Navigation properties
        public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
        public virtual ICollection<PhotoShoot> PhotoShoots { get; set; } = new List<PhotoShoot>();
    }
}
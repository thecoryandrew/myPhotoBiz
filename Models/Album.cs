
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsPublic { get; set; } = false;


        // Foreign key
        public int PhotoShootId { get; set; }
        public virtual PhotoShoot PhotoShoot { get; set; } = null!;


        // Foreign key - Client via ClientProfile
        public int ClientProfileId { get; set; }
        public virtual ClientProfile ClientProfile { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
        public virtual ICollection<Gallery> Galleries { get; set; } = new List<Gallery>();
    }
}


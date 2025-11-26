using MyPhotoBiz.Models;

namespace MyPhotoBiz.Models.ViewModels
{
    public class AlbumViewModel
    {
        public int Id { get; set; }
        public int PhotoShootId { get; set; }
        public string? PhotoShootTitle { get; set; }

        public string Name { get; set; } = string.Empty; // Album name
        public string Description { get; set; } = string.Empty;

        public ICollection<Photo>? Photos { get; set; }
    }
}

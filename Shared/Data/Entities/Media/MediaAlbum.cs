using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Media
{
    public class MediaAlbum
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<MediaFile> Files { get; set; } = new List<MediaFile>();
    }
}

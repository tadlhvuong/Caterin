namespace Shared.Data.Entities.Media
{
    public class BlogMedia
    {
        public int BlogId { get; set; }

        public long MediaFileId { get; set; }

        //public Blog Blog { get; set; } = null!;

        public MediaFile MediaFile { get; set; } = null!;
    }
}

namespace Shared.Data.Entities.Marketing
{
    public class Banner
    {
        public int Id { get; set; } 
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public int Position { get; set; }
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; }   
        public DateTime StartDate { get; set; }
        public DateTime EndTime { get; set; }
    }
}

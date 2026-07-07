using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Marketing
{
//    - Id
//- Title
//- ImageUrl
//- Link
//- Position
//- SortOrder
//- IsPublished
//- StartDate
//- EndDate
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Review
{
//    - Id
//- ProductId
//- UserId
//- Rating
//- CreatedAt
    public class ProductRating
    {
        public int Id { get; set; } 
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

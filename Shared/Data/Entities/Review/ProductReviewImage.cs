using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Review
{
//    - Id
//- ProductReviewId
//- ImageUrl
//- CreatedAt

    public class ProductReviewImage
    {
        public int Id { get; set; }
        public int productReviewId { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

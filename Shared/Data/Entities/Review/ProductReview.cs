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
//- Title
//- Content
//- IsApproved
//- IsVerifiedPurchase
//- HelpfulCount
//- CreatedAt
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public string Tilte { get; set; }
        public string Content { get; set; }
        public bool IsApproved { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public int HelpfulCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

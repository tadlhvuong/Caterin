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
//- ParentId
//- Content
//- IsApproved
//- CreatedAt
    public class ProductComment
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public int ParentId { get; set; }
        public string Content { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

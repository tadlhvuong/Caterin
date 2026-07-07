using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Order
{
//    - Id
//- OrderId
//- ReceiverName
//- Phone
//- Province
//- District
//- Ward
//- AddressLine
    public class OrderAddress
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ReceiverName { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string? District { get; set; }
        public string Ward { get; set; }
        public string AddressLine { get; set; }
    }
}

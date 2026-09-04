using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Enums
{
    public enum InventoryTransactionType
    {
        Import = 1,
        Export = 2,
        Adjustment = 3,
        Reserve = 4,
        Release = 5,
        Return = 6,
        TransferIn = 7,
        TransferOut = 8
    }
}

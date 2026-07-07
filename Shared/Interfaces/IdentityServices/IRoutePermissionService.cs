using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.IdentityServices
{
    public interface IRoutePermissionService
    {
        Task SyncAsync(CancellationToken cancellationToken = default);
    }
}

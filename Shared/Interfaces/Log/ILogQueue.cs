using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Log
{
    /// <summary>
    /// Lưu lịch sử hoạt động người dùng
    /// </summary>
    public interface ILogQueue
    {
        ValueTask EnqueueAsync( IActivityLogger logEvent);

        ValueTask<IActivityLogger> DequeueAsync( CancellationToken token);
    }


}

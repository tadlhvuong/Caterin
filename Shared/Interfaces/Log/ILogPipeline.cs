using Shared.Data.Entities.Identity.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Log
{
    public interface ILogPipeline
    {
        /// <summary>
        /// Luồng phân bổ sự kiện log
        /// </summary>
        ValueTask EnqueueAsync(LogBase log, CancellationToken cancellationToken = default);
    }
}

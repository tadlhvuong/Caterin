using Shared.Data.Entities.Identity.Log;
using Shared.Interfaces.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shared.Services.Log
{
    public sealed class LogPipeline : ILogPipeline
    {
        private readonly LogQueue _queue;

        public LogPipeline(LogQueue queue)
        {
            _queue = queue;
        }

        public ValueTask EnqueueAsync(LogBase log, CancellationToken cancellationToken = default)
        {
            return _queue.EnqueueAsync(log, cancellationToken);
        }
    }
}

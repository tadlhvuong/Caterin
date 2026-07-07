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
    public sealed class LogQueue
    {
        private readonly Channel<LogBase> _channel;

        public LogQueue()
        {
            _channel = Channel.CreateBounded<LogBase>(
                new BoundedChannelOptions(5000)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
        }

        public ValueTask EnqueueAsync(LogBase item, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(item, ct);

        public bool TryEnqueue(LogBase item)
            => _channel.Writer.TryWrite(item);

        public ChannelReader<LogBase> Reader => _channel.Reader;
    }
}

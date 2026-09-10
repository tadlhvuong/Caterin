using Shared.Data.Entities.Identity.Log;
using System.Threading.Channels;

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

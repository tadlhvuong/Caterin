using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Log;

namespace Shared.Services.Log
{
    public sealed class LogBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly LogQueue _queue;
        private readonly LogWorkerOptions _options;
        private readonly ILogger<LogBackgroundWorker> _logger;

        public LogBackgroundWorker(IServiceScopeFactory scopeFactory, IOptions<LogWorkerOptions> options,
            LogQueue queue, ILogger<LogBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _queue = queue;

            _logger = logger;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<LogBase>(_options.BatchSize);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    batch.Clear();

                    // Chờ log đầu tiên
                    var firstItem = await _queue.Reader.ReadAsync(stoppingToken);

                    batch.Add(firstItem);

                    // Gom thêm log trong khoảng FlushInterval
                    var deadline = DateTime.UtcNow.AddSeconds(_options.FlushIntervalSeconds);

                    while (batch.Count < _options.BatchSize)
                    {
                        var remaining = deadline - DateTime.UtcNow;

                        if (remaining <= TimeSpan.Zero)
                            break;

                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                            stoppingToken);

                        timeoutCts.CancelAfter(remaining);

                        try
                        {
                            var item = await _queue.Reader.ReadAsync(
                                timeoutCts.Token);

                            batch.Add(item);
                        }
                        catch (OperationCanceledException)
                            when (!stoppingToken.IsCancellationRequested)
                        {
                            // Hết thời gian gom batch
                            break;
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await FlushAsync(batch, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in log background worker.");
            }
            finally
            {
                await FlushAsync(batch, CancellationToken.None);
            }
        }

        private async Task FlushAsync(List<LogBase> batch, CancellationToken ct)
        {
            if (batch.Count == 0)
                return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.ChangeTracker.AutoDetectChangesEnabled = false;

                db.AddRange(batch);
                await db.SaveChangesAsync(ct);
                batch.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush logs");
            }
        }
    }
}

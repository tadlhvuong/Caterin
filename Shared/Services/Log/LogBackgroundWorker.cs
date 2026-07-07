using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Log
{
    public sealed class LogBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly LogQueue _queue;
        private readonly LogWorkerOptions _options;
        private readonly ILogger<LogBackgroundWorker> _logger;

        public LogBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            LogQueue queue,
            IOptions<LogWorkerOptions> options,
            ILogger<LogBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _queue = queue;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<LogBase>(_options.BatchSize);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var readTask = _queue.Reader.ReadAsync(stoppingToken).AsTask();
                        var delayTask = Task.Delay(
                            TimeSpan.FromSeconds(_options.FlushIntervalSeconds),
                            stoppingToken);

                        var completed = await Task.WhenAny(readTask, delayTask);

                        if (completed == readTask)
                        {
                            batch.Add(await readTask);

                            while (batch.Count < _options.BatchSize &&
                                   _queue.Reader.TryRead(out var item))
                            {
                                batch.Add(item);
                            }
                        }

                        if (batch.Count >= _options.BatchSize || completed == delayTask)
                        {
                            await FlushAsync(batch, stoppingToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in log worker loop");
                    }
                }
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

namespace Shared.Services.Log
{
    public sealed class LogWorkerOptions
    {
        public int BatchSize { get; set; } = 50;

        public int FlushIntervalSeconds { get; set; } = 3;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Log
{
    public sealed class LogWorkerOptions
    {
        public int BatchSize { get; set; } = 50;

        public int FlushIntervalSeconds { get; set; } = 3;
    }
}

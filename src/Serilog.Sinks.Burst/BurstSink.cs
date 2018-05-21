using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Serilog.Sinks.Burst
{
    public class BurstSink : ILogEventSink, IDisposable
    {
        private readonly Burst<LogEvent> _logEventsBurst;

        public BurstSink(Action<IEnumerable<LogEvent>> action, bool enableTimer = true,
            double interval = 5000, bool enableBatchLimit = true, int batchLimit = 100)
        {
            var builder = new BurstBuilder<LogEvent>();

            if (enableBatchLimit)
                builder.AddBatchLimit(batchLimit);

            if (enableTimer)
                builder.AddTimer(interval);

            _logEventsBurst = builder.AddCallback(action)
                .CreateBurst();
        }

        public void Emit(LogEvent logEvent) => 
            _logEventsBurst.Add(logEvent);

        public void Dispose() => 
            _logEventsBurst?.Dispose();
    }
}

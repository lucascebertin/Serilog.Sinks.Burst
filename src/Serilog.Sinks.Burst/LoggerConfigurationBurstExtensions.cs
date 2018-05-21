using Serilog.Configuration;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Serilog.Sinks.Burst
{
    public static class LoggerConfigurationBurstExtensions
    {
        public static LoggerConfiguration Burst(
            this LoggerSinkConfiguration loggerConfiguration, 
            Action<IEnumerable<LogEvent>> action, 
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool enableTimer = true,
            double interval = 5000, bool enableBatchLimit = true, int batchLimit = 100
        ) => 
            loggerConfiguration.Sink(
                new BurstSink(action, enableTimer, interval, enableBatchLimit, batchLimit), 
                restrictedToMinimumLevel
            );
    }
}

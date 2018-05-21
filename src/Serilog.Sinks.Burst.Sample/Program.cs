using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog.Sinks.Burst.Sample
{
    class Program
    {
        static async Task MainAsync(string[] args)
        {
            var burst = new BurstBuilder<string>()
               .AddBatchLimit(100)
               .AddCallback(async strs =>
                {
                    await Task.WhenAll(strs.Select(Console.Out.WriteLineAsync));
                    await Task.Delay(1000);
                })
               .CreateBurst();

            Enumerable.Range(0, 1000)
                .ToList()
                .ForEach(async i => await burst.AddAsync($"{i + 1}"));

            await Console.Out.WriteLineAsync("Done!");
            await Console.In.ReadLineAsync();
        }

        static void Sink()
        {
            var template = "[{0}] : [{1}] : {2} - {3}";

            Action<IEnumerable<LogEvent>> simpleConsoleOutput = lst =>
                lst.ToList().ForEach(x => Console.WriteLine(template, 
                    x.Timestamp, x.Level, x.MessageTemplate, x.Exception
                ));

            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Burst(simpleConsoleOutput, LogEventLevel.Debug)
                .CreateLogger();

            Enumerable.Range(0, 999)
                .ToList()
                .ForEach(x => logger.Debug($"{x}"));

            Console.WriteLine("Almost done... now, wait for 5 seconds, the time will be fired and messages will pop down here!");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Sink();

            MainAsync(args).Wait();
            //var burst = new BurstBuilder<string>()
            //    .AddBatchLimit(100)
            //    .AddCallback(async strs =>
            //    {
            //        strs.ToList().ForEach(Console.WriteLine);
            //        await Task.Delay(1000);
            //    }).AddCallback(strs =>
            //    {
            //        strs.ToList().ForEach(Console.WriteLine);
            //        Thread.Sleep(1000);
            //    })
            //    .CreateBurst();

            //Enumerable.Range(0, 1000)
            //    .ToList()
            //    .ForEach(i => burst.Add($"{i + 1}"));

            //Console.WriteLine("Done!");
            //Console.ReadLine();
        }
    }
}

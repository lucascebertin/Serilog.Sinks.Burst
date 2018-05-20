using System;
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
                   strs.ToList().ForEach(async x => await Console.Out.WriteLineAsync(x));
                   await Task.Delay(1000);
               })
               .CreateBurst();

            Enumerable.Range(0, 1000)
                .ToList()
                .ForEach(i => burst.Add($"{i + 1}"));

            await Console.Out.WriteLineAsync("Done!");
            await Console.In.ReadLineAsync();
        }

        static void Main(string[] args)
        {
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

using FakeItEasy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Serilog.Sinks.Burst.Tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;
        public UnitTest1(ITestOutputHelper output) =>
            _output = output;

        [Fact(DisplayName = "Callback should be invoked 2 times and passed all the amount of data when configured with batch limit")]
        public void Should_be_invoked_two_times_and_passed_all_the_amount_of_data_when_configured_with_batch_limit()
        {
            var amountOfData = 200;
            var batchLimit = 100;
            var numberOfInvocationsExpected = 2;

            var fn = A.Fake<Action<IEnumerable<string>>>();
            var expectedObjects = new List<string>(amountOfData);

            A.CallTo(fn).Invokes((IEnumerable<string> x) =>
                expectedObjects.AddRange(x));

            var burst = new BurstBuilder<string>()
                .AddBatchLimit(batchLimit)
                .AddCallback(fn)
                .CreateBurst();

            Enumerable.Range(0, amountOfData)
                .ToList()
                .ForEach(i => burst.Add($"{i}"));

            expectedObjects.Count.Should().Be(amountOfData);
            A.CallTo(fn).MustHaveHappened(numberOfInvocationsExpected, Times.Exactly);
        }

        [Fact(DisplayName = "Callback should invoked 20 times even with concurrency over it")]
        public void Callback_should_invoked_20_times_even_with_concurrency_over_it()
        {
            var counter = 0;

            async Task Fn(IEnumerable<string> x)
            {
                _output.WriteLine(string.Join(",", x));
                Interlocked.Increment(ref counter);
                await Task.FromResult(0);
            }

            var burst = new BurstBuilder<string>()
                .AddBatchLimit(10)
                .AddCallback(Fn)
                .AddTimer(500000)
                .CreateBurst();

            IEnumerable<Task> TaskGen() =>
                Enumerable.Range(0, 100)
                .ToList()
                .Select(x => burst.AddAsync($"{x}"));

            var tasks = new List<Task>();
            tasks.AddRange(TaskGen());
            tasks.AddRange(TaskGen());

            Task.WaitAll(tasks.ToArray());

            counter.Should().Be(20);        }

        [Fact(DisplayName = "Callback should be invoked after some time when not added the minimum amount of data and with a timer configured")]
        public void Callback_should_be_invoked_after_some_time_when_not_added_the_minimum_amount_of_data_and_with_a_timer_configured()
        {
            var interval = TimeSpan.FromSeconds(2).TotalMilliseconds;
            var sleepExpected = Convert.ToInt32(TimeSpan.FromSeconds(3).TotalMilliseconds);

            var fn = A.Fake<Action<IEnumerable<string>>>();

            var burst = new BurstBuilder<string>()
                .AddTimer(interval)
                .AddBatchLimit(100)
                .AddCallback(fn)
                .CreateBurst();

            for (var i = 0; i < 199; i++)
                burst.Add(Guid.NewGuid().ToString());

            Thread.Sleep(sleepExpected);

            A.CallTo(fn).MustHaveHappened(2, Times.Exactly);
        }


        [Fact(DisplayName = "Callback should be invoked asynchronously after some time when not added the minimum amount of data and with a timer configured")]
        public async Task Callback_should_be_invoked_asynchronously_after_some_time_when_not_added_the_minimum_amount_of_data_and_with_a_timer_configured()
        {
            var interval = TimeSpan.FromSeconds(2).TotalMilliseconds;
            var sleepExpected = Convert.ToInt32(TimeSpan.FromSeconds(3).TotalMilliseconds);

            var fn = A.Fake<Func<IEnumerable<string>, Task>>();

            var burst = new BurstBuilder<string>()
                .AddTimer(interval)
                .AddBatchLimit(100)
                .AddCallback(fn)
                .CreateBurst();

            for (var i = 0; i < 199; i++)
                await burst.AddAsync(Guid.NewGuid().ToString());

            await Task.Delay(sleepExpected);

            A.CallTo(fn).MustHaveHappened(2, Times.Exactly);
        }
    }
}

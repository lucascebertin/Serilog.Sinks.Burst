using FakeItEasy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.Burst.Tests
{
    public class UnitTest1
    {
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

        [Fact(DisplayName = "Callback should invoked 20 times even with concurrency over it", Skip = "To check later!")]
        public async Task Callback_should_invoked_20_times_even_with_concurrency_over_it()
        {
            var fn = A.Fake<Action<IEnumerable<string>>>();
            
            var burst = new BurstBuilder<string>()
                .AddBatchLimit(10)
                .AddCallback(fn)
                .AddTimer(500000)
                .CreateBurst();

#pragma warning disable IDE0039 // Use local function
            Action act = () =>
#pragma warning restore IDE0039 // Use local function
                Enumerable.Range(0, 100).ToList().ForEach(x => burst.Add($"{x}"));

            await Task.WhenAll(Task.Run(act), Task.Run(act));

            A.CallTo(fn).MustHaveHappened(20, Times.Exactly);
        }

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

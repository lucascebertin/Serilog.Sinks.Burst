using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Serilog.Sinks.Burst
{
    public class Burst<T> : IDisposable
    {
        private readonly int _limit;
        private readonly Action<IEnumerable<T>> _fn;
        private readonly Func<IEnumerable<T>, Task> _fnAsync;
        private readonly ConcurrentQueue<T> _concurrentQueue;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Timer _timer;
        private readonly bool _usingAsync;

        private int _interlockedCount;

        public Burst(bool enableTimer, double interval, bool enableLimit, int limit, Action<IEnumerable<T>> fn)
        {
            _limit = limit;
            _fn = fn;
            _concurrentQueue = new ConcurrentQueue<T>();

            if (!enableLimit && !enableTimer)
                throw new Exception($"At least one of burst initiators should be enabled. Arguments {nameof(enableLimit)} and {nameof(enableLimit)} are false");

            if (enableTimer)
            {
                _timer = new Timer(interval) { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
            }

            _usingAsync = false;
        }

        public Burst(bool enableTimer, double interval, bool enableLimit, int limit, Func<IEnumerable<T>, Task> fn)
        {
            _limit = limit;
            _fnAsync = fn;
            _concurrentQueue = new ConcurrentQueue<T>();

            if (!enableLimit && !enableTimer)
                throw new Exception($"At least one of burst initiators should be enabled. Arguments {nameof(enableLimit)} and {nameof(enableLimit)} are false");

            if (enableTimer)
            {
                _timer = new Timer(interval) { AutoReset = false };
                _timer.Elapsed += async (sender, args) =>
                    await _timer_ElapsedAsync();
            }

            _usingAsync = true;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e) =>
            TryToWorkOnItens(false);

        private async Task _timer_ElapsedAsync() =>
            await TryToWorkOnItensAsync(false);

        private (bool, IEnumerable<T>) FetchItems(bool considerLimits)
        {
            var canStartToWork = (_fn != null || _fnAsync != null)
                && (!considerLimits || _interlockedCount == _limit)
                && _interlockedCount > 0;

            if (!canStartToWork)
            {
                if (_timer != null && !_timer.Enabled)
                    _timer.Start();

                return (false, null);
            }

            var amoutToDequeue = considerLimits
                ? _limit
                : _interlockedCount;

            return TryRemove(amoutToDequeue);
        }

        private void TryToWorkOnItens(bool considerLimits = true)
        {
            _semaphoreSlim.Wait();

            try
            {
                var (dequeued, items) = FetchItems(considerLimits);

                if (!dequeued) return;

                //The timer only matters when the batch limit was not hitted on proper time.
                if (_timer != null && _timer.Enabled)
                    _timer.Stop();

                _fn(items);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task TryToWorkOnItensAsync(bool considerLimits = true)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var (dequeued, items) = FetchItems(considerLimits);

                if (dequeued)
                {
                    //The timer only matters when the batch limit was not hitted on proper time.
                    if (_timer != null && _timer.Enabled)
                        _timer.Stop();

                    await _fnAsync(items);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void AddBase(T item)
        {
            _concurrentQueue.Enqueue(item);
            Interlocked.Increment(ref _interlockedCount);
        }

        public void Add(T item)
        {
            if(_usingAsync)
                throw new InvalidOperationException("You can't invoke a sync method when the callback is async... use AddAsync instead or choose another constructor");

            AddBase(item);
            TryToWorkOnItens();
        }

        public async Task AddAsync(T item)
        {
            if (!_usingAsync)
                throw new InvalidOperationException("You can't invoke an async method when the callback is sync... use Add instead or choose another constructor");

            AddBase(item);
            await TryToWorkOnItensAsync();
        }

        public void CloseAndFlush()
        {
            if (_usingAsync)
                throw new InvalidOperationException("You can't invoke a sync method when the callback is async... use CloseAndFlushAsync instead or choose another constructor");

            TryToWorkOnItens(false);
        }

        public async Task CloseAndFlushAsync()
        {
            if (!_usingAsync)
                throw new InvalidOperationException("You can't invoke an async method when the callback is sync... use CloseAndFlush instead or choose another constructor");

            await TryToWorkOnItensAsync(false);
        }

        private (bool, T) TryRemove()
        {
            var dequeued = _concurrentQueue.TryDequeue(out var item);

            if (dequeued)
                Interlocked.Decrement(ref _interlockedCount);

            return (dequeued, item);
        }

        private (bool, IEnumerable<T>) TryRemove(int amount)
        {
            var items = new List<T>();
            var canKeepDequeuing = true;

            for (var i = 0; i < amount && canKeepDequeuing; i++)
            {
                var (dequeued, item) = TryRemove();
                canKeepDequeuing = dequeued;
                items.Add(item);
            }

            return (canKeepDequeuing, items);
        }

        public void Dispose()
        {
            _semaphoreSlim?.Dispose();
            _timer?.Dispose();
        }
    }
}

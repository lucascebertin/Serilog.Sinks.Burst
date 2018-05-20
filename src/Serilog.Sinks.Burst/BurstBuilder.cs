using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Burst
{
    public class BurstBuilder<T>
    {
        private bool _enableTimer;
        private double _interval;
        private bool _enableBatchLimit;
        private int _limit;
        private Action<IEnumerable<T>> _callback;
        private Func<IEnumerable<T>, Task> _callbackWithTask;

        public BurstBuilder<T> AddTimer(double interval)
        {
            _enableTimer = true;
            _interval = interval;
            return this;
        }

        public BurstBuilder<T> AddBatchLimit(int limit)
        {
            _enableBatchLimit = true;
            _limit = limit;
            return this;
        }

        public BurstBuilder<T> AddCallback(Action<IEnumerable<T>> callback)
        {
            _callback = callback;
            return this;
        }
        public BurstBuilder<T> AddCallback(Func<IEnumerable<T>, Task> callback)
        {
            _callbackWithTask = callback;
            return this;
        }

        public void ClearState()
        {
            _enableBatchLimit = false;
            _enableTimer = false;
            _limit = 0;
            _interval = 0;
            _callback = null;
            _callbackWithTask = null;
        }
        public Burst<T> CreateBurst()
        {
            var burst = _callback != null
                ? new Burst<T>(_enableTimer, _interval, _enableBatchLimit, _limit, _callback)
                : new Burst<T>(_enableTimer, _interval, _enableBatchLimit, _limit, _callbackWithTask);

            ClearState();

            return burst;
        }
    }
}

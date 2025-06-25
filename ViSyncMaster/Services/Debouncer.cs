using System;

using System.Timers;

namespace ViSyncMaster.Services
{
    public class Debouncer
    {
        private readonly Timer _timer;
        private readonly Action _action;

        public Debouncer(int milliseconds, Action action)
        {
            _action = action;
            _timer = new Timer(milliseconds) { AutoReset = false };
            _timer.Elapsed += (s, e) => _action();
        }

        public void Debounce()
        {
            _timer.Stop();
            _timer.Start();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.Services
{
    public class BufferedQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly Action<T> _processItemAction;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public BufferedQueue(Action<T> processItemAction)
        {
            _processItemAction = processItemAction ?? throw new ArgumentNullException(nameof(processItemAction));
            StartProcessing();
        }

        public async Task Enqueue(T item)
        {
            _queue.Enqueue(item);
            _signal.Set();
        }

        private void StartProcessing()
        {
            Task.Run(() =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _signal.WaitOne(); // Czeka, aż pojawi się element w kolejce
                    while (_queue.TryDequeue(out var item))
                    {
                        try
                        {
                            _processItemAction(item); // Tutaj wywołana zostaje przekazana metoda (np. AddOrUpdateInternal)
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error processing item: {item}");
                        }
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void StopProcessing()
        {
            _cancellationTokenSource.Cancel();
            _signal.Set();
        }
    }
}

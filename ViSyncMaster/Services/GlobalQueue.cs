using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public class GlobalQueue
    {
        private readonly ConcurrentQueue<IQueueTask> _queue = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly CancellationTokenSource _cts = new();
        private readonly int _maxRetries = 3;
        private readonly int _retryDelayMs = 1000;

        private static readonly Lazy<GlobalQueue> _instance = new(() => new GlobalQueue());
        public static GlobalQueue Instance => _instance.Value;

        private GlobalQueue()
        {
            StartProcessing();
        }

        public void Enqueue(IQueueTask task)
        {
            _queue.Enqueue(task);
            Log.Debug("Dodano zadanie do kolejki: {TaskType}. Aktualna długość kolejki: {QueueLength}", task.GetType().Name, _queue.Count);
            _signal.Set();
        }

        private void StartProcessing()
        {
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    _signal.WaitOne();
                    while (_queue.TryDequeue(out var task))
                    {
                        var sw = Stopwatch.StartNew();
                        bool success = false;

                        for (int attempt = 1; attempt <= _maxRetries && !success; attempt++)
                        {
                            try
                            {
                                Log.Debug("Przetwarzanie zadania {TaskType}, próba {Attempt}/{Max}", task.GetType().Name, attempt, _maxRetries);
                                task.Execute();
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "Błąd podczas przetwarzania zadania {TaskType}. Próba {Attempt}/{Max}", task.GetType().Name, attempt, _maxRetries);
                                if (attempt < _maxRetries)
                                {
                                    Log.Debug("Oczekiwanie {RetryDelay}ms przed kolejną próbą...", _retryDelayMs);
                                    await Task.Delay(_retryDelayMs);
                                }
                            }
                        }
                        sw.Stop();
                        Log.Information("Przetworzono zadanie {TaskType} w {ElapsedMs} ms. Pozostało w kolejce: {QueueLength}",
                            task.GetType().Name, sw.ElapsedMilliseconds, _queue.Count);
                    }
                }
            }, _cts.Token);
        }

        public void Stop()
        {
            _cts.Cancel();
            _signal.Set();
            Log.Information("Zatrzymano przetwarzanie kolejki.");
        }
    }
}

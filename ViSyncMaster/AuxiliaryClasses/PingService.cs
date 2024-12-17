using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ViSyncMaster.AuxiliaryClasses
{
    public class PingService
    {
        private readonly string _ipAddress;
        private readonly TimeSpan _pingInterval;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _pingTask;

        public event EventHandler<bool> PingCompleted;

        public PingService(string ipAddress, TimeSpan? pingInterval = null)
        {
            _ipAddress = ipAddress;
            _pingInterval = pingInterval ?? TimeSpan.FromMinutes(1); // Domyślnie ping co minutę
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            // Uruchom pingowanie w osobnym wątku
            _pingTask = Task.Run(async () => await PerformPingAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            // Anuluj pętlę i upewnij się, że task zakończył się poprawnie
            _cancellationTokenSource.Cancel();
            _pingTask?.Wait();
        }

        private async Task PerformPingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(_ipAddress, 3000); // Timeout: 3 sekundy
                    bool isSuccess = reply.Status == IPStatus.Success;

                    // Wywołanie zdarzenia z wynikiem
                    PingCompleted?.Invoke(this, isSuccess);

                    if (isSuccess)
                    {
                        Console.WriteLine($"Ping do {_ipAddress} powiódł się.");
                    }
                    else
                    {
                        Console.WriteLine($"Ping do {_ipAddress} nie powiódł się.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas pingowania {_ipAddress}: {ex.Message}");
                    PingCompleted?.Invoke(this, false);
                }

                // Odczekaj przed kolejną próbą
                try
                {
                    await Task.Delay(_pingInterval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Zatrzymano pingowanie
                    break;
                }
            }

            Console.WriteLine("Pingowanie zatrzymane.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services.Test
{
    public class Rs232TestSimulator
    {
        private readonly Rs232DataProcessor _processor;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public Rs232TestSimulator(Rs232DataProcessor processor)
        {
            _processor = processor;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                int cycle = 1;

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Console.WriteLine($"🔄 Start cyklu testowego {cycle}");

                    // Producing = true
                    var startData = new Rs232Data
                    {
                        Producing = "true",
                        ProductName = $"TEST-PROD-{cycle}",
                        OperatorId = $"OP{cycle:00}",
                    };
                    _processor.Process(startData);

                    await Task.Delay(500); // Delay po rozpoczęciu produkcji

                    // 3 sztuki testowe co 200 ms
                    for (int i = 1; i <= 3; i++)
                    {
                        var passed = i % 2 == 1;

                        var testData = new Rs232Data
                        {
                            Producing = "true", // nadal trwa produkcja
                            ProductName = $"TEST-PROD-{cycle}",
                            OperatorId = $"OP{cycle:00}",
                            TestingPassed = passed ? "true" : null,
                            TestingFailed = passed ? null : "true",
                        };

                        _processor.Process(testData);
                        await Task.Delay(500);
                    }

                    // Producing = false -> zakończenie cyklu
                    var endData = new Rs232Data
                    {
                        Producing = "false",
                        ProductName = $"TEST-PROD-{cycle}",
                        OperatorId = $"OP{cycle:00}",
                    };
                    _processor.Process(endData);

                    cycle++;

                    await Task.Delay(TimeSpan.FromMinutes(2)); // kolejny cykl za 2 minuty
                }
            });
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}

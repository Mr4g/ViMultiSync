using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Entitys;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;

public class MessageQueue
{
    // In-memory queues
    private readonly Queue<MachineStatus> _machineStatusQueue = new();
    private readonly Queue<MachineStatus> _testingResultQueue = new();
    private readonly Queue<ProductionEfficiency> _productionEfficiencyQueue = new();
    private readonly Queue<FirstPartModel> _firstPartDataQueue = new();

    // ID tracking for TestingResult deduplication
    private readonly HashSet<long> _testingResultSeen = new();

    // Repositories and sender
    private readonly GenericRepository<MachineStatus> _repositoryMachineStatusQueue;
    private readonly GenericRepository<MachineStatus> _repositoryTestingResultQueue;
    private readonly GenericRepository<ProductionEfficiency> _repositoryProductionEfficiency;
    private readonly GenericRepository<FirstPartModel> _repositoryFirstPartQueue;
    private readonly MessageSender _messageSender;            // ← dodajemy

    private DateTime _lastSendTime = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(2);
    private bool _isSending = false;
    private bool _isInitialized = false;

    public MessageQueue(
        GenericRepository<MachineStatus> repositoryMachineStatusQueue,
        GenericRepository<MachineStatus> repositoryTestingResultQueue,
        GenericRepository<ProductionEfficiency> repositoryProductionEfficiency,
        GenericRepository<FirstPartModel> repositoryFirstPartQueue,
        MessageSender messageSender)                        // ← wstrzykujemy
    {
        _repositoryMachineStatusQueue = repositoryMachineStatusQueue;
        _repositoryTestingResultQueue = repositoryTestingResultQueue;
        _repositoryProductionEfficiency = repositoryProductionEfficiency;
        _repositoryFirstPartQueue = repositoryFirstPartQueue;
        _messageSender = messageSender;

        // Asynchroniczna inicjalizacja
        _ = EnsureInitializedAsync();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;
        await LoadInProgressMessagesAsync();
        await LoadPendingMessagesAsync();
        _isInitialized = true;
        await SendAllMessages();
    }

    private async Task LoadPendingMessagesAsync()
    {
        try
        {
            // MachineStatus Pending
            var m1 = await _repositoryMachineStatusQueue.GetByStatusAsync();
            EnqueueAndMark(m1, _repositoryMachineStatusQueue, _machineStatusQueue);

            // TestingResult Pending - dedupe by ID
            var m2 = await _repositoryTestingResultQueue.GetByStatusAsync();
            foreach (var m in m2)
            {
                if (_testingResultSeen.Add(m.Id))
                    EnqueueAndMark(new[] { m }, _repositoryTestingResultQueue, _testingResultQueue);
                else
                    Log.Debug("Skipping duplicate TestingResult ID={Id}", m.Id);
            }

            // ProductionEfficiency Pending
            var m3 = await _repositoryProductionEfficiency.GetByStatusAsync();
            EnqueueAndMark(m3, _repositoryProductionEfficiency, _productionEfficiencyQueue);

            // FirstPartData Pending
            var m4 = await _repositoryFirstPartQueue.GetByStatusAsync();
            EnqueueAndMark(m4, _repositoryFirstPartQueue, _firstPartDataQueue);

            Log.Information($"[LoadPending] Queues: MS={_machineStatusQueue.Count}, TR={_testingResultQueue.Count}, PE={_productionEfficiencyQueue.Count}, FP={_firstPartDataQueue.Count}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Błąd ładowania Pending");
        }
    }

    private async Task LoadInProgressMessagesAsync()
    {
        try
        {
            var i1 = await _repositoryMachineStatusQueue.GetByStatusInProgressAsync();
            EnqueueOnly(i1, _machineStatusQueue);

            var i2 = await _repositoryTestingResultQueue.GetByStatusInProgressAsync();
            EnqueueOnly(i2, _testingResultQueue);

            var i3 = await _repositoryProductionEfficiency.GetByStatusInProgressAsync();
            EnqueueOnly(i3, _productionEfficiencyQueue);

            var i4 = await _repositoryFirstPartQueue.GetByStatusInProgressAsync();
            EnqueueOnly(i4, _firstPartDataQueue);

            Log.Information($"[LoadInProgress] Queues: MS={_machineStatusQueue.Count}, TR={_testingResultQueue.Count}, PE={_productionEfficiencyQueue.Count}, FP={_firstPartDataQueue.Count}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Błąd ładowania InProgress");
        }
    }

    private static void EnqueueAndMark<T>(IEnumerable<T> msgs, GenericRepository<T> repo, Queue<T> q)
        where T : class, IEntity, new()
    {
        foreach (var m in msgs)
        {
            m.SendStatus = "InProgress";
            repo.AddOrUpdate(m).Wait();
            q.Enqueue(m);
        }
    }

    private static void EnqueueOnly<T>(IEnumerable<T> msgs, Queue<T> q)
    {
        foreach (var m in msgs) q.Enqueue(m);
    }

    /// <summary>
    /// Wysyła wszystkie dostępne wiadomości z każdej kolejki.
    /// </summary>
    public async Task SendAllMessages()
    {
        await EnsureInitializedAsync();
        // throttle
        if (DateTime.Now - _lastSendTime < _minInterval) return;
        _lastSendTime = DateTime.Now;

        // jeśli wszystkie kolejki są puste, próbuj jeszcze raz z bazy
        if (_machineStatusQueue.Count == 0
         && _testingResultQueue.Count == 0
         && _productionEfficiencyQueue.Count == 0
         && _firstPartDataQueue.Count == 0)
        {
            await LoadPendingMessagesAsync();
        }

        if (!_isSending)
        {
            _ = SendMessagesFromQueue(_machineStatusQueue, _repositoryMachineStatusQueue);
            _ = SendMessagesFromQueue(_testingResultQueue, _repositoryTestingResultQueue);
            _ = SendMessagesFromQueue(_productionEfficiencyQueue, _repositoryProductionEfficiency);
            _ = SendMessagesFromQueue(_firstPartDataQueue, _repositoryFirstPartQueue);
        }
    }

    private async Task SendMessagesFromQueue<T>(Queue<T> queue, GenericRepository<T> repo)
        where T : class, IEntity, new()
    {
        _isSending = true;
        try
        {
            while (queue.Count > 0)
            {
                var msg = queue.Peek();
                bool ok;
                try
                {
                    ok = await _messageSender.SendMessageAsync(msg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Błąd wysyłania: {ex.Message}");
                    ok = false;
                }

                if (ok)
                {
                    repo.Delete(msg.Id);
                    queue.Dequeue();
                }
                else
                {
                    msg.SendStatus = "Pending";
                    await repo.AddOrUpdate(msg);
                    await Task.Delay(500);
                }
            }
        }
        finally
        {
            _isSending = false;
        }
    }

    // ** Zwróć uwagę: każda metoda Enqueue teraz wywołuje SendAllMessages() **
    public void EnqueueMachineStatusMessage(MachineStatus m)
    {
        _machineStatusQueue.Enqueue(m);
        _ = SendAllMessages();
    }

    public void EnqueueTestingResultMessage(MachineStatus m)
    {
        _testingResultQueue.Enqueue(m);
        _ = SendAllMessages();
    }

    public void EnqueueProductionEfficiencyMessage(ProductionEfficiency m)
    {
        _productionEfficiencyQueue.Enqueue(m);
        _ = SendAllMessages();
    }

    public void EnqueueFirstPartDataMessage(FirstPartModel m)
    {
        _firstPartDataQueue.Enqueue(m);
        _ = SendAllMessages();
    }
}

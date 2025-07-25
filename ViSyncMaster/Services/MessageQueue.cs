﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
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
    private readonly Queue<HourlyPlanMessage> _hourlyPlanQueue = new();


    // ID tracking for TestingResult deduplication
    private readonly HashSet<long> _testingResultSeen = new();

    // Repositories and sender
    private readonly GenericRepository<MachineStatus> _repositoryMachineStatusQueue;
    private readonly GenericRepository<MachineStatus> _repositoryTestingResultQueue;
    private readonly GenericRepository<ProductionEfficiency> _repositoryProductionEfficiency;
    private readonly GenericRepository<FirstPartModel> _repositoryFirstPartQueue;
    private readonly GenericRepository<HourlyPlanMessage> _repositoryHourlyPlan;
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
        GenericRepository<HourlyPlanMessage> repositoryHourlyPlan,
        MessageSender messageSender)                        // ← wstrzykujemy
    {
        _repositoryMachineStatusQueue = repositoryMachineStatusQueue;
        _repositoryTestingResultQueue = repositoryTestingResultQueue;
        _repositoryProductionEfficiency = repositoryProductionEfficiency;
        _repositoryFirstPartQueue = repositoryFirstPartQueue;
        _repositoryHourlyPlan = repositoryHourlyPlan;
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
            await EnqueueAndMark(m1, _repositoryMachineStatusQueue, _machineStatusQueue);

            // TestingResult Pending - dedupe by ID
            var m2 = await _repositoryTestingResultQueue.GetByStatusAsync();
            foreach (var m in m2)
            {
                if (_testingResultSeen.Add(m.Id))
                    await EnqueueAndMark(new[] { m }, _repositoryTestingResultQueue, _testingResultQueue);
                else
                    Log.Debug("Skipping duplicate TestingResult ID={Id}", m.Id);
            }

            // ProductionEfficiency Pending
            var m3 = await _repositoryProductionEfficiency.GetByStatusAsync();
            await EnqueueAndMark(m3, _repositoryProductionEfficiency, _productionEfficiencyQueue);
            // FirstPartData Pending
            var m4 = await _repositoryFirstPartQueue.GetByStatusAsync();
            await EnqueueAndMark(m4, _repositoryFirstPartQueue, _firstPartDataQueue);

            // HourlyPlan Pending
            var m5 = await _repositoryHourlyPlan.GetByStatusAsync();
            await EnqueueAndMark(m5, _repositoryHourlyPlan, _hourlyPlanQueue);

            Log.Information($"[LoadPending] Queues: MS={_machineStatusQueue.Count}, TR={_testingResultQueue.Count}, PE={_productionEfficiencyQueue.Count}, FP={_firstPartDataQueue.Count}, HP={_hourlyPlanQueue.Count}");
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

            var i5 = await _repositoryHourlyPlan.GetByStatusInProgressAsync();
            EnqueueOnly(i5, _hourlyPlanQueue);

            Log.Information($"[LoadInProgress] Queues: MS={_machineStatusQueue.Count}, TR={_testingResultQueue.Count}, PE={_productionEfficiencyQueue.Count}, FP={_firstPartDataQueue.Count}, HP={_hourlyPlanQueue.Count}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Błąd ładowania InProgress");
        }
    }

    private static async Task EnqueueAndMark<T>(IEnumerable<T> msgs, GenericRepository<T> repo, Queue<T> q)
        where T : class, IEntity, new()
    {
        foreach (var m in msgs)
        {
            m.SendStatus = "InProgress";
            await repo.AddOrUpdate(m);

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
         && _firstPartDataQueue.Count == 0
         && _hourlyPlanQueue.Count == 0)
        {
            await LoadPendingMessagesAsync();
        }

        if (_isSending) return;

        _isSending = true;
        try
        {
            var tasks = new[]
            {
                SendMessagesFromQueue(_machineStatusQueue, _repositoryMachineStatusQueue),
                SendMessagesFromQueue(_testingResultQueue, _repositoryTestingResultQueue),
                SendMessagesFromQueue(_productionEfficiencyQueue, _repositoryProductionEfficiency),
                SendMessagesFromQueue(_firstPartDataQueue, _repositoryFirstPartQueue),
                SendMessagesFromQueue(_hourlyPlanQueue, _repositoryHourlyPlan)
            };

            await Task.WhenAll(tasks);
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task SendMessagesFromQueue<T>(Queue<T> queue, GenericRepository<T> repo)
        where T : class, IEntity, new()
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
                try
                {
                    await repo.DeleteAsync(msg.Id);
                    queue.Dequeue();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete message ID={Id}", msg.Id);
                    msg.SendStatus = "Pending";
                    await repo.AddOrUpdate(msg);
                    await Task.Delay(500);
                }
            }
            else
            {
                msg.SendStatus = "Pending";
                await repo.AddOrUpdate(msg);
                await Task.Delay(500);
            }
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
    public void EnqueueHourlyPlanMessage(HourlyPlanMessage m)
    {
        _hourlyPlanQueue.Enqueue(m);
        _ = SendAllMessages();
    }
}

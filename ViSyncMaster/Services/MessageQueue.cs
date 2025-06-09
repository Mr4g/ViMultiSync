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
    private readonly Queue<MachineStatus> _machineStatusQueue = new Queue<MachineStatus>();
    private readonly Queue<MachineStatus> _testingResultQueue = new Queue<MachineStatus>();
    private readonly Queue<ProductionEfficiency> _productionEfficiencyQueue = new Queue<ProductionEfficiency>();
    private readonly Queue<FirstPartModel> _firstPartDataQueue = new Queue<FirstPartModel>();
    private readonly GenericRepository<MachineStatus> _repositoryMachineStatusQueue;
    private readonly GenericRepository<MachineStatus> _repositoryTestingResultQueue;
    private readonly GenericRepository<ProductionEfficiency> _repositoryProductionEfficiency;
    private readonly GenericRepository<FirstPartModel> _reposiotryFirstPartDataQueue;
    private DateTime _lastSendTime = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(2);
    private bool _isSending = false;


    public MessageQueue(GenericRepository<MachineStatus> repositoryMachineStatusQueue, 
                        GenericRepository<MachineStatus> repositoryTestingResultQueue,
                        GenericRepository<ProductionEfficiency> repositoryProductionEfficiency, 
                        GenericRepository<FirstPartModel> repositoryFirstPartDataQueue)
    {
        _repositoryMachineStatusQueue = repositoryMachineStatusQueue;
        _repositoryTestingResultQueue = repositoryTestingResultQueue;
        _repositoryProductionEfficiency = repositoryProductionEfficiency;
        _reposiotryFirstPartDataQueue = repositoryFirstPartDataQueue;
        Initialize();
    }

    private void Initialize()
    {
        LoadMessagesFromDatabase();
        LoadMessagesFromBackupDatabase();
    }

    private async Task LoadMessagesFromBackupDatabase()
    {
        try
        {
            var pendingMachineStatusMessages = await _repositoryMachineStatusQueue.GetByStatusInProgressAsync();
            foreach (var message in pendingMachineStatusMessages)
            {
                message.SendStatus = "InProgress";
                await _repositoryMachineStatusQueue.AddOrUpdate(message);
                _machineStatusQueue.Enqueue(message);
            }

            var pendingTestingResultMessages = await _repositoryTestingResultQueue.GetByStatusInProgressAsync();
            foreach (var message in pendingTestingResultMessages)
            {
                message.SendStatus = "InProgress";
                await _repositoryTestingResultQueue.AddOrUpdate(message);
                _testingResultQueue.Enqueue(message);
            }

            var pendingProductionEfficiency = await _repositoryProductionEfficiency.GetByStatusInProgressAsync();
            foreach (var message in pendingProductionEfficiency)
            {
                message.SendStatus = "InProgress";
                await _repositoryProductionEfficiency.AddOrUpdate(message);
                _productionEfficiencyQueue.Enqueue(message);
            }

            var pendingFirstPartData = await _reposiotryFirstPartDataQueue.GetByStatusInProgressAsync();
            foreach (var message in pendingFirstPartData)
            {
                message.SendStatus = "InProgress";
                await _reposiotryFirstPartDataQueue.AddOrUpdate(message);
                _firstPartDataQueue.Enqueue(message);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd {ex} ładowania danych z bazy danych");
        }
    }

    public async Task LoadMessagesFromDatabase()
    {
        try
        {
            var pendingMachineStatusMessages = await _repositoryMachineStatusQueue.GetByStatusAsync();
            foreach (var message in pendingMachineStatusMessages)
            {
            message.SendStatus = "InProgress";
            await _repositoryMachineStatusQueue.AddOrUpdate(message);
            _machineStatusQueue.Enqueue(message);
            }

            var pendingTestingResultMessages = await _repositoryTestingResultQueue.GetByStatusAsync();
            foreach (var message in pendingTestingResultMessages)
            {
            message.SendStatus = "InProgress";
            await _repositoryTestingResultQueue.AddOrUpdate(message);
            _testingResultQueue.Enqueue(message);
            }

            var pendingProductionEfficiency = await _repositoryProductionEfficiency.GetByStatusAsync();
            Debug.WriteLine($"Załadowano {pendingProductionEfficiency.Count} wiadomości z bazy danych.");
            foreach (var message in pendingProductionEfficiency)
            {
                message.SendStatus = "InProgress";
                await _repositoryProductionEfficiency.AddOrUpdate(message);
                _productionEfficiencyQueue.Enqueue(message);
            }

            var pendingFirstPartData = await _reposiotryFirstPartDataQueue.GetByStatusAsync();
            foreach (var message in pendingFirstPartData)
            {
                message.SendStatus = "InProgress";
                await _reposiotryFirstPartDataQueue.AddOrUpdate(message);
                _firstPartDataQueue.Enqueue(message);
            }
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Błąd {ex} ładowania danych z bazy danych");
        }
    }

    // Dodawanie wiadomości do kolejki i zapisywanie w bazie danych
    public void EnqueueMachineStatusMessage(MachineStatus machineStatus)
    {
        _machineStatusQueue.Enqueue(machineStatus); // Dodaj do kolejki
    }

    public void EnqueueTestingResultMessage(MachineStatus machineStatus)
    {
        _testingResultQueue.Enqueue(machineStatus); // Dodaj do kolejki
    }

    public void EnqueueProductionEfficiencyMessage(ProductionEfficiency productionEfficiency)
    {
        _productionEfficiencyQueue.Enqueue(productionEfficiency); // Dodaj do kolejki
    }

    // Pobieranie wiadomości z kolejki
    public MachineStatus? DequeueMachineStatusMessage()
    {
        return _machineStatusQueue.Count > 0 ? _machineStatusQueue.Dequeue() : null;
    }

    public MachineStatus? DequeueTestingResultMessage()
    {
        return _testingResultQueue.Count > 0 ? _testingResultQueue.Dequeue() : null;
    }


    // Sprawdzanie, czy kolejka jest pusta
    public bool IsMachineStatusQueueEmpty()
    {
        return _machineStatusQueue.Count == 0;
    }

    public bool IsTestingResultQueueEmpty()
    {
        return _testingResultQueue.Count == 0;
    }


    public async Task SendAllMessages(MessageSender messageSender)
    {
        if (DateTime.Now - _lastSendTime < _minInterval)
        {
            Debug.WriteLine("Za wcześnie na ponowną wysyłkę, pomijam.");
            return;
        }

        _lastSendTime = DateTime.Now;

        if (_machineStatusQueue.Count == 0 && _testingResultQueue.Count == 0)
        {
            await LoadMessagesFromDatabase();
        }

        if (!_isSending) // Jeśli już nie wysyłamy, rozpocznij proces wysyłania
        {
            _ = SendMessagesFromQueue(_machineStatusQueue, _repositoryMachineStatusQueue, messageSender);
            _ = SendMessagesFromQueue(_testingResultQueue, _repositoryTestingResultQueue, messageSender);
            _ = SendMessagesFromQueue(_productionEfficiencyQueue, _repositoryProductionEfficiency, messageSender);
            _ = SendMessagesFromQueue(_firstPartDataQueue, _reposiotryFirstPartDataQueue, messageSender);   
        }
    }


    private async Task SendMessagesFromQueue<T>(Queue<T> queue, GenericRepository<T> repository, MessageSender messageSender)
        where T : IEntity, new()
    {
        if (_isSending) return;
        _isSending = true;
        try
        {
            while (queue.Count > 0)
            {
                T message = queue.Peek();
                try
                {
                    bool success = await messageSender.SendMessageAsync(message);
                    if (success)
                    {
                        repository.Delete(message.Id);
                        queue.Dequeue();
                    }
                    else
                    {
                        message.SendStatus = "Pending";
                        await repository.AddOrUpdate(message);
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd wysyłania: {ex.Message}");
                    message.SendStatus = "Pending";
                    await repository.AddOrUpdate(message);
                    await Task.Delay(500);
                }
            }
        }
        finally
        {
            _isSending = false;
        }
    }



}


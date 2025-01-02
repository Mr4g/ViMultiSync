using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;

public class MessageQueue
{
    private readonly Queue<MachineStatus> _queue = new Queue<MachineStatus>();
    private readonly GenericRepository<MachineStatus> _repositoryQueue;

    public MessageQueue(GenericRepository<MachineStatus> repository)
    {
        _repositoryQueue = repository;
        Initialize();
    }

    private void Initialize()
    {
        //LoadMessagesFromDatabase();
    }

    public async Task LoadMessagesFromDatabase()
    {
        var pendingMessages = await _repositoryQueue.GetAllAsync();
        foreach (var message in pendingMessages)
        {
            _queue.Enqueue(message); // Dodaj do kolejki w pamięci
        }
    }

    // Dodawanie wiadomości do kolejki i zapisywanie w bazie danych
    public void EnqueueMessage(MachineStatus machineStatus)
    {
        _queue.Enqueue(machineStatus); // Dodaj do kolejki
    }

    // Pobieranie wiadomości z kolejki
    public MachineStatus? DequeueMessage()
    {
        return _queue.Count > 0 ? _queue.Dequeue() : null;
    }

    // Sprawdzanie, czy kolejka jest pusta
    public bool IsQueueEmpty()
    {
        return _queue.Count == 0;
    }

    public async Task SendAllMessages(MessageSender messageSender)
    {
        // Jeśli kolejka jest pusta, załaduj wiadomości z bazy danych
        if (_queue.Count == 0)
        {
            LoadMessagesFromDatabase();
        }

        while (_queue.Count > 0)
        {
            var machineStatus = _queue.Dequeue(); // Pobierz obiekt MachineStatus z kolejki

            try
            {
                // Wyślij wiadomość
                var isSent = await messageSender.SendMessageAsync(machineStatus);

                if (isSent)
                {
                    // Usuń wiadomość z bazy danych po pomyślnym wysłaniu
                    _repositoryQueue.Delete(machineStatus.Id);
                }
                else
                {
                    // Dodaj wiadomość z powrotem na koniec kolejki, jeśli wysyłanie się nie powiodło
                    EnqueueMessage(machineStatus);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas wysyłania wiadomości: {ex.Message}");
                EnqueueMessage(machineStatus);
            }
        }
    }
}


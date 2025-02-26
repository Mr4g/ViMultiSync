using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;

namespace ViSyncMaster.Services
{
    public class MachineStatusService
    {
        private readonly GenericRepository<MachineStatus> _repositoryMachineStatus;
        private readonly GenericRepository<MachineStatus> _repositoryMachineStatusQueue;
        private readonly GenericRepository<MachineStatus> _repositoryTestingResultQueue;
        private readonly GenericRepository<MachineStatus> _repositoryTestingResult;
        private readonly GenericRepository<ProductionEfficiency> _repositoryProductionEfficiency;
        private readonly MessageSender _messageSender;
        private readonly MessageQueue _messageQueue;
        private readonly SQLiteDatabase _database;
        private long _lastStatusId = 0;
        private CancellationTokenSource _cacheUpdatedCts = new();
        private Task _lastTask = Task.CompletedTask;
        public event Action? TableResultTestUpdate;


        // Konstruktor przyjmuje repozytoria generyczne dla obu tabel
        public MachineStatusService(GenericRepository<MachineStatus> repositoryMachineStatus,
                                    GenericRepository<MachineStatus> repositoryMachineStatusQueue,
                                    GenericRepository<MachineStatus> repositoryTestingResultQueue,
                                    GenericRepository<MachineStatus> repositoryTestingResult,
                                    GenericRepository<ProductionEfficiency> repositoryProductionEfficiency,
                                    MessageSender messageSender,
                                    MessageQueue messageQueue,
                                    SQLiteDatabase database)
        {
            _repositoryMachineStatus = repositoryMachineStatus;
            _repositoryMachineStatusQueue = repositoryMachineStatusQueue;
            _repositoryTestingResultQueue = repositoryTestingResultQueue;
            _repositoryTestingResult = repositoryTestingResult;
            _repositoryProductionEfficiency = repositoryProductionEfficiency;
            _messageSender = messageSender;
            _messageQueue = messageQueue;
            _database = database;

            Debug.WriteLine("Rejestracja subskrypcji CacheUpdated");
            _repositoryMachineStatusQueue.CacheUpdated += HandleCacheUpdated;
            _repositoryTestingResultQueue.CacheUpdated += HandleCacheUpdated;
            _repositoryProductionEfficiency.CacheUpdated += HandleCacheUpdated;

        }

        // Rozpoczęcie nowego statusu
        public async Task<MachineStatus> StartStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;
            machineStatus.SendTime = epochMilliseconds;

            // Asynchronicznie dodaj status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            return machineStatus;
        }
        public async Task<MachineStatus> ReportPartQuality(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.StartTime = DateTime.Now;
            machineStatus.SendTime = epochMilliseconds;
            machineStatus.Id = uniqueId;
            await _repositoryTestingResultQueue.AddOrUpdate(machineStatus);
            await _repositoryTestingResult.AddOrUpdate(machineStatus);
            if (machineStatus.Name == "S7.TestingPassed")
            {
                Debug.WriteLine($"PASS TO DATABASE S7.TestingPassed ReportPartQuality {machineStatus.Id}");
            }
            //await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            return machineStatus;
        }

        public async Task<ProductionEfficiency> RaportProdcuctionEfficiency(ProductionEfficiency productionEfficiency)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            productionEfficiency.SendTime = epochMilliseconds;
            productionEfficiency.Id = uniqueId;
            await _repositoryProductionEfficiency.AddOrUpdate(productionEfficiency);
            return productionEfficiency;
        }
 

        // Wysyłanie wiadomości które już posiadają ID/Start/End etc..
        public async Task<MachineStatus> ReSendMessageToSplunk(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            machineStatus.SendTime = epochMilliseconds;
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            return machineStatus;
        }

        public async Task<MachineStatus> UpdateStatus(MachineStatus machineStatus)
        {
            // Asynchronicznie dodaj status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            return machineStatus;
        }

        // Zakończenie statusu
        public MachineStatus EndStatus(MachineStatus machineStatus)
        {
            machineStatus.EndTime = DateTime.Now;  // Zaktualizowanie czasu zakończenia 
            // Zapisz zaktualizowany status w repozytorium MachineStatus
            _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            _repositoryMachineStatus.AddOrUpdate(machineStatus);      
            return machineStatus;
        }


        private async void HandleCacheUpdated()
        {
            Debug.WriteLine("Wywołano HandleCacheUpdated");

            // Anulowanie poprzedniego zadania
            _cacheUpdatedCts.Cancel();
            _cacheUpdatedCts.Dispose(); // Zwolnienie zasobów
            _cacheUpdatedCts = new CancellationTokenSource();

            // Nie czekamy na anulowane zadania (zapobiega blokowaniu)
            if (!_lastTask.IsCanceled && !_lastTask.IsFaulted)
            {
                await _lastTask;
            }
            try
            {
                // Przechowujemy nowe zadanie
                _lastTask = Task.Delay(3000, _cacheUpdatedCts.Token);
                await _lastTask; // Poczekaj 2 sek, jeśli nie będzie kolejnych wywołań

                TableResultTestUpdate?.Invoke();
                // Jeśli nie było anulowania, wysyłamy wiadomości
                await _messageQueue.SendAllMessages(_messageSender);
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Anulowano wysyłanie – przyszły nowe dane");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas wysyłania wiadomości: {ex}");
            }
        }

        // Dodatkowa metoda do zarządzania kolejką statusów
        public void QueueMachineStatus(MachineStatus machineStatus)
        {
            // Zapisz status do kolejki
            _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);

            // Można dodać logikę do przetwarzania wiadomości w kolejce
            _messageQueue.SendAllMessages(_messageSender);
        }
    }
}

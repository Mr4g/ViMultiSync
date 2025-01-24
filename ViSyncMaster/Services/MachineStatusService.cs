using System;
using System.Collections.Generic;
using System.Text.Json;
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
        private readonly MessageSender _messageSender;
        private readonly MessageQueue _messageQueue;
        private readonly SQLiteDatabase _database;

        // Konstruktor przyjmuje repozytoria generyczne dla obu tabel
        public MachineStatusService(GenericRepository<MachineStatus> repositoryMachineStatus,
                                    GenericRepository<MachineStatus> repositoryMachineStatusQueue,
                                    MessageSender messageSender,
                                    MessageQueue messageQueue,
                                    SQLiteDatabase database)
        {
            _repositoryMachineStatus = repositoryMachineStatus;
            _repositoryMachineStatusQueue = repositoryMachineStatusQueue;
            _messageSender = messageSender;
            _messageQueue = messageQueue;
            _database = database;

            _repositoryMachineStatusQueue.CacheUpdated += HandleCacheUpdated;
        }

        // Rozpoczęcie nowego statusu
        public async Task<MachineStatus> StartStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;

            // Asynchronicznie dodaj status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            return machineStatus;
        }
        public async Task<MachineStatus> ReportPartQuality(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;
            machineStatus.EndTime = DateTime.Now;

            // Asynchronicznie dodaj status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            //await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            return machineStatus;
        }

        // Wysyłanie wiadomości które już posiadają ID/Start/End etc..
        public async Task<MachineStatus> ReSendMessageToSplunk(MachineStatus machineStatus)
        {
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
            // Po zaktualizowaniu bazy danych, spróbuj wysłać wiadomości z kolejki
            await _messageQueue.SendAllMessages(_messageSender);
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

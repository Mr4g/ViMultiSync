using System;
using System.Collections.Generic;
using System.Text.Json;
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
        }

        // Rozpoczęcie nowego statusu
        public MachineStatus StartStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;

            // Zapisz status w repozytorium MachineStatus
            _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            _repositoryMachineStatus.AddOrUpdate(machineStatus);

            var message = JsonSerializer.Serialize(machineStatus);

            // Spróbuj wysłać wiadomości z kolejki
            _messageQueue.SendAllMessages(_messageSender);

            return machineStatus;
        }

        // Zakończenie statusu
        public MachineStatus EndStatus(MachineStatus machineStatus)
        {
            machineStatus.EndTime = DateTime.Now;  // Zaktualizowanie czasu zakończenia 

            // Zapisz zaktualizowany status w repozytorium MachineStatus
            _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            _repositoryMachineStatus.AddOrUpdate(machineStatus);      

            // Spróbuj wysłać wiadomości z kolejki
            _messageQueue.SendAllMessages(_messageSender);

            return machineStatus;
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

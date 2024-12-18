using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Repositories;

namespace ViSyncMaster.Services
{
    public class MachineStatusService
    {
        private readonly MachineStatusRepository _repository;
        private readonly MessageSender _messageSender;
        private readonly MessageQueue _messageQueue;

        public MachineStatusService(MachineStatusRepository repository, MessageSender messageSender, MessageQueue messageQueue)
        {
            _repository = repository;
            _messageSender = messageSender;
            _messageQueue = messageQueue;
        }

        // Rozpoczęcie nowego statusu
        public MachineStatus StartStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;

            _repository.SaveStatus(machineStatus); // Zapisz do repozytorium

            var message = JsonSerializer.Serialize(machineStatus);
            _messageQueue.EnqueueMessage(message); // Dodaj wiadomość do kolejki

            // Spróbuj wysłać wszystkie wiadomości w kolejce
            _messageQueue.SendAllMessages(_messageSender);

            return machineStatus;
        }

        public MachineStatus EndStatus(MachineStatus machineStatus)
        {
            machineStatus.EndTime = DateTime.Now;  // Zaktualizowanie czasu zakończenia 

            // Zapisz zaktualizowany status w repozytorium
            _repository.UpdateStatus(machineStatus);

            // Serializowanie zaktualizowanego statusu do JSON
            var message = JsonSerializer.Serialize(machineStatus);

            // Spróbuj wysłać wszystkie wiadomości w kolejce
            _messageQueue.SendAllMessages(_messageSender);

            return machineStatus;
        }
    }
}

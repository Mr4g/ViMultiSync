using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class MessageQueue
    {
        private readonly Queue<string> _queue = new Queue<string>();

        // Dodawanie wiadomości do kolejki
        public void EnqueueMessage(string message)
        {
            _queue.Enqueue(message);
        }

        // Pobieranie wiadomości z kolejki
        public string DequeueMessage()
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }

        // Sprawdzanie, czy kolejka jest pusta
        public bool IsQueueEmpty()
        {
            return _queue.Count == 0;
        }

        // Wysyłanie wszystkich wiadomości z kolejki (np. po odzyskaniu połączenia)
        public void SendAllMessages(MessageSender messageSender)
        {
            while (_queue.Count > 0)
            {
                var message = _queue.Dequeue();
                messageSender.SendMessage(message); // Wyślij wiadomość
            }
        }
    }
}

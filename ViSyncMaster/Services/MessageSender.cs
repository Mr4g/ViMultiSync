using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public class MessageSender
    {
        private readonly bool _isConnected;

        public MessageSender(bool isConnected)
        {
            _isConnected = isConnected;
        }

        // Wysyłanie wiadomości, jeśli połączenie jest aktywne
        public void SendMessage(string message)
        {
            if (_isConnected)
            {
                // Logika wysyłania wiadomości
                Console.WriteLine($"Wiadomość wysłana: {message}");
            }
            else
            {
                // Logika dodawania wiadomości do kolejki, jeśli brak połączenia
                Console.WriteLine("Brak połączenia, wiadomość dodana do kolejki.");
            }
        }
    }
}

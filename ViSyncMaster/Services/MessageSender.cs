using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class MessageSender
    {
        private readonly bool _isConnected;

        public MessageSender(bool isConnected)
        {
            _isConnected = isConnected;
        }


        public async Task<bool> SendMessageAsync(MachineStatus message)
        {
            try
            {
                // Symulacja wysyłania wiadomości
                await Task.Delay(500);
                Console.WriteLine($"Wiadomość wysłana: {message.Id}");
                return true; // Sukces
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd wysyłania wiadomości: {ex.Message}");
                return false; // Wysyłanie nie powiodło się
            }
        }
    }
}

using MQTTnet;
using MQTTnet.Client;

using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class MqttMessageSender
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;

        public MqttMessageSender(string brokerHost, int brokerPort, string clientId, string token)
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerHost, brokerPort)
                .WithClientId(clientId)  // clientId = login (token)
                .WithCredentials(token, token)  // login i hasło = token
                .WithCleanSession()  // Opcjonalnie: ustawienie CleanSession
                .Build();

            // Obsługa zdarzeń MQTT
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        }

        // Metoda do wysyłania wiadomości
        public async Task SendMqttMessageAsync(string topic, MachineStatus messageObject)
        {
            try
            {
                // Serializuj obiekt do JSON
                string jsonMessage = JsonSerializer.Serialize(messageObject);

                // Upewnij się, że jesteś połączony z brokerem
                if (!_mqttClient.IsConnected)
                {
                    Console.WriteLine("Łączenie z brokerem MQTT...");
                    await _mqttClient.ConnectAsync(_mqttOptions);
                }

                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonMessage) // Wysyłanie JSON jako payload
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                var result = await _mqttClient.PublishAsync(mqttMessage);

                if (result.ReasonCode == MqttClientPublishReasonCode.Success)
                    Console.WriteLine($"Wiadomość JSON wysłana: {jsonMessage} na temat: {topic}");
                else
                    Console.WriteLine($"Nie udało się wysłać wiadomości. Kod: {result.ReasonCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd MQTT: {ex.Message}");
                throw;
            }
        }

        private Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            Console.WriteLine("Połączono z brokerem MQTT.");
            return Task.CompletedTask;
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            Console.WriteLine("Rozłączono z brokerem MQTT. Próba ponownego połączenia...");
            _ = ReconnectAsync();
            return Task.CompletedTask;
        }

        private async Task ReconnectAsync()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // Odczekaj 5 sekund przed próbą ponownego połączenia
                await _mqttClient.ConnectAsync(_mqttOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nie udało się ponownie połączyć z brokerem: {ex.Message}");
            }
        }
    }
}

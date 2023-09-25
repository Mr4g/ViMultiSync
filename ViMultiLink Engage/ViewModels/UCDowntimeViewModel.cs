
using System.Net.Http;
using System.Text;
using System;
using CommunityToolkit.Mvvm.ComponentModel;


namespace ViMultiSync.ViewModels
{
    public partial class UCDowntimeViewModel : ObservableObject
    {
        [ObservableProperty] private string downtime01 = "Uszkodzony wąż";

        [ObservableProperty] private string downtime02;

        string splunkUrl = "https://w6243w05.viessmann.net:8088/services/collector/event";

        // Token HEC do autoryzacji
        string hecToken = "efb19351-ab0c-4c6d-b506-0a3c440e208a";

        string eventName = "w16_test";


        public async void ActionDowntime()
        {
            // Inicjalizacja handlera HttpClientHandler z obsługą certyfikatów
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (HttpClient client = new HttpClient(handler))
            {
                // Nagłówki żądania HTTP
                client.DefaultRequestHeaders.Add("Authorization", $"Splunk {hecToken}");

                // Dane do wysłania
                string logData = "Witaj, Splunk! To jest przykład logowania.";
                
                // Tworzenie treści HTTP
                StringContent content = new StringContent(logData, Encoding.UTF8, "application/json");

                // Dodanie nagłówka X-Splunk-Index do określenia indeksu
                content.Headers.Add("X-Splunk-Index", "machinedata_test");

                content.Headers.Add("X-Splunk-Event-Name", eventName);
                Console.WriteLine($"Wysyłanie");
                // Wysłanie danych do Splunka za pomocą POST
                HttpResponseMessage response = await client.PostAsync(splunkUrl, content);

                // Sprawdzenie odpowiedzi
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Dane zostały pomyślnie wysłane do indeksu w Splunku.");
                }
                else
                {
                    Console.WriteLine($"Wystąpił błąd: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }
    }
}

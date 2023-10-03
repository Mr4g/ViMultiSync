
using System.Net.Http;
using System.Text;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Splunk.Logging;


using System.Diagnostics;

namespace ViMultiSync.ViewModels
{
    public partial class UCDowntimeViewModel : ObservableObject
    {
        [ObservableProperty] private string downtime01 = "Uszkodzony wąż";

        [ObservableProperty] private string downtime02 = "Send to splunk";


        public async void ActionDowntime()
        {
 
        }

        public async void ActionDowntime2()
        {
            string splunkUrl = "https://10.10.212.27:8088/services/collector/event"; // Zaktualizuj na właściwy URL Splunka
            string hecToken = "a5a26423-9874-4383-8f31-436c3c86a4ee"; // Zaktualizuj na prawidłowy token HEC
            string splunkIndex = "ipc_test"; // Zaktualizuj na właściwy indeks
            string splunkSource = "W16FunctionTesterODUIV1673000313"; // Zaktualizuj na właściwe źródło
            string eventName = "connection"; // Zaktualizuj na nazwę zdarzenia

            // Inicjalizacja handlera HttpClientHandler z obsługą certyfikatów
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (HttpClient client = new HttpClient(handler))
            {
                // Nagłówki żądania HTTP
                client.DefaultRequestHeaders.Add("Authorization", $"Splunk {hecToken}");

                // Dane do wysłania
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logData = $@"{{
                    ""event"": {{
                                    ""name"": ""S1.Test_Polaczenia"",
                                    ""value"": ""true_bo_jak_inaczej""                                    
                               }},
                    ""index"": ""machinedata_w16"",
                    ""source"": ""W16iPC_Test""
                
            }}";

                // Tworzenie treści HTTP
                StringContent content = new StringContent(logData, Encoding.UTF8, "application/json");

                // Dodanie nagłówka X-Splunk-Index do określenia indeksu
                content.Headers.Add("X-Splunk-Index", splunkIndex);

                // Dodanie nagłówka X-Splunk-Source do określenia źródła
                content.Headers.Add("X-Splunk-Source", splunkSource);

                // Dodanie nagłówka X-Splunk-Event-Name do określenia nazwy zdarzenia
                content.Headers.Add("X-Splunk-Event-Name", eventName);

                Console.WriteLine($"Wysyłanie");
                // Wysłanie danych do Splunka za pomocą POST
                HttpResponseMessage response = await client.PostAsync(splunkUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Odpowiedź z Splunk: {responseContent}");

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
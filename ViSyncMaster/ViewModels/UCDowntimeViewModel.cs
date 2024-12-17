
using System.Net.Http;
using System.Text;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using ViSyncMaster.AuxiliaryClasses;
using System.Xml.Linq;
using Avalonia.Metadata;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ViSyncMaster.ViewModels
{
    public partial class UCDowntimeViewModel : ObservableObject
    {
        [ObservableProperty] private string downtime01 = "Uszkodzony wąż";

        [ObservableProperty] private string downtime02 = "Brak zasilania";

        [ObservableProperty] private string downtime03 = "Awaria silnika";

        [RelayCommand]
        public async void ActionDowntime2(CommandParameters parameters)
        {
            string value = null;
            string name = null;
            string status = null;
            string sapCode = null;

            if (parameters != null)
            {
                value = parameters.Value;
                name = parameters.Name;
                status = parameters.Status;
                sapCode = parameters.SapCode;
            }

            string splunkUrl = "https://10.13.21.22:8088/services/collector/event"; // Zaktualizuj na właściwy URL Splunka
            string hecToken = "6112a68b-c8f6-4946-a7dd-7d48a710bc83"; // Zaktualizuj na prawidłowy token HEC
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
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");

                // Get the offset from current time in UTC time
                DateTimeOffset dto = new DateTimeOffset(DateTime.UtcNow);
                // Get the unix timestamp in seconds
                string unixTime = dto.ToUnixTimeSeconds().ToString();
                // Get the unix timestamp in seconds, and add the milliseconds
                string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();


                string logData = $@"{{
                    ""time"": {unixTimeMilliSeconds},
                    ""sourcetype"": ""iPC"",                  
                    ""event"": {{ ""name"": ""{name}"", ""value"": ""{value}"", ""Status"": ""{status}"", ""SapCode"": ""{sapCode}"", ""time"":""{unixTimeMilliSeconds}""}},                   
                    ""index"": ""machinedata_w16"",
                    ""source"": ""W16iPC_Test"",
                    ""fields"": {{""device"": ""iPC"", ""user"": ""Sambor""}}}}";


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

        public async void ActionDowntime3()
        {
            try
            {
                // Konfiguracja poświadczeń
                string username = "vi-ipad-local-w16";
                string password = "SplunkW16";

                // Konfiguracja hosta i endpointu Splunka
                string splunkHost = "w6228w05.viessmann.net:8000";
                string splunkEndpoint = "/rest/search/jobs/export";

                // Konfiguracja zapytania SPL (Search Processing Language)
                string splQuery = "index=machinedata_w16 source=W16iPC_Test earliest=-15m | stats values(name) as event_names";

                // Konstruowanie URL
                string splunkUrl = $"https://splunk05w05.viessmann.net:8000/en-GB/app/VI_W16/search?q=search%20index%3Dmachinedata_w16%20source%3DW16iPC_Test%20name%3DS1.Downtime&display.page.search.mode=smart&dispatch.sample_ratio=1&earliest=0&latest=&display.page.search.tab=statistics&display.general.type=events&sid=1696833344.2159888&display.events.fields=%5B%22host%22%2C%22source%22%2C%22sourcetype%22%2C%22scope%22%2C%22user%22%2C%22device%22%5D";

                // Inicjalizacja klienta HttpClient
                using (HttpClient client = new HttpClient())
                {
                    // Utworzenie zapytania HTTP GET
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, splunkUrl);

                    // Wykonaj zapytanie HTTP GET
                    HttpResponseMessage response = await client.SendAsync(request);

                    // Sprawdź, czy odpowiedź jest udana (status kod 200 OK)
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Odpowiedź z Splunk:");
                        Console.WriteLine(responseBody);
                    }
                    else
                    {
                        Console.WriteLine($"Błąd: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił wyjątek: {ex.Message}");
            }
        }
        }
    }


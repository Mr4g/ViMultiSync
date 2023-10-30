using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ViMultiSync.DataModel;
using ViMultiSync.Entitys;

namespace ViMultiSync.Repositories
{
    public class GenericSplunkLogger<T>
    {

        string splunkUrl = "https://10.13.21.22:8088/services/collector/event"; // Zaktualizuj na właściwy URL Splunka
        string hecToken = "6112a68b-c8f6-4946-a7dd-7d48a710bc83"; // Zaktualizuj na prawidłowy token HEC
        string splunkIndex = "ipc_test"; // Zaktualizuj na właściwy indeks
        string splunkSource = "W16FunctionTesterODUIV1673000313"; // Zaktualizuj na właściwe źródło
        string eventName = "connection"; // Zaktualizuj na nazwę zdarzenia

        public GenericSplunkLogger()
        {
        }

        public async Task LogAsync(T data)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Splunk {hecToken}");

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");

                // Get the offset from the current time in UTC time
                DateTimeOffset dto = new DateTimeOffset(DateTime.UtcNow);
                // Get the unix timestamp in seconds
                string unixTime = dto.ToUnixTimeSeconds().ToString();
                // Get the unix timestamp in seconds and add the milliseconds
                string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();

                string jsonPayload = ConvertDataToJson(data, unixTimeMilliSeconds);

                // Tworzenie treści HTTP
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

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

        private string ConvertDataToJson(T data, string unixTimeMilliSeconds)
        {
            var type = typeof(T);
            var fields = new Dictionary<string, string>();

            foreach (var property in type.GetProperties())
            {
                var propertyName = property.Name;
                var propertyValue = property.GetValue(data);

                if (propertyValue != null)
                {
                    fields[propertyName] = propertyValue.ToString();
                }
            }

            // Dodaj standardowe pola JSON
            fields.Add("time", unixTimeMilliSeconds);
            fields.Add("sourcetype", "iPC");
            fields.Add("index", "machinedata_w16");
            fields.Add("time_epoch", unixTimeMilliSeconds);

            // Buduj JSON na podstawie słownika pól
            var jsonPayload = JsonConvert.SerializeObject(new
            {
                time = fields["time"],
                sourcetype = fields["sourcetype"],
                index = fields["index"],
                @event = fields,
                fields = new { }
            }, Formatting.None); // Ustawiamy "None", aby usunąć formatowanie

            // Usuń białe znaki i znaki nowej linii
            var jsonPayloadWithoutWhitespace = Regex.Replace(jsonPayload, @"\s+", "");

            return jsonPayloadWithoutWhitespace;
        }
    }
}

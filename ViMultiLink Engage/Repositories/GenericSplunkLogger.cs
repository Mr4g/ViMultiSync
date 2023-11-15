using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Avalonia;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;


namespace ViMultiSync.Repositories
{
    public class GenericSplunkLogger<T>
    {

        string splunkUrl = "https://10.10.212.27:8088/services/collector/event"; // Zaktualizuj na właściwy URL Splunka
        string hecToken = "a5a26423-9874-4383-8f31-436c3c86a4ee"; // Zaktualizuj na prawidłowy token HEC
        string splunkIndex = "ipc_test"; // Zaktualizuj na właściwy indeks
        string splunkSource = "W16FunctionTesterODUIV1673000313"; // Zaktualizuj na właściwe źródło
        string eventName = "connection"; // Zaktualizuj na nazwę zdarzenia

        public bool DataIsSendingToSplunk { get; private set; }

        public GenericSplunkLogger()
        {
        }

        public async Task LogAsync(T data)
        {
            DataIsSendingToSplunk = true;
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    HttpResponseMessage response = null;

                    client.DefaultRequestHeaders.Add("Authorization", $"Splunk {hecToken}");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");

                    DateTimeOffset dto = new DateTimeOffset(DateTime.UtcNow);
                    string unixTime = dto.ToUnixTimeSeconds().ToString();
                    string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();

                    string jsonPayload = ConvertDataToJson(data, unixTimeMilliSeconds);

                    StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    content.Headers.Add("X-Splunk-Index", splunkIndex);
                    content.Headers.Add("X-Splunk-Source", splunkSource);
                    content.Headers.Add("X-Splunk-Event-Name", eventName);

                    Console.WriteLine($"Wysyłanie");

                    // Wysłanie danych do Splunka za pomocą POST
                    response = await client.PostAsync(splunkUrl, content);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Odpowiedź z Splunk: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Dane zostały pomyślnie wysłane do indeksu w Splunku.");
                    }
                    else
                    {
                        Console.WriteLine($"Wystąpił błąd: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }

                DataIsSendingToSplunk = false;
            }
            catch (HttpRequestException e)
            {
                var box = MessageBoxManager.GetMessageBoxCustom(
                    new MessageBoxCustomParams
                    {
                        ButtonDefinitions = new List<ButtonDefinition>
                        {
                            new ButtonDefinition { Name = "OK", },
                            new ButtonDefinition { Name = "Cancel", }
                        },
                        ContentTitle = "BŁĄD WYSYŁANIA DANYCH",
                        ContentMessage = $"Brak połączenia z siecią OT proszę wezwać Utrzymanie Ruchu przez telefon... ",
                        Icon = Icon.Error,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        MaxWidth = 500,
                        MaxHeight = 800,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        ShowInCenter = true,
                        Topmost = true, 
                    });

                var result = await box.ShowAsync();
                Console.WriteLine($"Wystąpił błąd HTTP: {e.Message}");
            }
            catch (Exception e)
            {
                // Obsługa innych ogólnych błędów
                Console.WriteLine($"Wystąpił błąd: {e.Message}");
            }
        }

        private string ConvertDataToJson(T data, string unixTimeMilliSeconds)
        {
            var type = typeof(T);
            var eventFields = new Dictionary<string, string>();
            var generalFields = new Dictionary<string, string>();
            var otherFields = new Dictionary<string, string>();

            foreach (var property in type.GetProperties())
            {
                var propertyName = property.Name.ToLower();
                var propertyValue = property.GetValue(data);

                if (propertyValue != null && propertyName != "time")
                {
                    if (propertyName == "name" || propertyName == "value" || propertyName == "status" || propertyName == "time_epoch" || propertyName == "reason")
                    {
                        eventFields[propertyName] = propertyValue.ToString();
                    }
                    else if (propertyName == "source")
                    {
                        generalFields[propertyName] = propertyValue.ToString();
                    }
                    else 
                    {
                        otherFields[propertyName] = propertyValue.ToString();
                    }
                }
            }

            // Add general fields
            generalFields.Add("time", unixTimeMilliSeconds);
            generalFields.Add("sourcetype", "iPC");
            generalFields.Add("index", "machinedata_w16");
            eventFields.Add("time_epoch", unixTimeMilliSeconds);

            // Build json file
            var jsonPayload = JsonConvert.SerializeObject(new
            {
                time = generalFields["time"],
                sourcetype = generalFields["sourcetype"],
                index = generalFields["index"],
                source = generalFields["source"],
                @event = eventFields,
                @fields = otherFields
            }, Formatting.None); // delete format

            // Clear line and blank symbols
            var jsonPayloadWithoutWhitespace = Regex.Replace(jsonPayload, @"\s+", "");

            return jsonPayloadWithoutWhitespace;
        }

    }
}

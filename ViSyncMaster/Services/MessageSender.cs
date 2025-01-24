using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ViSyncMaster.AuxiliaryClasses;
using ViSyncMaster.DataModel;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;

namespace ViSyncMaster.Services
{
 public class MessageSender
    {
        private readonly string splunkUrl;
        private readonly string hecToken;
        private readonly MainWindowViewModel _viewModel;
        private SharedDataService _sharedDataService;
        private AppConfigData appConfig;

        public MessageSender(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            _sharedDataService = new SharedDataService();
            this.appConfig = _sharedDataService.AppConfig;
            this.splunkUrl = _sharedDataService.AppConfig.UrlSplunk ?? string.Empty;
            this.hecToken = _sharedDataService.AppConfig.TokenSplunk;
        }

        public async Task<bool> SendMessageAsync<T>(T data)
        {
            _viewModel.DataIsSendingToSplunk = true;

            try
            {
                using (var client = CreateHttpClientWithKeepAlive())
                {
                    HttpResponseMessage response = null;

                    client.DefaultRequestHeaders.Add("Authorization", $"Splunk {hecToken}");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.fff");

                    DateTimeOffset dto = new DateTimeOffset(DateTime.UtcNow);
                    string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();

                    string jsonPayload = ConvertDataToJson(data, unixTimeMilliSeconds, currentTime);

                    StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    Console.WriteLine($"Wysyłanie");

                    // Ustawienie timeout dla HttpClient
                    var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Ustaw timeout na 10 sekund

                    try
                    {
                        // Wysłanie danych do Splunka za pomocą POST z uwzględnieniem timeout
                        response = await client.PostAsync(splunkUrl, content, timeoutTokenSource.Token);
                    }
                    catch (TaskCanceledException ex) when (ex.CancellationToken == timeoutTokenSource.Token)
                    {
                        // Timeout wystąpił
                        Console.WriteLine("Timeout - dane nie zostały wysłane");
                        await ErrorHandler.ShowErrorNetwork($"Timeout - dane nie zostały wysłane.\r\nProblemy z siecią, proszę powiadomić UR przez telefon.");
                        return false; // Przerwij wysyłanie i zwróć false
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Odpowiedź z Splunk: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Dane zostały pomyślnie wysłane do indeksu w Splunku.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Wystąpił błąd: {response.StatusCode} - {response.ReasonPhrase}");
                        return false; // Zwróć false w przypadku błędu
                    }
                }
            }
            catch (HttpRequestException e)
            {
                _viewModel.DataIsSendingToSplunk = false;
                await ErrorHandler.ShowErrorNetwork($"Brak połączenia z siecią OT proszę wezwać Utrzymanie Ruchu przez telefon...");
                Console.WriteLine($"Wystąpił błąd HTTP: {e.Message}");
                return false; // Zwróć false w przypadku błędu
            }
            catch (Exception e)
            {
                _viewModel.DataIsSendingToSplunk = false;
                await ErrorHandler.ShowErrorNetwork($"Brak połączenia z siecią OT proszę wezwać Utrzymanie Ruchu przez telefon...");
                return false; // Zwróć false w przypadku błędu
            }
            finally
            {
                _viewModel.DataIsSendingToSplunk = false;
            }
        }

        private HttpClient CreateHttpClientWithKeepAlive()
        {
            var httpClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = true, // You may need to customize this based on your requirements
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            var httpClient = new HttpClient(httpClientHandler);
            httpClient.DefaultRequestHeaders.ConnectionClose = false;

            return httpClient;
        }

        private string ConvertDataToJson<T>(T data, string unixTimeMilliSeconds, string currentTime)
        {
            var type = typeof(T);
            var eventFields = new Dictionary<string, string>();
            var generalFields = new Dictionary<string, string>();
            var otherFields = new Dictionary<string, string>();

            foreach (var property in type.GetProperties())
            {
                var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
                var propertyValue = property.GetValue(data);

                if (propertyValue != null && propertyName != "time" && propertyValue != "")
                {
                    if (propertyName == "name" || propertyName == "value" || propertyName == "status" 
                        || propertyName == "timeEpoch" || propertyName == "reason" || propertyName == "device"
                        || propertyName == "testFault" || propertyName == "totalAbs" || propertyName == "tGoodAbs"
                        || propertyName == "operator" || propertyName == "testObject" || propertyName == "serialNumber"
                        || propertyName == "id" || propertyName == "callForService" || propertyName == "startTime" 
                        || propertyName == "callForServiceRunning" || propertyName == "serviceArrival"
                        || propertyName == "serviceArrivalRunning" || propertyName == "endTime" || propertyName == "isActive"
                        || propertyName == "durationStatus" || propertyName == "durationService" || propertyName == "durationWaitingForService" 
                        || propertyName == "stepOfStatus")
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
            string jsonPayload;

            // Add general fields based on your viewModel condition
            if (_viewModel.IsTimeStampFromiPC)
            {
                generalFields.Add("time", unixTimeMilliSeconds);
                generalFields.Add("sourcetype", "_json");
                generalFields.Add("index", appConfig.Index);
                generalFields.Add("source", appConfig.Source);

                eventFields.Add("timeEpoch", unixTimeMilliSeconds);
                otherFields.Add("workplaceName", appConfig.WorkplaceName);
                otherFields.Add("isMachine", appConfig.IsMachine);
                otherFields.Add("line", appConfig.Line);
                otherFields.Add("hostname", appConfig.Hostname);
                otherFields.Add("workplace", appConfig.Workplace);

                // Build json file
                jsonPayload = JsonConvert.SerializeObject(new
                {
                    time = generalFields["time"],
                    sourcetype = generalFields["sourcetype"],
                    index = generalFields["index"],
                    source = generalFields["source"],
                    @event = eventFields,
                    @fields = otherFields
                }, Formatting.None); // delete format
            }
            else
            {
                generalFields.Add("sourcetype", "_json");
                generalFields.Add("index", appConfig.Index);
                generalFields.Add("source", appConfig.Source);

                eventFields.Add("time", currentTime);
                eventFields.Add("timeEpoch", unixTimeMilliSeconds);

                otherFields.Add("workplaceName", appConfig.WorkplaceName);
                otherFields.Add("isMachine", appConfig.IsMachine);
                otherFields.Add("line", appConfig.Line);
                otherFields.Add("hostname", appConfig.Hostname);
                otherFields.Add("workplace", appConfig.Workplace);

                // Build json file
                jsonPayload = JsonConvert.SerializeObject(new
                {
                    sourcetype = generalFields["sourcetype"],
                    index = generalFields["index"],
                    source = generalFields["source"],
                    @event = eventFields,
                    @fields = otherFields
                }, Formatting.None); // delete format
            }

            // Clear line and blank symbols
            var jsonPayloadWithoutWhitespace = Regex.Replace(jsonPayload, @"\s+", "");

            return jsonPayloadWithoutWhitespace;
        }
    }
}

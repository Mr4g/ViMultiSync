using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ViSyncMaster.DataModel;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;
using System.Threading;
using Avalonia.Threading;
using ViSyncMaster.AuxiliaryClasses;

namespace ViSyncMaster.Repositories
{
    public class GenericSplunkLogger<T>
    {
        private string splunkUrl = null;
        private string hecToken = null;

        private ConcurrentQueue<T> messageQueue = new ConcurrentQueue<T>();
        private bool isSending = false;  // Dodaj flagę, aby sprawdzić, czy już wysyłasz
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);


        private SharedDataService _sharedDataService;
        private AppConfigData appConfig;
        private MainWindowViewModel _viewModel;

        public GenericSplunkLogger(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            _sharedDataService = new SharedDataService();
            this.appConfig = _sharedDataService.AppConfig;
            this.splunkUrl = _sharedDataService.AppConfig.UrlSplunk ?? string.Empty;
            this.hecToken = _sharedDataService.AppConfig.TokenSplunk;
        }

        public async Task LogAsync(T data)
        {
            messageQueue.Enqueue(data);

            await ProcessQueueAsync();
        }

        private async Task ProcessQueueAsync()
        {
            if (semaphoreSlim.CurrentCount == 0)
            {
                // Jeśli inny wątek aktualnie coś wysyła, zakończ
                return;
            }

            await semaphoreSlim.WaitAsync();

            try
            {
                while (messageQueue.TryDequeue(out T data))
                {
                    await SendDataAsync(data);
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task SendDataAsync(T data)
        {
            _viewModel.DataIsSendingToSplunk = true;

            try
            {
                using (var client = CreateHttpClientWithKeepAlive())
                {
                    HttpResponseMessage response = null;

                    client.DefaultRequestHeaders.Add("Authorization", $"Splunk {hecToken}");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.fff");

                    DateTimeOffset dto;
                    var startTimeProperty = typeof(T).GetProperty("StartTime");

                    if (startTimeProperty != null)
                    {
                        var startTimeValue = startTimeProperty.GetValue(data) as DateTime?;
                        dto = startTimeValue.HasValue
                            ? new DateTimeOffset(startTimeValue.Value.ToUniversalTime())
                            : new DateTimeOffset(DateTime.UtcNow);
                    }
                    else
                    {
                        // Jeśli właściwość StartTime nie istnieje
                        dto = new DateTimeOffset(DateTime.UtcNow);
                    }
                    string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();

                    string jsonPayload;

                    // Wybierz metodę konwersji danych w zależności od typu danych
                    if (data is MessagePgToSplunk messageData)
                    {
                        // Jeśli dane są typu MessagePgToSplunk, używamy specyficznej metody konwersji
                        jsonPayload = ConvertDataToJsonForMessagePgToSplunk(messageData, unixTimeMilliSeconds, currentTime);
                    }
                    else
                    {
                        // W przeciwnym razie używamy ogólnej metody konwersji
                        jsonPayload = ConvertDataToJson(data, unixTimeMilliSeconds, currentTime);
                    }

                    StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    Console.WriteLine($"Wysyłanie");

                    // Ustawienie timeout dla HttpClient
                    var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Ustaw timeout na 30 sekund

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
                        messageQueue.Enqueue(data);
                        return; // Przerwij wysyłanie i wróć
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Odpowiedź z Splunk: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Dane zostały pomyślnie wysłane do indeksu w Splunku.");
                    }
                    else
                    {
                        Console.WriteLine($"Wystąpił błąd: {response.StatusCode} - {response.ReasonPhrase}");
                        // Jeśli wystąpił błąd, ponownie dodaj dane do kolejki
                        messageQueue.Enqueue(data);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                _viewModel.DataIsSendingToSplunk = false;

                await ErrorHandler.ShowErrorNetwork($"Brak połączenia z siecią OT proszę wezwać Utrzymanie Ruchu przez telefon...");
                Console.WriteLine($"Wystąpił błąd HTTP: {e.Message}");
                // Jeśli wystąpił błąd, ponownie dodaj dane do kolejki
                messageQueue.Enqueue(data);
            }
            catch (Exception e)
            {
                _viewModel.DataIsSendingToSplunk = false;
                await ErrorHandler.ShowErrorNetwork($"Brak połączenia z siecią OT proszę wezwać Utrzymanie Ruchu przez telefon...");
                // Jeśli wystąpił błąd, ponownie dodaj dane do kolejki
                messageQueue.Enqueue(data);
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

        private string ConvertDataToJson(T data, string unixTimeMilliSeconds, string currentTime)
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
                        || propertyName == "rGoodAbs" || propertyName == "testWithRetest"
                        || propertyName == "operator" || propertyName == "testObject" || propertyName == "serialNumber"
                        || propertyName == "timeOfAllStatus" || propertyName == "timeOfAllRepairs"
                        || propertyName == "productionTime" || propertyName == "preparationTime" || propertyName == "taktTime"
                        || propertyName == "unitsProduced" || propertyName == "passedUnits" || propertyName == "failedUnits"
                        || propertyName == "productNumber" || propertyName == "operatorId" || propertyName == "efficiency"
                        || propertyName == "efficiencyRequired" || propertyName == "target" || propertyName == "plan"
                        || propertyName == "passedPiecesPerShift" || propertyName == "failedPiecesPerShift"
                        || propertyName == "sendTime" || propertyName == "sendStatus")
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

            // Add general fields
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

        private string ConvertDataToJsonForMessagePgToSplunk(MessagePgToSplunk data, string unixTimeMilliSeconds, string currentTime)
        {
            var eventFields = new Dictionary<string, string>();
            var generalFields = new Dictionary<string, string>();
            var otherFields = new Dictionary<string, string>();


            if (!string.IsNullOrEmpty(data.Producing))
                eventFields["S1.Producing_PG"] = data.Producing;

            if (!string.IsNullOrEmpty(data.Waiting))
                eventFields["S1.Waiting_PG"] = data.Waiting;

            if (!string.IsNullOrEmpty(data.MaintenanceMode))
                eventFields["S1.MaintenanceMode_PG"] = data.MaintenanceMode;

            if (!string.IsNullOrEmpty(data.SettingMode))
                eventFields["S1.SettingMode_PG"] = data.SettingMode;

            if (!string.IsNullOrEmpty(data.MachineDowntime))
                eventFields["S1.MachineDowntime_PG"] = data.MachineDowntime;

            if (!string.IsNullOrEmpty(data.LogisticMode))
                eventFields["S1.LogisticMode_PG"] = data.LogisticMode;

            string jsonPayload;

            generalFields.Add("time", unixTimeMilliSeconds);
            generalFields.Add("sourcetype", "_json");
            generalFields.Add("index", appConfig.Index);
            generalFields.Add("source", appConfig.Source);
            eventFields.Add("timeEpoch", unixTimeMilliSeconds);

            otherFields.Add("isMachine", appConfig.IsMachine);
            otherFields.Add("hostname", appConfig.Hostname);

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

            // Tym wywołaniem metody usuwającej białe znaki i polskie znaki:
            var jsonPayloadCleaned = RemoveDiacriticsAndWhitespace(jsonPayload);

            return jsonPayloadCleaned;
        }
        private string RemoveDiacriticsAndWhitespace(string input)
        {
            // Usuń białe znaki (spacje, taby, nowe linie)
            string noWhitespace = Regex.Replace(input, @"\s+", "");

            // Normalizacja - rozkłada znaki z diakrytykami (np. ą -> a + ˛)
            string normalized = noWhitespace.Normalize(NormalizationForm.FormD);

            // Usuń znaki diakrytyczne (kategoria Mn - NonSpacingMark)
            string noDiacritics = Regex.Replace(normalized, @"\p{Mn}+", "");

            // Zamień wyjątki (Ł, ł)
            return noDiacritics.Replace('Ł', 'L').Replace('ł', 'l');
        }
    }
}

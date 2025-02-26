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

                    DateTimeOffset dto;
                    var startTimeProperty = typeof(T).GetProperty("StartTime");
                    var sendTimeProperty = typeof(T).GetProperty("SendTime");

                    if(sendTimeProperty != null)
                    {
                        var sendTimeValue = sendTimeProperty.GetValue(data) as DateTime?;
                        dto = sendTimeValue.HasValue
                            ? new DateTimeOffset(sendTimeValue.Value.ToUniversalTime())
                            : new DateTimeOffset(DateTime.UtcNow);
                    }
                    else if (startTimeProperty != null)
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

                    string jsonPayload = ConvertDataToJson(data, unixTimeMilliSeconds, currentTime);

                    StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    Console.WriteLine($"Wysyłanie");

                    // Ustawienie timeout dla HttpClient
                    var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Ustaw timeout na 10 sekund

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

            // Najpierw zbierz wszystkie właściwości do listy
            var properties = type.GetProperties()
                                 .Select(p => new
                                 {
                                     PropertyName = char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1),
                                     PropertyValue = p.GetValue(data)
                                 })
                                 .Where(p => p.PropertyValue != null && p.PropertyValue.ToString() != "" && p.PropertyName != "time")
                                 .ToList();

            var sendTimeProperty = type.GetProperty("SendTime");
            string sendTimeValue = sendTimeProperty?.GetValue(data)?.ToString();

            // Sprawdź najpierw, czy istnieje właściwość "name" o odpowiedniej wartości
            var nameProperty = properties.FirstOrDefault(p => p.PropertyName == "name" &&
                (p.PropertyValue.ToString() == "S7.TestingPassed" || p.PropertyValue.ToString() == "S7.TestingFailed"));

            if (nameProperty != null)
            {
                // Jeśli znajdziesz "name" o wartości "S7.TestingPassed" lub "S7.TestingFailed", obsługuj tylko te pola
                eventFields.Clear();
                eventFields["name"] = nameProperty.PropertyValue.ToString();

                // Znajdź "value" w properties i dodaj je do eventFields
                var valueProperty = properties.FirstOrDefault(p => p.PropertyName == "value");
                var idProperty = properties.FirstOrDefault(p => p.PropertyName == "id");
                var productNameProperty = properties.FirstOrDefault(p => p.PropertyName == "productName");
                var operatorIdProperty = properties.FirstOrDefault(p => p.PropertyName == "operatorId");
                if (valueProperty != null)
                {
                    eventFields["id"] = idProperty.PropertyValue?.ToString() ?? "default_value";
                    eventFields["value"] = valueProperty.PropertyValue?.ToString() ?? "default_value";
                    eventFields["productName"] = productNameProperty?.PropertyValue?.ToString() ?? "default_value";
                    eventFields["operatorId"] = operatorIdProperty?.PropertyValue?.ToString() ?? "default_value";
                }
            }
            else
            {
                // Standardowa logika dla pozostałych przypadków
                foreach (var property in properties)
                {
                    if (property.PropertyName == "name" || property.PropertyName == "value" || property.PropertyName == "status"
                        || property.PropertyName == "timeEpoch" || property.PropertyName == "reason" || property.PropertyName == "device"
                        || property.PropertyName == "testFault" || property.PropertyName == "totalAbs" || property.PropertyName == "tGoodAbs"
                        || property.PropertyName == "operator" || property.PropertyName == "testObject" || property.PropertyName == "serialNumber"
                        || property.PropertyName == "id" || property.PropertyName == "callForService" || property.PropertyName == "startTime"
                        || property.PropertyName == "callForServiceRunning" || property.PropertyName == "serviceArrival"
                        || property.PropertyName == "serviceArrivalRunning" || property.PropertyName == "endTime" || property.PropertyName == "isActive"
                        || property.PropertyName == "durationStatus" || property.PropertyName == "durationService" || property.PropertyName == "durationWaitingForService"
                        || property.PropertyName == "stepOfStatus" || property.PropertyName == "efficiency" || property.PropertyName == "efficiencyRequired" 
                        || property.PropertyName == "target" || property.PropertyName == "passedPiecesPerShift" || property.PropertyName == "failedPiecesPerShift")
                    {
                        eventFields[property.PropertyName] = property.PropertyValue.ToString();
                    }
                    else if (property.PropertyName == "source")
                    {
                        generalFields[property.PropertyName] = property.PropertyValue.ToString();
                    }
                    else
                    {
                        otherFields[property.PropertyName] = property.PropertyValue.ToString();
                    }
                }
            }
            string jsonPayload;

            // Add general fields based on your viewModel condition
            if (_viewModel.IsTimeStampFromiPC)
            {
                generalFields.Add("time", string.IsNullOrEmpty(sendTimeValue) ? unixTimeMilliSeconds : sendTimeValue);
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
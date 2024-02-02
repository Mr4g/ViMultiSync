using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Avalonia.ReactiveUI;
using Avalonia;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using ViMultiSync.DataModel;
using ViMultiSync.Services;
using ViMultiSync.ViewModels;
using System.Threading;

namespace ViMultiSync.Repositories
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
            this.splunkUrl = _sharedDataService.AppConfig.UrlSplunk;
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

                    DateTimeOffset dto = new DateTimeOffset(DateTime.UtcNow);
                    string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();

                    string jsonPayload = ConvertDataToJson(data, unixTimeMilliSeconds, currentTime);

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
                // Jeśli wystąpił błąd, ponownie dodaj dane do kolejki
                messageQueue.Enqueue(data);
            }
            catch (Exception e)
            {
                _viewModel.DataIsSendingToSplunk = false;
                // Obsługa innych ogólnych błędów
                Console.WriteLine($"Wystąpił błąd: {e.Message}");
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

                if (propertyValue != null && propertyName != "time")
                {
                    if (propertyName == "name" || propertyName == "value" || propertyName == "status" || propertyName == "timeEpoch" || propertyName == "reason")
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
    }
}

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
    public class GetLookup
    {
        private string splunkUrl = null;
        private string hecToken = null;

        private bool isSending = false;  // Dodaj flagę, aby sprawdzić, czy już wysyłasz
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);


        private SharedDataService _sharedDataService;
        private AppConfigData appConfig;
        private MainWindowViewModel _viewModel;

        public GetLookup(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            _sharedDataService = new SharedDataService();
            this.appConfig = _sharedDataService.AppConfig;
            this.splunkUrl = _sharedDataService.AppConfig.UrlSplunk;
            this.hecToken = _sharedDataService.AppConfig.TokenSplunk;
        }
        public async Task GetLookupDefinitionsAsync()
        {
            string splunkEndpoint = "https://10.13.33.40:8089/services/data/transforms/lookups/CHL.test.csv";
            string accessToken = "a5a26423-9874-4383-8f31-436c3c86a4ee";
            string username = "vi-ipad-local-w16";
            string password = "SplunkW16";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Dodaj nagłówki autoryzacyjne
                    var byteArray = System.Text.Encoding.UTF8.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    // Wykonaj żądanie HTTP GET
                    HttpResponseMessage response = await client.GetAsync(splunkEndpoint);

                    if (response.IsSuccessStatusCode)
                    {
                        // Pobierz odpowiedź w formie tekstu
                        string responseData = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseData);
                    }
                    else
                    {
                        Console.WriteLine($"Błąd: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd: {ex.Message}");
            }
        }

    }
}

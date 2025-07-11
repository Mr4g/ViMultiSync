﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.DataModel.Extension;
using ViSyncMaster.Entitys;
using ViSyncMaster.Heleprs;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;

namespace ViSyncMaster.Services
{
    public class MachineStatusService
    {
        private readonly GenericRepository<MachineStatus> _repositoryMachineStatus;
        private readonly GenericRepository<MachineStatus> _repositoryMachineStatusQueue;
        private readonly GenericRepository<MachineStatus> _repositoryTestingResultQueue;
        private readonly GenericRepository<MachineStatus> _repositoryTestingResult;
        private readonly GenericRepository<ProductionEfficiency> _repositoryProductionEfficiency;
        private readonly GenericRepository<FirstPartModel> _repositoryFirstPartQueue;
        private readonly MqttMessageSender _mqttMessageSender;
        private readonly MessageSender _messageSender;
        private readonly MessageQueue _messageQueue;
        private readonly SQLiteDatabase _database;
        private long _lastStatusId = 0;
        private CancellationTokenSource _cacheUpdatedCts = new();
        private Task _lastTask = Task.CompletedTask;
        public event Action? TableResultTestUpdate;
        private AppConfigData _appConfig = MainWindowViewModel.appConfig;
        private ConfigMqtt _mqttConfig = MainWindowViewModel.mqttConfig;
        private readonly Debouncer _queueDebouncer;
        private bool _hasQueueEvent;
        private string? _lastQueueTable;

        // cache for downtime queries
        private List<MachineStatus> _downtimeCache = new();
        private DateTime _downtimeCacheStart;
        private DateTime _downtimeCacheEnd;
        private DateTime _lastDowntimeRefresh = DateTime.MinValue;
        private readonly TimeSpan _downtimeCacheDuration = TimeSpan.FromMinutes(1);


        // Konstruktor przyjmuje repozytoria generyczne dla obu tabel
        public MachineStatusService(GenericRepository<MachineStatus> repositoryMachineStatus,
                                    GenericRepository<MachineStatus> repositoryMachineStatusQueue,
                                    GenericRepository<MachineStatus> repositoryTestingResultQueue,
                                    GenericRepository<MachineStatus> repositoryTestingResult,
                                    GenericRepository<ProductionEfficiency> repositoryProductionEfficiency,
                                    GenericRepository<FirstPartModel> repositoryFirstPartQueue,
                                    MessageSender messageSender,
                                    MessageQueue messageQueue,
                                    SQLiteDatabase database,
                                    MqttMessageSender mqttMessageSender)
        {
            _repositoryMachineStatus = repositoryMachineStatus;
            _repositoryMachineStatusQueue = repositoryMachineStatusQueue;
            _repositoryTestingResultQueue = repositoryTestingResultQueue;
            _repositoryTestingResult = repositoryTestingResult;
            _repositoryProductionEfficiency = repositoryProductionEfficiency;
            _repositoryFirstPartQueue = repositoryFirstPartQueue;
            _mqttMessageSender = mqttMessageSender;
            _messageSender = messageSender;
            _messageQueue = messageQueue;
            _database = database;
            _queueDebouncer = new Debouncer(3000, FlushQueue);

            Debug.WriteLine("Rejestracja subskrypcji CacheUpdated");
            _repositoryMachineStatusQueue.CacheUpdated += info => OnRepoUpdated(info);
            _repositoryTestingResultQueue.CacheUpdated += info => OnRepoUpdated(info);
            _repositoryProductionEfficiency.CacheUpdated += info => OnRepoUpdated(info);
            _repositoryFirstPartQueue.CacheUpdated += info => OnRepoUpdated(info);

        }

        // Rozpoczęcie nowego statusu
        public async Task<MachineStatus> StartStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;
            machineStatus.SendTime = epochMilliseconds;

            // Asynchronicznie dodawanie status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat());
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }

        public async Task<List<MachineStatus>> ReportBatchPartQuality(List<Rs232Data> testBatch)
        {
            var results = new List<MachineStatus>();

            // Pierwszy przebieg - Przetwarzanie TestingPassed
            foreach (var data in testBatch)
            {
                // Jeżeli TestingPassed ma wartość, tworzy TestingPassedMessage
                if (!string.IsNullOrEmpty(data.TestingPassed))
                {
                    var machineStatus = new TestingPassedMessage
                    {
                        ProductName = data.ProductName,
                        OperatorId = data.OperatorId,
                        StartTime = DateTime.Now,
                        SendTime = data.Timestamp,
                        Id = IdGenerator.GetNextId()
                    };
                    machineStatus.SetValue(data.TestingPassed); // Ustawienie wartości TestingPassed
                    results.Add(machineStatus);
                }
            }

            // Drugi przebieg - Przetwarzanie TestingFailed
            foreach (var data in testBatch)
            {
                // Jeżeli TestingFailed ma wartość, tworzysz TestingFailedMessage
                if (!string.IsNullOrEmpty(data.TestingFailed))
                {
                    var machineStatus = new TestingFailedMessage
                    {
                        ProductName = data.ProductName,
                        OperatorId = data.OperatorId,
                        StartTime = DateTime.Now,
                        SendTime = data.Timestamp,
                        Id = IdGenerator.GetNextId()
                };
                    machineStatus.SetValue(data.TestingFailed); // Ustawienie wartości TestingFailed
                    results.Add(machineStatus);
                }
            }

            // Jednorazowe zapisanie całej listy (np. do kolejki lub bazy)
            await _repositoryTestingResultQueue.AddOrUpdateBatch(results);
            await _repositoryTestingResult.AddOrUpdateBatch(results);

            Log.Information("Batch zapisany ({Count} sztuk)", results.Count);

            return results;
        }

        public async Task<ProductionEfficiency> RaportProdcuctionEfficiency(ProductionEfficiency productionEfficiency)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;
            productionEfficiency.SendTime = epochMilliseconds;
            productionEfficiency.Id = uniqueId;
            await _repositoryProductionEfficiency.AddOrUpdate(productionEfficiency);
            return productionEfficiency;
        }

        public async Task<MachineCounters> SendShiftCounterMqtt(MachineCounters machineCounters)
        {
            var jsonMessage = JsonSerializer.Serialize(machineCounters.ToMqttFormatForCuntersMessage());
            await SendMessageMqtt(jsonMessage);
            return machineCounters;
        }

        public async Task<FirstPartModel> SendFirstPartAsync(FirstPartModel firstPartModel)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            firstPartModel.SendTime = epochMilliseconds;
            firstPartModel.Id = uniqueId;
            await _repositoryFirstPartQueue.AddOrUpdate(firstPartModel);
            return firstPartModel;
        }

        // Wysyłanie wiadomości które już posiadają ID/Start/End etc..
        public async Task<MachineStatus> ReSendMessageToSplunk(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            machineStatus.SendTime = epochMilliseconds;
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat());
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }

        public async Task<MachineStatus> UpdateStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); 
            machineStatus.SendTime = epochMilliseconds;
            // Asynchronicznie dodaj status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);         
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat());
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }
        public async Task<MessagePgToSplunk> SendPgMessage(MessagePgToSplunk machineStatus)
        {
            // Asynchronicznie dodaj status do repozytoriów
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttPgFormat());          
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }

        // Zakończenie statusu
        public async Task<MachineStatus> EndStatus(MachineStatus machineStatus)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            machineStatus.SendTime = epochMilliseconds;
            machineStatus.EndTime = DateTime.Now;  // Zaktualizowanie czasu zakończenia 
            // Zapisz zaktualizowany status w repozytorium MachineStatus
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat());
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }

        private void OnRepoUpdated(DatabaseOperationInfo info)
        {
            if (info.TableName.Contains("Queue", StringComparison.OrdinalIgnoreCase))
            {
                _hasQueueEvent = true;
                _lastQueueTable = info.TableName;
            }
            if (info.TableName.Contains("MachineStatus", StringComparison.OrdinalIgnoreCase))
            {
                _downtimeCache.Clear();
            }
            _queueDebouncer.Debounce();
        }

        private void FlushQueue()
        {
            if (!_hasQueueEvent) return;

            try
            {
                TableResultTestUpdate?.Invoke();
                _ = _messageQueue.SendAllMessages().ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Debug.WriteLine($"[FlushQueue] Błąd: {t.Exception}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _hasQueueEvent = false;
                _lastQueueTable = null;
            }
        }

        public async Task SendMessageMqtt(string messageq)
        {
            string topic = $"{_mqttConfig.topic}{_appConfig.Source}";  // Zdefiniuj temat

            try
            {
                // Wyślij wiadomość za pomocą MqttMessageSender
                await _mqttMessageSender.SendMqttMessageAsync(topic, messageq);  // Przekazanie obiektu bezpośrednio
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<double> GetDowntimeMinutesAsync(DateTime start, DateTime end)
        {
            var records = await GetDowntimeRecordsAsync(start, end);
            double total = 0;
            foreach (var r in records)
            {
                var s = r.StartTime ?? start;
                var e = r.EndTime ?? DateTime.Now;
                if (e <= start || s >= end) continue;
                if (s < start) s = start;
                if (e > end) e = end;
                total += (e - s).TotalMinutes;
            }
            return total;
        }

        private async Task<List<MachineStatus>> GetDowntimeRecordsAsync(DateTime start, DateTime end)
        {
            bool refresh = _downtimeCache.Count == 0 ||
                           start < _downtimeCacheStart ||
                           end > _downtimeCacheEnd ||
                           (DateTime.Now - _lastDowntimeRefresh) > _downtimeCacheDuration;

            if (refresh)
            {
                const string query = @"SELECT * FROM MachineStatus WHERE Name = @name AND StartTime <= @end AND (EndTime >= @start OR EndTime IS NULL)";
                var parameters = new Dictionary<string, object>
                {
                    ["@name"] = "S1.MachineDowntime_IPC",
                    ["@start"] = start,
                    ["@end"] = end
                };

                _downtimeCache = await _database.ExecuteReaderAsync<MachineStatus>(query, parameters);
                _downtimeCacheStart = start;
                _downtimeCacheEnd = end;
                _lastDowntimeRefresh = DateTime.Now;
            }

            return _downtimeCache.Where(r => (r.StartTime ?? start) <= end && (r.EndTime ?? DateTime.Now) >= start).ToList();
        }

        public double GetDowntimeMinutes(DateTime start, DateTime end) => GetDowntimeMinutesAsync(start, end).GetAwaiter().GetResult();
    }
}


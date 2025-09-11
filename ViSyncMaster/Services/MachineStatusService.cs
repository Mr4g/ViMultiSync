using Serilog;
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
        private readonly GenericRepository<HourlyPlanMessage> _repositoryHourlyPlan;
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

        // names of statuses treated as downtime
        private readonly string[] _downtimeNames = new[]
        {
            "S1.LogisticMode_IPC",
            "S1.ProductionIssuesMode_IPC",
            "S1.SettingMode_IPC",
            "S1.MachineDowntime_IPC"
        };


        // Konstruktor przyjmuje repozytoria generyczne dla obu tabel
        public MachineStatusService(GenericRepository<MachineStatus> repositoryMachineStatus,
                                    GenericRepository<MachineStatus> repositoryMachineStatusQueue,
                                    GenericRepository<MachineStatus> repositoryTestingResultQueue,
                                    GenericRepository<MachineStatus> repositoryTestingResult,
                                    GenericRepository<ProductionEfficiency> repositoryProductionEfficiency,
                                    GenericRepository<FirstPartModel> repositoryFirstPartQueue,
                                    GenericRepository<HourlyPlanMessage> repositoryHourlyPlan,
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
            _repositoryHourlyPlan = repositoryHourlyPlan;
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
            _repositoryHourlyPlan.CacheUpdated += info => OnRepoUpdated(info);
        }

        // Rozpoczęcie nowego statusu
        public async Task<MachineStatus> StartStatus(MachineStatus machineStatus, bool stopsLine)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            var uniqueId = epochMilliseconds;

            machineStatus.Id = uniqueId;
            machineStatus.StartTime = DateTime.Now;
            machineStatus.SendTime = epochMilliseconds;

            // Asynchronicznie dodawanie status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat(stopsLine));
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
        public async Task ReportHourlyPlanAsync(List<HourlyPlanMessage> planMessages)
        {
            foreach (var msg in planMessages)
            {
                var epoch = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                msg.SendTime = epoch;
                msg.Id = epoch;
            }

            await _repositoryHourlyPlan.AddOrUpdateBatch(planMessages);
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
        public async Task<MachineStatus> ReSendMessageToSplunk(MachineStatus machineStatus, bool stopsLine)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            machineStatus.SendTime = epochMilliseconds;
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat(stopsLine));
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }

        public async Task<MachineStatus> UpdateStatus(MachineStatus machineStatus, bool stopsLine)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); 
            machineStatus.SendTime = epochMilliseconds;
            // Asynchronicznie dodaj status do repozytoriów
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);         
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat(stopsLine));
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
        public async Task<MachineStatus> EndStatus(MachineStatus machineStatus, bool stopsLine)
        {
            var epochMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Czas epoch w milisekundach
            machineStatus.SendTime = epochMilliseconds;
            machineStatus.EndTime = DateTime.Now;  // Zaktualizowanie czasu zakończenia 
            // Zapisz zaktualizowany status w repozytorium MachineStatus
            await _repositoryMachineStatusQueue.AddOrUpdate(machineStatus);
            await _repositoryMachineStatus.AddOrUpdate(machineStatus);
            var jsonMessage = JsonSerializer.Serialize(machineStatus.ToMqttFormat(stopsLine));
            await SendMessageMqtt(jsonMessage);
            return machineStatus;
        }

        private void OnRepoUpdated(DatabaseOperationInfo info)
        {
            if (info.TableName.Contains("Queue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(info.TableName, "ProductionEfficiency", StringComparison.OrdinalIgnoreCase)
                || string.Equals(info.TableName, "FirstPartData", StringComparison.OrdinalIgnoreCase)
                || string.Equals(info.TableName, "HourlyPlanMessage", StringComparison.OrdinalIgnoreCase))
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

            var intervals = records
                .Select(r =>
                {
                    var s = r.StartTime ?? start;
                    var e = r.EndTime ?? DateTime.Now;
                    if (s < start) s = start;
                    if (e > end) e = end;
                    return (Start: s, End: e);
                })
                .Where(iv => iv.End > start && iv.Start < end)
                .OrderBy(iv => iv.Start)
                .ToList();

            double total = 0;
            DateTime? currentStart = null;
            DateTime? currentEnd = null;

            foreach (var iv in intervals)
            {
                if (currentStart == null)
                {
                    currentStart = iv.Start;
                    currentEnd = iv.End;
                }
                else if (iv.Start <= currentEnd)
                {
                    if (iv.End > currentEnd) currentEnd = iv.End;
                }
                else
                {
                    total += (currentEnd.Value - currentStart.Value).TotalMinutes;
                    currentStart = iv.Start;
                    currentEnd = iv.End;
                }
            }

            if (currentStart != null)
                total += (currentEnd.Value - currentStart.Value).TotalMinutes;

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
                var namePlaceholders = string.Join(", ", _downtimeNames.Select((_, i) => $"@name{i}"));
                var query = $@"SELECT * FROM MachineStatus WHERE Name IN ({namePlaceholders}) AND StartTime <= @end AND (EndTime >= @start OR EndTime IS NULL)";

                var parameters = new Dictionary<string, object>
                {
                    ["@start"] = start,
                    ["@end"] = end
                };
                for (int i = 0; i < _downtimeNames.Length; i++)
                {
                    parameters[$"@name{i}"] = _downtimeNames[i];
                }

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


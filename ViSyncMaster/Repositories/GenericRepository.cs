using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using ViSyncMaster.DataModel;
using System.IO;
using ViSyncMaster.Services;
using ViSyncMaster.Entitys;
using Serilog;
using DynamicData;
using System.Diagnostics;
using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using LiveChartsCore.Themes;

namespace ViSyncMaster.Repositories
{
    public class GenericRepository<T> where T : IEntity, new()
    {
        private readonly SQLiteDatabase _db;
        private readonly string _tableName;
        private readonly List<T> _cache = new List<T>();  // Przechowywanie danych w pamięci
        private readonly List<T> _cacheTestResult = new List<T>();
        private readonly QueueManager<T> _queueManager;
        private bool _hasIsActive;

        public event Action? CacheUpdated;

        public string TableName => _tableName;  // Właściwość do odczytu nazwy tabeli

        public GenericRepository(SQLiteDatabase db, string tableName)
        {
            _db = db;
            _tableName = tableName;
            // Initialize the QueueManager with the processing function
            _queueManager = new QueueManager<T>(AddOrUpdateInternalSync);  // Inicjalizacja instancji QueueManager
            Debug.WriteLine($"Initialized QueueManager for {0} {tableName}", tableName);
            RestoreFromBackup();
            UpdateCacheAsync();
            if(_tableName == "MachineStatus")
                _hasIsActive = true;
        }

        public async Task AddOrUpdate(T entity)
        {
            if (entity.Id == 0)
            {
                entity.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Ustawienie Id na podstawie czasu epoch w milisekundach
            }
            // Dodaj do kolejki zamiast bezpośrednio do bazy danych
            await _queueManager.Enqueue(entity);  // Użycie instancji QueueManager
        }

        // Dodawanie/aktualizowanie rekordu w bazie danych
        public async Task AddOrUpdateInternal(T entity)
        {
            try
            {
                string query = $@"
            INSERT INTO {_tableName} 
            ({GetColumns(entity)}) 
            VALUES 
            ({GetColumnParameters(entity)})
            ON CONFLICT(Id) DO UPDATE SET
            {GetUpdateColumns(entity)}";

                await _db.ExecuteNonQuery(query, entity);
                Log.Information("Dodano/zmodyfikowano rekord w tabeli {TableName}: {Entity}", _tableName, entity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd podczas zapisywania rekordu do tabeli {TableName}: {Entity}", _tableName, entity);
                SaveToBackupFile(entity);
            }
        }

        public async void AddOrUpdateInternalSync(T entity)
        {
            try
            {
                string query = $@"
            INSERT INTO {_tableName} 
            ({GetColumns(entity)}) 
            VALUES 
            ({GetColumnParameters(entity)})
            ON CONFLICT(Id) DO UPDATE SET
            {GetUpdateColumns(entity)}";

                _db.ExecuteNonQuery(query, entity).Wait(); // Użyj .Wait() aby wykonać asynchronicznie metodę synchronicznie
                Log.Information("Dodano/zmodyfikowano rekord w tabeli {TableName}: {Entity}", _tableName, entity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd podczas zapisywania rekordu do tabeli {TableName}: {Entity}", _tableName, entity);
                SaveToBackupFile(entity);
            }
            await UpdateCacheAsync();
            Debug.WriteLine($"Wywołanie zdarzenia cacheUpdate");
            CacheUpdated?.Invoke();
        }

        // Metoda do aktualizacji cache
        public async Task UpdateCacheAsync()
        {
            var activeItems = await GetActiveAsync();
            var passedRecords = await GetFromLastWeekAsync("S7.TestingPassed");
            var failedRecords = await GetFromLastWeekAsync("S7.TestingFailed");
            _cacheTestResult.Clear();
            _cache.Clear();
            _cacheTestResult.AddRange(passedRecords);
            _cacheTestResult.AddRange(failedRecords);
            _cache.AddRange(activeItems);
        }

        // Odczyt danych z cache
        public async Task<List<T>> GetFromCache()
        {
            return new List<T>(_cache);  // Zwracamy kopię listy, żeby uniknąć manipulacji z zewnątrz
        }

        public async Task<List<T>> GetFromCacheTestResult()
        {
            return new List<T>(_cacheTestResult);  // Zwracamy kopię listy, żeby uniknąć manipulacji z zewnątrz
        }

        // Pobieranie danych
        public async Task<List<T>> GetAllAsync(string whereClause = "")
        {
            string query = $"SELECT * FROM {_tableName} {whereClause} ORDER BY SendTime ASC";
            return await _db.ExecuteReaderAsync<T>(query);
        }
        public async Task<List<T>> GetActiveAsync()
        {
            if(!_hasIsActive)
            {
                return new List<T>();
            }
            string query = $"SELECT * FROM {_tableName} WHERE IsActive = 1 ORDER BY SendTime ASC";
            Log.Information("Wykonywanie zapytania: {Query}", query);
            var result = await _db.ExecuteReaderAsync<T>(query);
            Log.Information("Znaleziono rekordy: {Count}", result.Count);
            return result;
        }
        public async Task<List<T>> GetFromLastWeekAsync(string name)
        {
            long oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeMilliseconds();

            string query = $"SELECT * FROM {_tableName} WHERE SendTime >= @oneWeekAgo AND Name = @name ORDER BY SendTime ASC";

            Log.Information("Wykonywanie zapytania: {Query} z parametrami Name = {Name}", query, name);

            var parameters = new Dictionary<string, object>
            {
                { "@oneWeekAgo", oneWeekAgo },
                { "@name", name }
            };

            var result = await _db.ExecuteReaderAsync<T>(query, parameters); // Przekazanie parametrów

            Log.Information("Znaleziono rekordy: {Count}", result.Count);
            return result;
        }

        public async Task<List<T>> GetFromLastWeekAsync()
        {
            long oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeMilliseconds();

            string query = $"SELECT * FROM {_tableName} WHERE SendTime >= {oneWeekAgo} ORDER BY SendTime ASC";

            Log.Information("Wykonywanie zapytania: {Query}", query);
            var result = await _db.ExecuteReaderAsync<T>(query);

            Log.Information("Znaleziono rekordy: {Count}", result.Count);
            return result;
        }

        public async Task<List<T>> GetByStatusAsync()
        {
            string query = $"SELECT * FROM {_tableName} WHERE SendStatus = 'Pending' ORDER BY SendTime ASC";
            Log.Information("Wykonywanie zapytania: {Query}", query);
            var result = await _db.ExecuteReaderAsync<T>(query);
            Log.Information("Znaleziono rekordy: {Count}", result?.Count ?? 0);
            return result;
        }

        public async Task<List<T>> GetByStatusInProgressAsync()
        {
            string query = $"SELECT * FROM {_tableName} WHERE SendStatus = 'InProgress' ORDER BY SendTime ASC";
            Log.Information("Wykonywanie zapytania: {Query}", query);
            var result = await _db.ExecuteReaderAsync<T>(query);
            Log.Information("Znaleziono rekordy: {Count}", result?.Count ?? 0);
            return result;
        }

        // Usuwanie rekordu
        public void Delete(long id)
        {
            string query = $"DELETE FROM {_tableName} WHERE Id = @Id";
            _db.ExecuteNonQuery(query, new { Id = id });
        }

        // Pomocnicze metody do generowania kolumn i parametrów SQL
        private string GetColumns(T entity)
        {
            return string.Join(", ", typeof(T).GetProperties().Select(p => p.Name));
        }

        private string GetColumnParameters(T entity)
        {
            return string.Join(", ", typeof(T).GetProperties().Select(p => $"@{p.Name}"));
        }

        private string GetUpdateColumns(T entity)
        {
            return string.Join(", ", typeof(T).GetProperties()
                .Where(p => p.Name != "Id") // Pomijamy kolumnę Id
                .Select(p => $"{p.Name} = excluded.{p.Name}"));
        }

        private void SaveToBackupFile(T entity)
        {
            string backupFilePath = $"backup_{_tableName}.json";


            var allEntities = File.Exists(backupFilePath)
                ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(backupFilePath)) ?? new List<T>()
                : new List<T>();

            allEntities.Add(entity);

            File.WriteAllText(backupFilePath, JsonSerializer.Serialize(allEntities));
        }

        public void RestoreFromBackup()
        {
            string backupFilePath = $"backup_{_tableName}.json";
            if (!File.Exists(backupFilePath)) return;

            try
            {
                var allEntities = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(backupFilePath)) ?? new List<T>();

                foreach (var entity in allEntities)
                {
                    AddOrUpdateInternal(entity).Wait();
                }

                File.Delete(backupFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring backup: {ex.Message}");
            }
        }
    }
}
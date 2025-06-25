using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using Serilog;
using ViSyncMaster.DataModel;
using ViSyncMaster.Services;
using ViSyncMaster.Entitys;
using Path = System.IO.Path;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace ViSyncMaster.Repositories
{
    public class GenericRepository<T> where T : class, IEntity, new()
    {
        private readonly SQLiteDatabase _db;
        private readonly string _tableName;
        private readonly List<T> _cache = new();
        private readonly List<T> _cacheTestResult = new();
        private bool _hasIsActive;

        public event Action<DatabaseOperationInfo>? CacheUpdated;

        public string TableName => _tableName;

        public GenericRepository(SQLiteDatabase db, string tableName)
        {
            _db = db;
            _tableName = tableName;
            RestoreFromBackup();
            UpdateCacheAsync();
            if (_tableName == "MachineStatus")
                _hasIsActive = true;
        }

        public Task AddOrUpdate(T entity)
        {
            if (entity.Id == 0)
                entity.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            GlobalQueue.Instance.Enqueue(new SingleEntityTask<T>(entity, AddOrUpdateInternalSync));
            return Task.CompletedTask;
        }

        public Task AddOrUpdateBatch(List<T> entities)
        {
            entities.ForEach(e =>
            {
                if (e.Id == 0)
                    e.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            });

            GlobalQueue.Instance.Enqueue(new BatchEntityTask<T>(entities, AddOrUpdateInternalBatchSync));
            return Task.CompletedTask;
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

                _db.ExecuteNonQuery(query, entity).Wait();
                Log.Debug("Zapisano rekord w tabeli {TableName}", _tableName);

                // Wywołanie eventu CacheUpdated z obiektem DatabaseOperationInfo
                CacheUpdated?.Invoke(new DatabaseOperationInfo(_tableName, DatabaseOperation.Insert, entity));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd zapisu do tabeli {TableName}", _tableName);
                SaveToBackupFile(entity);
            }

            await UpdateCacheAsync();
        }

        public async void AddOrUpdateInternalBatchSync(List<T> batch)
        {
            try
            {
                foreach (var entity in batch)
                {
                    if (entity.Id == 0)
                        entity.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    string query = $@"
                        INSERT INTO {_tableName} 
                        ({GetColumns(entity)}) 
                        VALUES 
                        ({GetColumnParameters(entity)}) 
                        ON CONFLICT(Id) DO UPDATE SET
                        {GetUpdateColumns(entity)}";

                    _db.ExecuteNonQuery(query, entity).Wait();
                }

                Log.Debug("Zapisano batch ({Count}) elementów do tabeli {TableName}", batch.Count, _tableName);

                // Wywołanie eventu CacheUpdated dla każdego elementu w batchu
                foreach (var entity in batch)
                {
                    CacheUpdated?.Invoke(new DatabaseOperationInfo(_tableName, DatabaseOperation.Update, entity));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd zapisu batcha do tabeli {TableName}", _tableName);
                foreach (var entity in batch)
                    SaveToBackupFile(entity);
            }

            await UpdateCacheAsync();
        }


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
            Log.Debug("Zaktualizowano cache dla tabeli {TableName}", _tableName);
        }

        public async Task<List<T>> GetFromCache() => new(_cache);
        public async Task<List<T>> GetFromCacheTestResult() => new(_cacheTestResult);

        public async Task<List<T>> GetAllAsync(string whereClause = "")
        {
            string query = $"SELECT * FROM {_tableName} {whereClause} ORDER BY SendTime ASC";
            return await _db.ExecuteReaderAsync<T>(query);
        }

        public async Task<List<T>> GetActiveAsync()
        {
            if (!_hasIsActive) return new();

            string query = $"SELECT * FROM {_tableName} WHERE IsActive = 1 ORDER BY SendTime ASC";
            var result = await _db.ExecuteReaderAsync<T>(query);
            return result;
        }

        public async Task<List<T>> GetFromLastWeekAsync(string name)
        {
            long oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeMilliseconds();
            string query = $"SELECT * FROM {_tableName} WHERE SendTime >= @oneWeekAgo AND Name = @name ORDER BY SendTime ASC";
            var parameters = new Dictionary<string, object> { { "@oneWeekAgo", oneWeekAgo }, { "@name", name } };
            return await _db.ExecuteReaderAsync<T>(query, parameters);
        }

        public async Task<List<T>> GetFromLastWeekAsync()
        {
            long oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeMilliseconds();
            string query = $"SELECT * FROM {_tableName} WHERE SendTime >= {oneWeekAgo} ORDER BY SendTime ASC";
            return await _db.ExecuteReaderAsync<T>(query);
        }

        public async Task<List<T>> GetByStatusAsync()
        {
            string query = $"SELECT * FROM {_tableName} WHERE SendStatus = 'Pending' ORDER BY SendTime ASC";
            return await _db.ExecuteReaderAsync<T>(query);
        }

        public async Task<List<T>> GetByStatusInProgressAsync()
        {
            string query = $"SELECT * FROM {_tableName} WHERE SendStatus = 'InProgress' ORDER BY SendTime ASC";
            return await _db.ExecuteReaderAsync<T>(query);
        }

        public void Delete(long id)
        {
            string query = $"DELETE FROM {_tableName} WHERE Id = @Id";
            _db.ExecuteNonQuery(query, new { Id = id });
        }

        private string GetColumns(T entity) => string.Join(", ", typeof(T).GetProperties().Select(p => p.Name));
        private string GetColumnParameters(T entity) => string.Join(", ", typeof(T).GetProperties().Select(p => $"@{p.Name}"));
        private string GetUpdateColumns(T entity) => string.Join(", ", typeof(T).GetProperties().Where(p => p.Name != "Id").Select(p => $"{p.Name} = excluded.{p.Name}"));

        private void SaveToBackupFile(T entity)
        {
            // 1) Poprawna ścieżka bez podwójnych backslashy
            string backupDirectory = @"C:\ViSM\App\backupDB";
            Directory.CreateDirectory(backupDirectory);

            // 2) Pełna ścieżka pliku
            string backupFilePath = Path.Combine(backupDirectory, $"backup_{_tableName}.json");

            List<T> allEntities;

            if (File.Exists(backupFilePath))
            {
                // 3) Wczytaj zawartość
                string json = File.ReadAllText(backupFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    // plik jest pusty -> nowa lista
                    allEntities = new List<T>();
                }
                else
                {
                    try
                    {
                        allEntities = JsonSerializer.Deserialize<List<T>>(json)
                                      ?? new List<T>();
                    }
                    catch (JsonException)
                    {
                        // plik ma niepoprawny JSON -> nadpisz go jako nowy
                        allEntities = new List<T>();
                    }
                }
            }
            else
            {
                allEntities = new List<T>();
            }

            // 4) Dodaj nowy rekord i zapisz
            allEntities.Add(entity);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string output = JsonSerializer.Serialize(allEntities, options);
            File.WriteAllText(backupFilePath, output);

            Log.Warning("Zapisano backup rekordu do pliku {BackupFile}", backupFilePath);
        }


        public void RestoreFromBackup()
        {
            string backupDirectory = @"C:\\ViSM\\App\\backupDB";
            string backupFilePath = Path.Combine(backupDirectory, $"backup_{_tableName}.json");
            if (!File.Exists(backupFilePath)) return;

            try
            {
                var allEntities = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(backupFilePath)) ?? new List<T>();
                foreach (var entity in allEntities)
                {
                    AddOrUpdate(entity).Wait();
                }
                File.Delete(backupFilePath);
                Log.Information("Przywrócono dane z backupu do tabeli {TableName}", _tableName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd przywracania backupu z pliku {BackupFile}", backupFilePath);
            }
        }
    }
}

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

namespace ViSyncMaster.Repositories
{
    public class GenericRepository<T> where T : IEntity, new()
    {
        private readonly SQLiteDatabase _db;
        private readonly string _tableName;
        private readonly BufferedQueue<T> _bufferedQueue;
        private readonly List<T> _cache = new List<T>();  // Przechowywanie danych w pamięci

        public event Action? CacheUpdated;

        public GenericRepository(SQLiteDatabase db, string tableName)
        {
            _db = db;
            _tableName = tableName;
            _bufferedQueue = new BufferedQueue<T>(entity =>
            {
                // Dodawanie/aktualizowanie w SQLite
                AddOrUpdateInternal(entity);
                UpdateCacheAsync();
                CacheUpdated?.Invoke();
            });
            RestoreFromBackup();
        }
        public async Task AddOrUpdate(T entity)
        {
            if (entity.Id == 0)
            {
                entity.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Ustawienie Id na podstawie czasu epoch w milisekundach
            }
            // Dodaj do kolejki zamiast bezpośrednio do bazy danych
            await _bufferedQueue.Enqueue(entity);
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
        // Metoda do aktualizacji cache
        public async Task UpdateCacheAsync()
        {
            var activeItems = await GetActiveAsync();
            _cache.Clear();
            _cache.AddRange(activeItems);
        }

        // Odczyt danych z cache
        public async Task<List<T>> GetFromCache()
        {
            return new List<T>(_cache);  // Zwracamy kopię listy, żeby uniknąć manipulacji z zewnątrz
        }

        // Pobieranie danych
        public async Task<List<T>> GetAllAsync(string whereClause = "")
        {
            string query = $"SELECT * FROM {_tableName} {whereClause} ORDER BY StartTime ASC";
            return await _db.ExecuteReaderAsync<T>(query);
        }
        public async Task<List<T>> GetActiveAsync()
        {
            string query = $"SELECT * FROM {_tableName} WHERE IsActive = 1 ORDER BY StartTime ASC";
            Log.Information("Wykonywanie zapytania: {Query}", query);
            var result = await _db.ExecuteReaderAsync<T>(query);
            Log.Information("Znaleziono rekordy: {Count}", result.Count);
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
            string backupFilePath = "backup.json";

            var allEntities = File.Exists(backupFilePath)
                ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(backupFilePath)) ?? new List<T>()
                : new List<T>();

            allEntities.Add(entity);

            File.WriteAllText(backupFilePath, JsonSerializer.Serialize(allEntities));
        }
        public void RestoreFromBackup()
        {
            string backupFilePath = "backup.json";
            if (!File.Exists(backupFilePath)) return;

            try
            {
                var allEntities = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(backupFilePath)) ?? new List<T>();

                foreach (var entity in allEntities)
                {
                    AddOrUpdateInternal(entity);
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
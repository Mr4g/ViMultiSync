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

namespace ViSyncMaster.Repositories
{
    public class GenericRepository<T> where T : IEntity, new()
    {
        private readonly SQLiteDatabase _db;
        private readonly string _tableName;

        public GenericRepository(SQLiteDatabase db, string tableName)
        {
            _db = db;
            _tableName = tableName;
        }

        // Dodawanie/aktualizowanie rekordu w bazie danych
        public void AddOrUpdate(T entity)
        {
            string query = $@"
            INSERT INTO {_tableName} 
            ({GetColumns(entity)}) 
            VALUES 
            ({GetColumnParameters(entity)})
            ON CONFLICT(Id) DO UPDATE SET
            {GetUpdateColumns(entity)}";

            _db.ExecuteNonQuery(query, entity);
        }

        // Pobieranie danych
        public List<T> GetAll(string whereClause = "")
        {
            string query = $"SELECT * FROM {_tableName} {whereClause} ORDER BY StartTime ASC";
            return _db.ExecuteReader<T>(query);
        }
        public List<T> GetActive()
        {
            string query = $"SELECT * FROM {_tableName} WHERE IsActive = 1 ORDER BY StartTime ASC";
            return _db.ExecuteReader<T>(query);
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
    }
}
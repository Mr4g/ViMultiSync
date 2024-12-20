using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;

namespace ViSyncMaster.Services
{
    public class SQLiteDatabase
    {
        private string _connectionString;

        public SQLiteDatabase(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        // Tworzenie tabeli dla dowolnego typu encji, jeśli nie istnieje
        public void CreateTableIfNotExists<T>(string tableName = null)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Ustal nazwę tabeli
                var finalTableName = tableName ?? typeof(T).Name;

                // Generuj kolumny
                var columns = string.Join(", ", GetColumnsForType<T>());

                // Zapytanie SQL
                var createTableQuery = $@"
                CREATE TABLE IF NOT EXISTS {finalTableName} (
                {columns}
                  )";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Pobranie kolumn z typu encji
        private IEnumerable<string> GetColumnsForType<T>()
        {
            return typeof(T).GetProperties().Select(p =>
            {
                var columnType = GetSQLiteColumnType(p.PropertyType);
                var isPrimaryKey = p.Name == "Id" ? " PRIMARY KEY" : ""; // Jeśli nazwa to Id, dodaj PRIMARY KEY
                return $"{p.Name} {columnType}{isPrimaryKey}";
            });
        }
        // Mapowanie typów C# na odpowiednie typy SQLite
        private string GetSQLiteColumnType(Type propertyType)
        {
            if (propertyType == typeof(int) || propertyType == typeof(long)) return "INTEGER";
            if (propertyType == typeof(string)) return "TEXT";
            if (propertyType == typeof(bool)) return "BOOLEAN";
            if (propertyType == typeof(DateTime)) return "DATETIME";
            return "TEXT"; // Default type
        }

        // Przykładowe wykonanie zapytania SQL
        public void ExecuteNonQuery(string query, object parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    AddParameters(command, parameters);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Pomocnicza metoda dodająca parametry do zapytania
        private void AddParameters(SQLiteCommand command, object parameters)
        {
            foreach (var property in parameters.GetType().GetProperties())
            {
                var value = property.GetValue(parameters);
                command.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
            }
        }

        // Wykonywanie zapytania SQL i mapowanie wyników na generyczny typ
        public List<T> ExecuteReader<T>(string query)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    var reader = command.ExecuteReader();
                    var result = new List<T>();
                    while (reader.Read())
                    {
                        var item = MapReaderToEntity<T>(reader);
                        result.Add(item);
                    }
                    return result;
                }
            }
        }

        // Mapowanie wyników zapytania na typ encji
        private T MapReaderToEntity<T>(SQLiteDataReader reader)
        {
            var entity = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var columnName = prop.Name;

                // Sprawdzamy, czy właściwość ma publiczny setter
                if (prop.CanWrite && !reader.IsDBNull(reader.GetOrdinal(columnName)))
                {
                    var value = reader[columnName];
                    prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
                }
            }

            return entity;
        }
    }
}

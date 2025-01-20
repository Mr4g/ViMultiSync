using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class SQLiteDatabase
    {
        private string _connectionString;

        public SQLiteDatabase(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            ConfigureWALMode().Wait();
        }

        // Create table for MachineStatus model if it doesn't exist
        public async Task CreateTableIfNotExists<T>(string tableName = null)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var finalTableName = tableName ?? typeof(T).Name;
                    var columns = string.Join(", ", GetColumnsForType<T>());
                    var createTableQuery = $@"
                        CREATE TABLE IF NOT EXISTS {finalTableName} (
                        {columns}
                        )";

                    using (var command = new SQLiteCommand(createTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        Log.Information("Table '{TableName}' created or already exists.", finalTableName);
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Log.Error(ex, "SQLite error while creating table: {TableName}", tableName);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while creating table: {TableName}", tableName);
                throw;
            }
        }

        // Generate columns for the specified model type
        private IEnumerable<string> GetColumnsForType<T>()
        {
            return typeof(T).GetProperties().Select(p =>
            {
                var columnType = GetSQLiteColumnType(p.PropertyType);
                var isPrimaryKey = p.Name == "Id" ? " PRIMARY KEY AUTOINCREMENT" : ""; // Use AUTOINCREMENT for Id
                return $"{p.Name} {columnType}{isPrimaryKey}";
            });
        }

        // Map C# types to SQLite types
        private string GetSQLiteColumnType(Type propertyType)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(int) || underlyingType == typeof(long)) return "INTEGER";
            if (underlyingType == typeof(string)) return "TEXT";
            if (underlyingType == typeof(bool)) return "BOOLEAN";
            if (underlyingType == typeof(DateTime)) return "DATETIME";

            return "TEXT"; // Default type
        }

        // Execute non-query SQL commands
        public async Task ExecuteNonQuery(string query, object parameters)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        AddParameters(command, parameters);
                        await command.ExecuteNonQueryAsync();
                        Log.Information("Executed query: {Query}", query);
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Log.Error(ex, "SQLite error during ExecuteNonQuery: {Query}", query);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during ExecuteNonQuery: {Query}", query);
                throw;
            }
        }

        // Execute a query and map results to the specified model type
        public async Task<List<T>> ExecuteReaderAsync<T>(string query)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var result = new List<T>();

                        while (await reader.ReadAsync())
                        {
                            var item = MapReaderToEntity<T>(reader);
                            result.Add(item);
                        }

                        Log.Information("Executed query and mapped results: {Query}", query);
                        return result;
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Log.Error(ex, "SQLite error during ExecuteReaderAsync: {Query}", query);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during ExecuteReaderAsync: {Query}", query);
                throw;
            }
        }

        // Enable WAL mode for better write performance
        private async Task ConfigureWALMode()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SQLiteCommand("PRAGMA journal_mode = WAL;", connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        Log.Information("SQLite database configured to use WAL mode.");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Log.Error(ex, "SQLite error while configuring WAL mode.");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while configuring WAL mode.");
                throw;
            }
        }

        // Add parameters to a query
        private void AddParameters(SQLiteCommand command, object parameters)
        {
            foreach (var property in parameters.GetType().GetProperties())
            {
                var value = property.GetValue(parameters);
                command.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
            }
        }

        // Map database rows to the specified entity type
        private T MapReaderToEntity<T>(DbDataReader reader)
        {
            var entity = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                // Sprawdzamy, czy właściwość ma publiczny setter
                if (!prop.CanWrite) continue;

                var columnName = prop.Name;

                // Pobieranie wartości z readera
                var value = reader[columnName];

                // Obsługuje Nullable<DateTime> (DateTime?)
                if (prop.PropertyType == typeof(DateTime?) && value is DBNull)
                {
                    prop.SetValue(entity, null); // Ustawiamy null, jeśli wartość jest DBNull
                }
                else if (prop.PropertyType == typeof(DateTime?) && value is DateTime dateTimeValue)
                {
                    prop.SetValue(entity, dateTimeValue);
                }
                // Obsługuje zwykły DateTime
                else if (prop.PropertyType == typeof(DateTime) && value is DateTime dateValue)
                {
                    prop.SetValue(entity, dateValue);
                }
                // Obsługuje inne typy (ogólna konwersja)
                else if (value != DBNull.Value)
                {
                    prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
                }
            }

            return entity;
        }



        // Check if a column exists in the reader
        private bool ColumnExists(IDataRecord reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
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
            EnforceFifoLimitForAllTables().Wait(); 
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

        public async Task<List<T>> ExecuteReaderAsync<T>(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        // Dodanie parametrów do zapytania, jeśli są
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

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
                if (!prop.CanWrite) continue;

                var columnName = prop.Name;
                if (!ColumnExists(reader, columnName)) continue;

                var value = reader[columnName];

                if (value == DBNull.Value)
                {
                    prop.SetValue(entity, null);
                }
                else if (prop.PropertyType == typeof(double))
                {
                    prop.SetValue(entity, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (prop.PropertyType == typeof(double?))
                {
                    prop.SetValue(entity, value == null ? (double?)null : Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                    prop.SetValue(entity, Convert.ChangeType(value, underlyingType));
                }
                else
                {
                    prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
                }
            }

            return entity;
        }

        private async Task EnforceFifoLimit(string tableName, int maxRecords = 100000)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 1️⃣ Sprawdź liczbę rekordów
                    using (var countCommand = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName};", connection))
                    {
                        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

                        if (count > maxRecords)
                        {
                            int recordsToDelete = count - maxRecords; // Tyle musimy usunąć

                            // 2️⃣ Pobierz ID ostatniego rekordu, który zostawiamy
                            var findThresholdQuery = $@"
                            SELECT ID FROM {tableName} 
                            ORDER BY ID ASC LIMIT {recordsToDelete};";

                            long idThreshold = 0;
                            using (var thresholdCommand = new SQLiteCommand(findThresholdQuery, connection))
                            using (var reader = await thresholdCommand.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    idThreshold = reader.GetInt64(0); // Najstarsze ID, które zostawiamy
                                }
                            }

                            if (idThreshold > 0)
                            {
                                // 3️⃣ Usuń wszystkie rekordy starsze niż znalezione ID
                                var deleteQuery = $"DELETE FROM {tableName} WHERE ID < {idThreshold};";

                                using (var deleteCommand = new SQLiteCommand(deleteQuery, connection))
                                {
                                    int deletedRows = await deleteCommand.ExecuteNonQueryAsync();
                                    Log.Information("FIFO enforced: removed {RecordsRemoved} old records from {TableName}.", deletedRows, tableName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while enforcing FIFO limit on table: {TableName}", tableName);
            }
        }

        private async Task EnforceFifoLimitForAllTables(int maxRecords = 100000)
        {
            var tableNames = await GetTableNames();

            foreach (var tableName in tableNames)
            {
                await EnforceFifoLimit(tableName, maxRecords);
            }
        }

        private async Task<List<string>> GetTableNames()
        {
            var tableNames = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tableNames.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while retrieving table names.");
            }

            return tableNames;
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

using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.Json;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class SQLiteDatabaseStatusesMachine
    {
        private string _connectionString;

        public SQLiteDatabaseStatusesMachine(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            CreateTableIfNotExists();
        }

        // Tworzenie tabeli MessageQueue, jeśli nie istnieje
        private void CreateTableIfNotExists()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS MachineStatuses (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Status TEXT,
                    Reason TEXT,
                    StartTime DATETIME,
                    EndTime DATETIME,
                    IsActive BOOLEAN,
                    Duration TEXT,
                    Color TEXT,
                    IsSent TEXT
                )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

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
                        var item = MapReaderToEntity<T>(reader); // Mapa wyników zapytania na obiekt T
                        result.Add(item);
                    }
                    return result;
                }
            }
        }
        private T MapReaderToEntity<T>(SQLiteDataReader reader)
        {
            if (typeof(T) == typeof(MachineStatus))
            {
                var machineStatus = new MachineStatus
                {
                    Id = Convert.ToInt64(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Status = reader["Status"].ToString(),
                    Reason = reader["Reason"].ToString(),
                    StartTime = DateTime.Parse(reader["StartTime"].ToString()),
                    EndTime = reader["EndTime"] != DBNull.Value ? (DateTime?)DateTime.Parse(reader["EndTime"].ToString()) : null,
                    Color = reader["Color"].ToString(),
                };
                return (T)(object)machineStatus;
            }

            throw new InvalidOperationException("Unsupported type");
        }

        private void AddParameters(SQLiteCommand command, object parameters)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
            }
        }     
    }
}

using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.Json;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class SQLiteDatabase
    {
        private string _connectionString;

        public SQLiteDatabase(string dbPath)
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
                CREATE TABLE IF NOT EXISTS MachineStatusQueue (
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

        public void AddMessageToQueue(MachineStatus machineStatus)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string upsertQuery = @"
                 INSERT INTO MachineStatusQueue 
                 (Id, Name, Status, Reason, StartTime, EndTime, IsActive, Duration, Color, IsSent) 
                 VALUES 
                 (@Id, @Name, @Status, @Reason, @StartTime, @EndTime, @IsActive, @Duration, @Color, 'Pending')
                 ON CONFLICT(Id) DO UPDATE SET
                     Name = excluded.Name,
                     Status = excluded.Status,
                     Reason = excluded.Reason,
                     StartTime = excluded.StartTime,
                     EndTime = excluded.EndTime,
                     IsActive = excluded.IsActive,
                     Duration = excluded.Duration,
                     Color = excluded.Color";

                using (var command = new SQLiteCommand(upsertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", machineStatus.Id);
                    command.Parameters.AddWithValue("@Name", machineStatus.Name);
                    command.Parameters.AddWithValue("@Status", machineStatus.Status);
                    command.Parameters.AddWithValue("@Reason", machineStatus.Reason);
                    command.Parameters.AddWithValue("@StartTime", machineStatus.StartTime);
                    command.Parameters.AddWithValue("@EndTime", machineStatus.EndTime.HasValue ? (object)machineStatus.EndTime.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IsActive", machineStatus.IsActive);
                    command.Parameters.AddWithValue("@Duration", machineStatus.Duration.HasValue ? machineStatus.Duration.Value.ToString() : DBNull.Value);
                    command.Parameters.AddWithValue("@Color", machineStatus.Color);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Pobieranie wiadomości do przetworzenia (status "Pending")
        public List<MachineStatus> GetPendingMessages()
        {
            List<MachineStatus> messages = new List<MachineStatus>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                // Zmienione zapytanie - teraz odczytujemy dane z odpowiednich kolumn
                string selectQuery = "SELECT Id, Name, Status, Reason, StartTime, EndTime, Color, IsSent " +
                                           "FROM MachineStatusQueue ORDER BY StartTime ASC"; // Sortowanie od najstarszego

                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Mapowanie danych z bazy danych na obiekt MachineStatus
                            var machineStatus = new MachineStatus
                            {
                                Id = Convert.ToInt64(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Status = reader["Status"].ToString(),
                                Reason = reader["Reason"].ToString(),
                                StartTime = DateTime.Parse(reader["StartTime"].ToString()), // Upewnij się, że StartTime jest poprawnym typem
                                EndTime = reader["EndTime"] != DBNull.Value ? (DateTime?)DateTime.Parse(reader["EndTime"].ToString()) : null,
                                Color = reader["Color"].ToString(),
                            };

                            messages.Add(machineStatus);
                        }
                    }
                }
            }
            return messages;
        }


        // Aktualizowanie statusu wiadomości na 'Processed'
        public void UpdateMessageStatus(long messageId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string updateQuery = "UPDATE MachineStatusQueue SET IsSent = 'Processed' WHERE Id = @Id";
                using (var command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", messageId);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Usuwanie wiadomości po jej pomyślnym przetworzeniu
        public void DeleteMessage(long messageId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM MachineStatusQueue WHERE Id = @Id AND IsSent = 'Processed'";
                using (var command = new SQLiteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", messageId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

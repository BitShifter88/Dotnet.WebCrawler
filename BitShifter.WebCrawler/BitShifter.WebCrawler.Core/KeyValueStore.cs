using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Core
{
    public class KeyValueStore
    {
        public int FlushSize { get; set; } = -1;

        private readonly string _connectionString;
        Dictionary<string, string> _buffer = new Dictionary<string, string>();
        

        public KeyValueStore(string dbPath, int flushSize)
        {
            FlushSize = flushSize;
            _connectionString = $"Data Source={dbPath};";

            SQLitePCL.Batteries.Init();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = @"
            CREATE TABLE IF NOT EXISTS KeyValuePairs (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )";

            using var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public void Set(string key, string value)
        {
            _buffer[key] = value;

            if (_buffer.Count > FlushSize)
            {
                BulkSet();
            }

            //using var connection = new SqliteConnection(_connectionString);
            //connection.Open();

            //string sql = @"
            //INSERT OR REPLACE INTO KeyValuePairs (Key, Value)
            //VALUES (@Key, @Value)";

            //using var command = new SqliteCommand(sql, connection);
            //command.Parameters.AddWithValue("@Key", key);
            //command.Parameters.AddWithValue("@Value", value);
            //command.ExecuteNonQuery();
        }

        public void Flush()
        {
            BulkSet();
        }

        public string GetByIndex(int index)
        {
            if (_buffer.Count > 0)
            {
                return _buffer.First().Key;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Select a record at a specific index using OFFSET
            string sql = "SELECT Key, Value FROM KeyValuePairs LIMIT 1 OFFSET @Index";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Index", index);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                string key = reader.GetString(0);
                string value = reader.GetString(1);
                return key;
            }

            return null; // Or handle this case as needed
        }

        public string Get(string key)
        {
            if (_buffer.ContainsKey(key))
            {
                return _buffer[key];
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "SELECT Value FROM KeyValuePairs WHERE Key = @Key";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Key", key);

            object result = command.ExecuteScalar();
            return result?.ToString();
        }

        public void Delete(string key)
        {
            if (_buffer.ContainsKey(key))
            {
                _buffer.Remove(key);
                return;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "DELETE FROM KeyValuePairs WHERE Key = @Key";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Key", key);
            command.ExecuteNonQuery();
        }

        public bool Contains(string key)
        {
            if (_buffer.ContainsKey(key))
                return true;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string sql = "SELECT COUNT(*) FROM KeyValuePairs WHERE Key = @Key";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Key", key);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        public List<string> GetAllKeys()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string sql = "SELECT Key FROM KeyValuePairs";
            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            var keys = new List<string>();
            while (reader.Read())
            {
                keys.Add(reader.GetString(0));
            }

            return keys;
        }

        private void BulkSet()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                string sql = @"
        INSERT OR REPLACE INTO KeyValuePairs (Key, Value)
        VALUES (@Key, @Value)";

                using var command = new SqliteCommand(sql, connection, transaction);

                foreach (var kvp in _buffer)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Key", kvp.Key);
                    command.Parameters.AddWithValue("@Value", kvp.Value);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            _buffer.Clear();
        }
    }
}

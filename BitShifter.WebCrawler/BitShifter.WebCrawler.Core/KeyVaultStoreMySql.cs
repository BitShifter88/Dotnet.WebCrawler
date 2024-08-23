using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitShifter.WebCrawler.Core
{
    public class KeyValueStoreMySql
    {
        public int FlushSize { get; set; } = -1;

        private readonly string _connectionString;
        HashSet<string> _buffer = new HashSet<string>();
        string _tableName;

        MySqlConnection connection;

        object _lock = new object();

        public KeyValueStoreMySql(string connectionString, int flushSize, string tableName)
        {
            _tableName = tableName;
            FlushSize = flushSize;
            _connectionString = connectionString;

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            connection = new MySqlConnection(_connectionString);
            connection.Open();

            string sql = $"CREATE TABLE IF NOT EXISTS {_tableName} (`Key` VARCHAR(500) PRIMARY KEY)";

            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public void Set(string key)
        {
            _buffer.Add(key);

            if (_buffer.Count > FlushSize)
            {
                BulkSet();
            }
        }

        public void Flush()
        {
            BulkSet();
        }

        public string GetByIndex(int index)
        {
            lock (_lock)
            {
                if (_buffer.Count > 0)
                {
                    return _buffer.ElementAt(index);
                }


                string sql = $"SELECT `Key` FROM {_tableName} LIMIT 1 OFFSET @Index";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Index", index);

                object result = command.ExecuteScalar();
                return result?.ToString();
            }
        }

        public bool Contains(string key)
        {
            lock (_lock)
            {
                if (_buffer.Contains(key))
                    return true;

                string sql = $"SELECT COUNT(*) FROM {_tableName} WHERE `Key` = @Key";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Key", key);
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }

        public void Delete(string key)
        {
            lock (_lock)
            {
                if (_buffer.Contains(key))
                {
                    _buffer.Remove(key);
                    return;
                }

                string sql = $"DELETE FROM {_tableName} WHERE `Key` = @Key";

                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Key", key);
                command.ExecuteNonQuery();
            }
        }

        public List<string> GetAllKeys()
        {
            lock (_lock)
            {
                string sql = $"SELECT `Key` FROM {_tableName}";
                using var command = new MySqlCommand(sql, connection);
                using var reader = command.ExecuteReader();

                var keys = new List<string>();
                while (reader.Read())
                {
                    keys.Add(reader.GetString(0));
                }

                return keys.Union(_buffer).ToList();
            }
        }

        private void BulkSet()
        {
            lock (_lock)
            {
                using var transaction = connection.BeginTransaction();

                try
                {
                    string sql = $"INSERT IGNORE INTO {_tableName} (`Key`) VALUES (@Key)";

                    using var command = new MySqlCommand(sql, connection, transaction);

                    foreach (var key in _buffer)
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Key", key);
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
}

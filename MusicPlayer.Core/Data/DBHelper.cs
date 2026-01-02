using System;
using Microsoft.Data.Sqlite;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 基础数据库操作类 - 负责数据库连接管理、事务和通用SQL执行
    /// </summary>
    public abstract class DBHelper : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection? _connection;
        private bool _disposed = false;

        protected DBHelper(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
        }

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        protected SqliteConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new SqliteConnection(_connectionString);
                }
                
                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }
                
                return _connection;
            }
        }

        /// <summary>
        /// 执行非查询SQL命令
        /// </summary>
        public int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
        {
            using var command = new SqliteCommand(sql, Connection);
            
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行非查询SQL命令（支持事务）
        /// </summary>
        public int ExecuteNonQuery(string sql, SqliteTransaction transaction, params SqliteParameter[] parameters)
        {
            using var command = new SqliteCommand(sql, Connection, transaction);
            
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行查询并返回单个值
        /// </summary>
        public T? ExecuteScalar<T>(string sql, params SqliteParameter[] parameters)
        {
            using var command = new SqliteCommand(sql, Connection);
            
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            
            var result = command.ExecuteScalar();
            return result == DBNull.Value ? default(T) : (T?)result;
        }

        /// <summary>
        /// 执行查询并返回单个值（支持事务）
        /// </summary>
        public T? ExecuteScalar<T>(string sql, SqliteTransaction transaction, params SqliteParameter[] parameters)
        {
            using var command = new SqliteCommand(sql, Connection, transaction);
            
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            
            var result = command.ExecuteScalar();
            return result == DBNull.Value ? default(T) : (T?)result;
        }

        /// <summary>
        /// 执行查询并返回读取器
        /// </summary>
        protected SqliteDataReader ExecuteReader(string sql, params SqliteParameter[] parameters)
        {
            var command = new SqliteCommand(sql, Connection);
            
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            
            return command.ExecuteReader();
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public SqliteTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
                _disposed = true;
            }
        }

        internal bool ExistsTable(string tableName) {
            string num = "";
            using var reader = ExecuteReader(@"SELECT count(*) as num  FROM sqlite_master WHERE type = 'table' AND name =  @TableName;",
                    new SqliteParameter("@TableName", tableName));
            while (reader.Read())
            {
                num = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            }
                return int.Parse(num)>0;
        }
        /// <summary>
        /// 析构函数
        /// </summary>
        ~DBHelper()
        {
            Dispose(false);
        }
    }
}
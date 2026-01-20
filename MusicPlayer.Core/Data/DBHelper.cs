using LiteDB;
using MusicPlayer.Core.Models;
using System;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 基础数据库操作类 - 负责数据库连接管理和通用操作
    /// </summary>
    public abstract class DBHelper : IDisposable
    {
        private readonly LiteDatabase _database;
        private bool _disposed = false;

        protected DBHelper(string databasePath)
        {
           
            // 使用共享模式连接，允许多个读取者和一个写入者
            var connectionString = new ConnectionString(databasePath)
            {
                Connection = ConnectionType.Shared
            };
            
            _database = new LiteDatabase(connectionString);
        }

        /// <summary>
        /// 获取数据库实例
        /// </summary>
        protected LiteDatabase Database
        {
            get { return _database; }
        }

        /// <summary>
        /// 获取集合
        /// </summary>
        protected ILiteCollection<T> GetCollection<T>(string collectionName = null)
        {
          
            return _database.GetCollection<T>(collectionName);
        }

        protected bool DeleteTable(string name) {
            bool isDropped = _database.DropCollection(name);
            return isDropped;
        }
        /// <summary>
        /// 检查集合是否存在
        /// </summary>
        internal bool ExistsTable(string collectionName) {
            return _database.CollectionExists(collectionName);
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
                _database?.Dispose();
                _disposed = true;
            }
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
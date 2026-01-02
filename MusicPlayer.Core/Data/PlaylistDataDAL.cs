using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 播放列表数据服务 - 负责对Songs表的增删改查操作
    /// </summary>
    public class PlaylistDataDAL : DBHelper
    {
        public PlaylistDataDAL(string databasePath) : base(databasePath)
        { 
        }

         
       
        /// <summary>
        /// 创建歌曲列表
        /// </summary>
        private void CreateSongS() {
            var createSongsTable = @"CREATE TABLE IF NOT EXISTS Songs (
                Id INTEGER PRIMARY KEY, 
                FilePath TEXT NOT NULL UNIQUE,
                Title TEXT,
                Artist TEXT,
                Album TEXT,
                Duration INTEGER,
                FileSize INTEGER,
                Heart INTEGER DEFAULT 0,
                IsDeleted INTEGER DEFAULT 0,
                AddedTime TEXT NOT NULL
            )";
            ExecuteNonQuery(createSongsTable); 
        }

        /// <summary>
        /// 检查播放列表是否存在，不存在则创建
        /// </summary>
        

         
       
        /// <summary>
        /// 批量插入或更新歌曲 - 使用INSERT OR REPLACE简化实现
        /// </summary>
        public int InsertSongs(  List<Song> songs, SqliteTransaction? externalTransaction = null)
        {
            if (songs.Count == 0) return 0;
            
            // 使用外部事务或创建新事务
            SqliteTransaction? transaction = externalTransaction;
            bool shouldCommit = false;
            
            if (transaction == null)
            {
                transaction = BeginTransaction();
                shouldCommit = true;
            }
            
            int processedCount = 0;
            
            try
            {
                foreach (var song in songs)
                {
                    // 使用INSERT OR REPLACE语句，如果记录存在则替换，不存在则插入
                    ExecuteNonQuery(@"
                        INSERT  INTO Songs 
                        ( Id, FilePath, Title, Artist, Album, Duration, FileSize, Heart, IsDeleted, AddedTime)
                        VALUES ( @Id, @FilePath, @Title, @Artist, @Album, @Duration, @FileSize, @Heart, @IsDeleted, @AddedTime)",
                        transaction,
                        
                        new SqliteParameter("@Id", song.Id),
                        new SqliteParameter("@FilePath", song.FilePath),
                        new SqliteParameter("@Title", song.Title),
                        new SqliteParameter("@Artist", song.Artist),
                        new SqliteParameter("@Album", song.Album),
                        new SqliteParameter("@Duration", (long)song.Duration.TotalSeconds),
                        new SqliteParameter("@FileSize", song.FileSize),
                        new SqliteParameter("@Heart", song.Heart ? 1 : 0),
                        new SqliteParameter("@IsDeleted", song.IsDeleted ? 1 : 0),
                        new SqliteParameter("@AddedTime", song.AddedTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
                    
                    processedCount++;
                }
                
                // 只有当是我们创建的事务时才提交
                if (shouldCommit)
                {
                    transaction.Commit();
                }
                
                return processedCount;
            }
            catch (Exception ex)
            {
                // 只有当是我们创建的事务时才回滚
                if (shouldCommit)
                {
                    transaction?.Rollback();
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 批量插入或更新歌曲失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从数据库加载所有歌曲
        /// </summary>
        public List<Song> LoadAllSongs( )
        {
            var songs = new List<Song>();
            if (!ExistsTable("Songs")) { CreateSongS(); }
            try
            {
                using var reader = ExecuteReader(@"
                    SELECT Id, FilePath, Title, Artist, Album, Duration, FileSize, Heart, IsDeleted, AddedTime
                    FROM Songs
                    WHERE  1 = 1
                    ORDER BY AddedTime" );
                
                while (reader.Read())
                {
                    var song = new Song
                    {
                        Id = reader.IsDBNull(0) ? -1 : reader.GetInt32(0),
                        FilePath = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Artist = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Album = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Duration = TimeSpan.FromSeconds(reader.IsDBNull(5) ? 0 : reader.GetInt64(5)),
                        FileSize = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                        Heart = !reader.IsDBNull(7) && reader.GetInt32(7) == 1,
                        IsDeleted = !reader.IsDBNull(8) && reader.GetInt32(8) == 1,
                        AddedTime = reader.IsDBNull(9) ? DateTime.Now : DateTime.Parse(reader.GetString(9)),
                        DelayAlbumArtLoading = true // 设置延迟加载标志
                    };
                    
                    songs.Add(song);
                }
                
                reader.Close();
                return songs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 加载歌曲失败: {ex.Message}");
                return new List<Song>();
            }
        }

        /// <summary>
        /// 清空播放列表中的所有歌曲
        /// </summary>
        public void ClearPlaylist(  SqliteTransaction? transaction = null)
        {
            try
            {
                
                    ExecuteNonQuery("DELETE FROM Songs WHERE 1 =1", transaction  );
                 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 清空播放列表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据文件路径获取歌曲信息
        /// </summary>
        public Song? GetSongByFilePath(string filePath)
        {
            try
            {
                using var reader = ExecuteReader(@"
                    SELECT FilePath, Title, Artist, Album, Duration, FileSize, Heart, IsDeleted, AddedTime
                    FROM Songs
                    WHERE FilePath = @FilePath",
                    new SqliteParameter("@FilePath", filePath));
                
                if (reader.Read())
                {
                    var song = new Song
                    {
                        FilePath = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Artist = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Album = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Duration = TimeSpan.FromSeconds(reader.IsDBNull(4) ? 0 : reader.GetInt64(4)),
                        FileSize = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                        Heart = !reader.IsDBNull(6) && reader.GetInt32(6) == 1,
                        IsDeleted = !reader.IsDBNull(7) && reader.GetInt32(7) == 1,
                        AddedTime = reader.IsDBNull(8) ? DateTime.Now : DateTime.Parse(reader.GetString(8)),
                        DelayAlbumArtLoading = true
                    };
                    
                    reader.Close();
                    return song;
                }
                
                reader.Close();
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 根据文件路径获取歌曲失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查歌曲是否存在于数据库中
        /// </summary>
        public bool ContainsSong(string filePath)
        {
            try
            {
                var count = ExecuteScalar<long>("SELECT COUNT(*) FROM Songs WHERE FilePath = @FilePath",
                    new SqliteParameter("@FilePath", filePath));
                
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 检查歌曲是否存在失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 更新单个歌曲的状态（收藏状态、删除状态等）
        /// </summary>
        public int UpdateSong(Song song, SqliteTransaction? externalTransaction = null)
        {
            if (song == null) return 0;
            
            // 使用外部事务或创建新事务
            SqliteTransaction? transaction = externalTransaction;
            bool shouldCommit = false;
            
            if (transaction == null)
            {
                transaction = BeginTransaction();
                shouldCommit = true;
            }
            
            try
            {
                // 只更新状态相关字段，其他字段保持不变
                int rowsAffected = ExecuteNonQuery(@"
                    UPDATE Songs 
                    SET Heart = @Heart, IsDeleted = @IsDeleted
                    WHERE FilePath = @FilePath",
                    transaction,
                    new SqliteParameter("@FilePath", song.FilePath),
                    new SqliteParameter("@Heart", song.Heart ? 1 : 0),
                    new SqliteParameter("@IsDeleted", song.IsDeleted ? 1 : 0));
                
                // 只有当是我们创建的事务时才提交
                if (shouldCommit)
                {
                    transaction.Commit();
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 更新歌曲状态: {song.Title}, Heart: {song.Heart}, IsDeleted: {song.IsDeleted}");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                // 只有当是我们创建的事务时才回滚
                if (shouldCommit)
                {
                    transaction?.Rollback();
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 更新歌曲状态失败: {ex.Message}");
                throw;
            }
        }
    }
}
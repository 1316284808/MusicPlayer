using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using LiteDB;
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
        /// 批量插入或更新歌曲
        /// </summary>
        public int InsertSongs(List<Song> songs)
        {
            if (songs.Count == 0) return 0;
            
            var collection = GetCollection<Song>("Songs");
            int processedCount = 0;
            
            try
            {
                foreach (var song in songs)
                {
                    // 使用Upsert方法，如果记录存在则更新，不存在则插入
                    collection.Upsert(song);
                    processedCount++;
                }
                
                return processedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 批量插入或更新歌曲失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从数据库加载所有歌曲
        /// </summary>
        public List<Song> LoadAllSongs()
        {
            var collection = GetCollection<Song>("Songs");
            try
            {
                return collection.FindAll().OrderBy(x => x.AddedTime).ToList();
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
        public void ClearPlaylist()
        {
            try
            {
                var collection = GetCollection<Song>("Songs");
                collection.DeleteAll();
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
                var collection = GetCollection<Song>("Songs");
                return collection.FindOne(x => x.FilePath == filePath);
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
                var collection = GetCollection<Song>("Songs");
                return collection.Exists(x => x.FilePath == filePath);
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
        public int UpdateSong(Song song)
        {
            if (song == null) return 0;
            
            try
            {
                var collection = GetCollection<Song>("Songs");
                bool updated = collection.Update(song);
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 更新歌曲状态: {song.Title}, Heart: {song.Heart}, IsDeleted: {song.IsDeleted}");
                return updated ? 1 : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 更新歌曲状态失败: {ex.Message}");
                throw;
            }
        }
    }
}
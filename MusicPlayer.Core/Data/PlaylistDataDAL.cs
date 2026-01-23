using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using LiteDB;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 播放列表数据服务 - 负责对Songs表、Playlists表和PlaylistSongs表的增删改查操作
    /// </summary>
    public class PlaylistDataDAL : DBHelper
    {
        public PlaylistDataDAL(string databasePath) : base(databasePath)
        { 
        }

        #region 歌曲管理方法（原有）

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
                    try
                    {
                        collection.Upsert(song);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        processedCount--;
                        System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 插入或更新歌曲失败: {song.Title}, 错误信息: {ex.Message}");
                    }
                }
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 批量插入或更新歌曲失败: {ex.Message}");
                throw;
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: {processedCount}");
               }
            return processedCount;
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
                 
                bool isDropped = DeleteTable("Playlists");   
                bool isDropped1 = DeleteTable("PlaylistSongs"); 
                bool isDropped2 = DeleteTable("Songs");
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

        #endregion

        #region 播放列表管理方法

        /// <summary>
        /// 创建播放列表
        /// </summary>
        public int InsertPlaylist(Playlist playlist)
        {
            try
            {
                var collection = GetCollection<Playlist>("Playlists");
                
                // 确保创建时间和更新时间正确
                if (playlist.CreatedTime == DateTime.MinValue)
                    playlist.CreatedTime = DateTime.Now;
                playlist.UpdatedTime = DateTime.Now;
                
                collection.Upsert(playlist);
                return 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 创建播放列表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新播放列表
        /// </summary>
        public int UpdatePlaylist(Playlist playlist)
        {
            try
            {
                var collection = GetCollection<Playlist>("Playlists");
                
                // 更新时间
                playlist.UpdatedTime = DateTime.Now;
                
                bool updated = collection.Update(playlist);
                return updated ? 1 : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 更新播放列表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除播放列表
        /// </summary>
        public int DeletePlaylist(int playlistId)
        {
            try
            {
                // 先删除关联数据
                var playlistSongCollection = GetCollection<PlaylistSong>("PlaylistSongs");
                // 使用Find查询获取需要删除的文档，然后逐个删除
                var playlistSongs = playlistSongCollection.Find(x => x.PlaylistId == playlistId).ToList();
                foreach (var playlistSong in playlistSongs)
                {
                    playlistSongCollection.Delete(playlistSong.Id);
                }
                
                // 再删除播放列表
                var collection = GetCollection<Playlist>("Playlists");
                bool deleted = collection.Delete(playlistId);
                return deleted ? 1 : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 删除播放列表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取播放列表详情
        /// </summary>
        public Playlist? GetPlaylistById(int playlistId)
        {
            try
            {
                var collection = GetCollection<Playlist>("Playlists");
                return collection.FindOne(x => x.Id == playlistId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 根据ID获取播放列表失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有播放列表
        /// </summary>
        public List<Playlist> GetAllPlaylists()
        {
            try
            {
                var collection = GetCollection<Playlist>("Playlists");
                return collection.FindAll().OrderBy(x => x.CreatedTime).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 获取所有播放列表失败: {ex.Message}");
                return new List<Playlist>();
            }
        }

        /// <summary>
        /// 获取默认播放列表
        /// </summary>
        public Playlist? GetDefaultPlaylist()
        {
            try
            {
                var collection = GetCollection<Playlist>("Playlists");
                return collection.FindOne(x => x.IsDefault == true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 获取默认播放列表失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 播放列表歌曲管理方法

        /// <summary>
        /// 添加歌曲到播放列表
        /// </summary>
        public int AddSongToPlaylist(int playlistId, int songId)
        {
            try
            {
                var collection = GetCollection<PlaylistSong>("PlaylistSongs");
                
                // 检查歌曲是否已在播放列表中
                if (IsSongInPlaylist(playlistId, songId))
                    return 0;
                
                // 获取当前最大顺序值
                int maxOrder = collection.Find(x => x.PlaylistId == playlistId)
                    .Select(x => x.Order)
                    .DefaultIfEmpty(0)
                    .Max();
                
                var playlistSong = new PlaylistSong
                {
                    PlaylistId = playlistId,
                    SongId = songId,
                    Order = maxOrder + 1,
                    AddedTime = DateTime.Now
                };
                
                collection.Upsert(playlistSong);
                return 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 添加歌曲到播放列表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从播放列表移除歌曲
        /// </summary>
        public int RemoveSongFromPlaylist(int playlistId, int songId)
        {
            try
            {
                var collection = GetCollection<PlaylistSong>("PlaylistSongs");
                // 使用Find查询获取需要删除的文档，然后逐个删除
                var playlistSongs = collection.Find(x => x.PlaylistId == playlistId && x.SongId == songId).ToList();
                int deletedCount = 0;
                foreach (var playlistSong in playlistSongs)
                {
                    if (collection.Delete(playlistSong.Id))
                    {
                        deletedCount++;
                    }
                }
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 从播放列表移除歌曲失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取播放列表中的所有歌曲
        /// </summary>
        public List<Song> GetSongsByPlaylistId(int playlistId)
        {
            try
            {
                // 如果是默认播放列表，返回所有歌曲
                var playlist = GetPlaylistById(playlistId);
                if (playlist?.IsDefault == true)
                {
                    return LoadAllSongs();
                }
                
                // 否则根据关联表获取歌曲
                var playlistSongCollection = GetCollection<PlaylistSong>("PlaylistSongs");
                var songCollection = GetCollection<Song>("Songs");
                
                // 获取播放列表中的歌曲ID列表，按顺序排序
                var songIds = playlistSongCollection.Find(x => x.PlaylistId == playlistId)
                    .OrderBy(x => x.Order)
                    .Select(x => x.SongId)
                    .ToList();
                
                // 如果没有歌曲，返回空列表
                if (songIds.Count == 0)
                    return new List<Song>();
                
                // 获取歌曲详情
                return songCollection.Find(x => songIds.Contains(x.Id))
                    .OrderBy(song => songIds.IndexOf(song.Id))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 获取播放列表中的所有歌曲失败: {ex.Message}");
                return new List<Song>();
            }
        }

        /// <summary>
        /// 清空播放列表中的歌曲
        /// </summary>
        public int ClearPlaylistSongs(int playlistId)
        {
            try
            {
                var collection = GetCollection<PlaylistSong>("PlaylistSongs");
                // 使用Find查询获取需要删除的文档，然后逐个删除
                var playlistSongs = collection.Find(x => x.PlaylistId == playlistId).ToList();
                int deletedCount = 0;
                foreach (var playlistSong in playlistSongs)
                {
                    if (collection.Delete(playlistSong.Id))
                    {
                        deletedCount++;
                    }
                }
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 清空播放列表中的歌曲失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 检查歌曲是否在播放列表中
        /// </summary>
        public bool IsSongInPlaylist(int playlistId, int songId)
        {
            try
            {
                var collection = GetCollection<PlaylistSong>("PlaylistSongs");
                return collection.Exists(x => x.PlaylistId == playlistId && x.SongId == songId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataDAL: 检查歌曲是否在播放列表中失败: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
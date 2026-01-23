using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 自定义歌单服务实现
    /// 负责处理所有歌单管理相关的功能
    /// </summary>
    public class CustomPlaylistService : ICustomPlaylistService
    {
        private readonly PlaylistDataDAL _playlistDataDAL;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="playlistDataDAL">歌单数据访问层</param>
        public CustomPlaylistService(PlaylistDataDAL playlistDataDAL)
        {
            _playlistDataDAL = playlistDataDAL ?? throw new ArgumentNullException(nameof(playlistDataDAL));
        }
        
        #region 歌单管理实现
        
        /// <summary>
        /// 获取所有歌单
        /// </summary>
        public Task<List<Playlist>> GetAllPlaylistsAsync()
        {
            return Task.Run(() =>
            {
                return _playlistDataDAL.GetAllPlaylists();
            });
        }
        
        /// <summary>
        /// 根据ID获取歌单
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        public Task<Playlist?> GetPlaylistByIdAsync(int playlistId)
        {
            return Task.Run(() =>
            {
                return _playlistDataDAL.GetPlaylistById(playlistId);
            });
        }
        
        /// <summary>
        /// 创建歌单
        /// </summary>
        /// <param name="playlist">歌单对象</param>
        public Task<Playlist> CreatePlaylistAsync(Playlist playlist)
        {
            return Task.Run(() =>
            {
                _playlistDataDAL.InsertPlaylist(playlist);
                return playlist;
            });
        }
        
        /// <summary>
        /// 更新歌单
        /// </summary>
        /// <param name="playlist">歌单对象</param>
        public Task<bool> UpdatePlaylistAsync(Playlist playlist)
        {
            return Task.Run(() =>
            {
                int result = _playlistDataDAL.UpdatePlaylist(playlist);
                return result > 0;
            });
        }
        
        /// <summary>
        /// 删除歌单
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        public Task<bool> DeletePlaylistAsync(int playlistId)
        {
            return Task.Run(() =>
            {
                int result = _playlistDataDAL.DeletePlaylist(playlistId);
                return result > 0;
            });
        }
        
        #endregion
        
        #region 歌单歌曲管理实现
        
        /// <summary>
        /// 添加歌曲到歌单
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        /// <param name="songId">歌曲ID</param>
        public Task<bool> AddSongToPlaylistAsync(int playlistId, int songId)
        {
            return Task.Run(() =>
            {
                int result = _playlistDataDAL.AddSongToPlaylist(playlistId, songId);
                return result > 0;
            });
        }
        
        /// <summary>
        /// 从歌单移除歌曲
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        /// <param name="songId">歌曲ID</param>
        public Task<bool> RemoveSongFromPlaylistAsync(int playlistId, int songId)
        {
            return Task.Run(() =>
            {
                int result = _playlistDataDAL.RemoveSongFromPlaylist(playlistId, songId);
                return result > 0;
            });
        }
        
        /// <summary>
        /// 获取歌单中的所有歌曲
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        public Task<List<Song>> GetSongsByPlaylistIdAsync(int playlistId)
        {
            return Task.Run(() =>
            {
                return _playlistDataDAL.GetSongsByPlaylistId(playlistId);
            });
        }
        
        /// <summary>
        /// 检查歌曲是否在歌单中
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        /// <param name="songId">歌曲ID</param>
        public Task<bool> IsSongInPlaylistAsync(int playlistId, int songId)
        {
            return Task.Run(() =>
            {
                return _playlistDataDAL.IsSongInPlaylist(playlistId, songId);
            });
        }
        
        /// <summary>
        /// 清空歌单中的所有歌曲
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        public Task<int> ClearPlaylistSongsAsync(int playlistId)
        {
            return Task.Run(() =>
            {
                return _playlistDataDAL.ClearPlaylistSongs(playlistId);
            });
        }
        
        #endregion
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 此处不需要释放资源，因为PlaylistDataDAL的生命周期由依赖注入容器管理
        }
    }
}
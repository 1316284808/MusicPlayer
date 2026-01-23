using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 自定义歌单服务接口
    /// 负责处理所有歌单管理相关的功能
    /// </summary>
    public interface ICustomPlaylistService : IDisposable
    {
        #region 歌单管理
        
        /// <summary>
        /// 获取所有歌单
        /// </summary>
        Task<List<Playlist>> GetAllPlaylistsAsync();
        
        /// <summary>
        /// 根据ID获取歌单
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        Task<Playlist?> GetPlaylistByIdAsync(int playlistId);
        
        /// <summary>
        /// 创建歌单
        /// </summary>
        /// <param name="playlist">歌单对象</param>
        Task<Playlist> CreatePlaylistAsync(Playlist playlist);
        
        /// <summary>
        /// 更新歌单
        /// </summary>
        /// <param name="playlist">歌单对象</param>
        Task<bool> UpdatePlaylistAsync(Playlist playlist);
        
        /// <summary>
        /// 删除歌单
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        Task<bool> DeletePlaylistAsync(int playlistId);
        
        #endregion
        
        #region 歌单歌曲管理
        
        /// <summary>
        /// 添加歌曲到歌单
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        /// <param name="songId">歌曲ID</param>
        Task<bool> AddSongToPlaylistAsync(int playlistId, int songId);
        
        /// <summary>
        /// 从歌单移除歌曲
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        /// <param name="songId">歌曲ID</param>
        Task<bool> RemoveSongFromPlaylistAsync(int playlistId, int songId);
        
        /// <summary>
        /// 获取歌单中的所有歌曲
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        Task<List<Song>> GetSongsByPlaylistIdAsync(int playlistId);
        
        /// <summary>
        /// 检查歌曲是否在歌单中
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        /// <param name="songId">歌曲ID</param>
        Task<bool> IsSongInPlaylistAsync(int playlistId, int songId);
        
        /// <summary>
        /// 清空歌单中的所有歌曲
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        Task<int> ClearPlaylistSongsAsync(int playlistId);
        
        #endregion
    }
}
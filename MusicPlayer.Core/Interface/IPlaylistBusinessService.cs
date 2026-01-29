using System.Collections.Generic;
using System.Threading.Tasks;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放列表业务服务接口
    /// 职责：处理播放列表的业务逻辑（排序、过滤、添加/删除歌曲、更新状态等）
    /// </summary>
    public interface IPlaylistBusinessService
    {
        /// <summary>
        /// 设置排序规则并重新加载
        /// </summary>
        /// <param name="sortRule">排序规则</param>
        Task SetSortRuleAsync(SortRule sortRule);

        /// <summary>
        /// 应用过滤器
        /// </summary>
        /// <param name="filterText">过滤文本</param>
        void ApplyFilter(string filterText);

        /// <summary>
        /// 移除歌曲
        /// </summary>
        /// <param name="song">要移除的歌曲</param>
        void RemoveSong(Song song);

        /// <summary>
        /// 清空播放列表
        /// </summary>
        Task ClearPlaylistAsync();

        /// <summary>
        /// 添加歌曲
        /// </summary>
        /// <param name="songs">要添加的歌曲</param>
        Task AddSongsAsync(IEnumerable<Song> songs);

        /// <summary>
        /// 更新歌曲收藏状态
        /// </summary>
        /// <param name="song">要更新的歌曲</param>
        /// <param name="isFavorite">收藏状态</param>
        Task UpdateSongFavoriteStatusAsync(Song song, bool isFavorite);

        /// <summary>
        /// 更新歌曲删除状态
        /// </summary>
        /// <param name="song">要更新的歌曲</param>
        /// <param name="isDeleted">删除状态</param>
        Task UpdateSongDeletionStatusAsync(Song song, bool isDeleted);
    }
}
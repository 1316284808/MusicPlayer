using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放列表数据服务接口 - 唯一数据源管理器
    /// 职责：管理整个应用程序的唯一播放列表数据源
    /// </summary>
    public interface IPlaylistDataService : IDisposable
    {
        /// <summary>
        /// 唯一数据源 - 整个程序的播放列表真理
        /// 注意：使用后应立即调用ClearDataSource()清空临时缓存
        /// </summary>
        List<Song> DataSource { get; }

        /// <summary>
        /// 清除临时数据缓存
        /// </summary>
        void ClearDataSource();

        /// <summary>
        /// 当前排序规则
        /// </summary>
        SortRule CurrentSortRule { get; set; }

        /// <summary>
        /// 当前播放歌曲
        /// </summary>
        Song? CurrentSong { get; set; }

        /// <summary>
        /// 从JSON文件加载数据
        /// </summary>
        Task LoadFromDataAsync();

        /// <summary>
        /// 保存到JSON并重新加载
        /// </summary>
        /// <param name="sortRule">排序规则，为null则使用当前规则</param>
        Task SaveAndReloadAsync(SortRule? sortRule = null);

        /// <summary>
        /// 添加歌曲并重新加载
        /// </summary>
        /// <param name="songs">要添加的歌曲</param>
        Task AddSongsAndReloadAsync(IEnumerable<Song> songs);

        /// <summary>
        /// 清空播放列表并重新加载
        /// </summary>
        Task ClearAndReloadAsync();

        /// <summary>
        /// 获取下一首歌曲
        /// </summary>
        Song? GetNextSong(PlayMode playMode = PlayMode.RepeatAll);

        /// <summary>
        /// 获取上一首歌曲
        /// </summary>
        Song? GetPreviousSong(PlayMode playMode = PlayMode.RepeatAll);

        /// <summary>
        /// 设置当前歌曲
        /// </summary>
        void SetCurrentSong(Song? song);

        /// <summary>
        /// 获取歌曲在列表中的索引
        /// </summary>
        int GetSongIndex(Song song);

        /// <summary>
        /// 设置排序规则
        /// </summary>
        void SetSortRule(SortRule sortRule);

        /// <summary>
        /// 应用过滤器
        /// </summary>
        void ApplyFilter(string filterText);

        /// <summary>
        /// 移除歌曲
        /// </summary>
        void RemoveSong(Song song);

        /// <summary>
        /// 清空播放列表
        /// </summary>
        void ClearPlaylist();

        /// <summary>
        /// 添加歌曲（直接添加，不重新加载）
        /// </summary>
        void AddSongs(IEnumerable<Song> songs);

        /// <summary>
        /// 更新歌曲收藏状态
        /// </summary>
        /// <param name="song">要更新的歌曲</param>
        /// <param name="isFavorite">收藏状态</param>
        void UpdateSongFavoriteStatus(Song song, bool isFavorite);

        /// <summary>
        /// 更新歌曲删除状态
        /// </summary>
        /// <param name="song">要更新的歌曲</param>
        /// <param name="isDeleted">删除状态</param>
        void UpdateSongDeletionStatus(Song song, bool isDeleted);
    }


}
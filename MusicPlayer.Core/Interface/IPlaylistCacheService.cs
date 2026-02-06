using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface;

/// <summary>
/// 播放列表缓存服务接口
/// 提供统一的播放列表数据缓存管理，确保应用程序中只有一份数据源
/// </summary>
public interface IPlaylistCacheService
{
    /// <summary>
    /// 初始化缓存，从JSON文件加载播放列表到内存缓存
    /// </summary>
    /// <returns>初始化是否成功</returns>
    Task<bool> InitializeCacheAsync();

    /// <summary>
    /// 获取当前播放列表
    /// </summary>
    /// <returns>播放列表数据</returns>
    List<Song> GetPlaylist();

    /// <summary>
    /// 更新播放列表到缓存
    /// </summary>
    /// <param name="songs">新的播放列表数据</param>
    void UpdatePlaylist(List<Song> songs);

    /// <summary>
    /// 清空缓存
    /// </summary>
    void ClearCache();

    /// <summary>
    /// 缓存是否已准备就绪
    /// </summary>
    bool IsCacheReady { get; }

    /// <summary>
    /// 获取缓存中的歌曲数量
    /// </summary>
    int SongCount { get; }

    /// <summary>
    /// 根据文件路径获取歌曲
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>匹配的歌曲，找不到则返回null</returns>
    Song? GetSongByFilePath(string filePath);

    /// <summary>
    /// 检查歌曲是否存在于缓存中
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>歌曲是否存在</returns>
    bool ContainsSong(string filePath);

    /// <summary>
    /// 添加新歌曲到缓存
    /// </summary>
    /// <param name="songs">要添加的歌曲列表</param>
    /// <returns>实际添加的歌曲数量</returns>
    int AddSongs(IEnumerable<Song> songs);

    /// <summary>
    /// 从缓存中移除歌曲
    /// </summary>
    /// <param name="filePaths">要移除的歌曲文件路径列表</param>
    /// <returns>实际移除的歌曲数量</returns>
    int RemoveSongs(IEnumerable<string> filePaths);

    /// <summary>
    /// 根据当前排序规则获取排序后的播放列表
    /// </summary>
    /// <param name="sortRule">排序规则</param>
    /// <returns>排序后的播放列表</returns>
    List<Song> GetSortedPlaylist(SortRule sortRule);
    
    /// <summary>
    /// 保存播放列表到文件
    /// </summary>
    /// <param name="playlist">要保存的播放列表</param>
    /// <param name="sortRule">排序规则</param>
    void SavePlaylist(List<Song> playlist, SortRule sortRule);
    
    /// <summary>
    /// 异步更新单个歌曲状态到数据库
    /// </summary>
    /// <param name="song">要更新的歌曲</param>
    /// <returns>异步任务</returns>
    Task UpdateSongStatusInDatabaseAsync(Song song);

    #region 播放列表管理方法

    /// <summary>
    /// 创建播放列表
    /// </summary>
    /// <param name="playlist">要创建的播放列表</param>
    /// <returns>创建的播放列表ID</returns>
    Task<int> InsertPlaylistAsync(Playlist playlist);

    /// <summary>
    /// 更新播放列表
    /// </summary>
    /// <param name="playlist">要更新的播放列表</param>
    /// <returns>更新是否成功</returns>
    Task<int> UpdatePlaylistAsync(Playlist playlist);

    /// <summary>
    /// 删除播放列表
    /// </summary>
    /// <param name="playlistId">要删除的播放列表ID</param>
    /// <returns>删除是否成功</returns>
    Task<int> DeletePlaylistAsync(int playlistId);

    /// <summary>
    /// 根据ID获取播放列表详情
    /// </summary>
    /// <param name="playlistId">播放列表ID</param>
    /// <returns>播放列表详情</returns>
    Task<Playlist?> GetPlaylistByIdAsync(int playlistId);

    /// <summary>
    /// 获取所有播放列表
    /// </summary>
    /// <returns>所有播放列表</returns>
    Task<List<Playlist>> GetAllPlaylistsAsync();

    /// <summary>
    /// 获取默认播放列表
    /// </summary>
    /// <returns>默认播放列表</returns>
    Task<Playlist?> GetDefaultPlaylistAsync();

    #endregion

    #region 播放列表歌曲管理方法

    /// <summary>
    /// 添加歌曲到播放列表
    /// </summary>
    /// <param name="playlistId">播放列表ID</param>
    /// <param name="songId">歌曲ID</param>
    /// <returns>添加是否成功</returns>
    Task<int> AddSongToPlaylistAsync(int playlistId, int songId);

    /// <summary>
    /// 从播放列表移除歌曲
    /// </summary>
    /// <param name="playlistId">播放列表ID</param>
    /// <param name="songId">歌曲ID</param>
    /// <returns>移除是否成功</returns>
    Task<int> RemoveSongFromPlaylistAsync(int playlistId, int songId);

    /// <summary>
    /// 获取播放列表中的所有歌曲
    /// </summary>
    /// <param name="playlistId">播放列表ID</param>
    /// <returns>播放列表中的歌曲</returns>
    Task<List<Song>> GetSongsByPlaylistIdAsync(int playlistId);

    /// <summary>
    /// 清空播放列表中的歌曲
    /// </summary>
    /// <param name="playlistId">播放列表ID</param>
    /// <returns>清空的歌曲数量</returns>
    Task<int> ClearPlaylistSongsAsync(int playlistId);

    /// <summary>
    /// 检查歌曲是否在播放列表中
    /// </summary>
    /// <param name="playlistId">播放列表ID</param>
    /// <param name="songId">歌曲ID</param>
    /// <returns>歌曲是否在播放列表中</returns>
    Task<bool> IsSongInPlaylistAsync(int playlistId, int songId);

    #endregion

    #region 统计方法

    /// <summary>
    /// 获取音乐库统计信息
    /// </summary>
    /// <returns>包含总歌曲数、总歌单数、总歌手数、总专辑数的统计对象</returns>
    Task<LibraryStatistics> GetLibraryStatisticsAsync();

    #endregion
}
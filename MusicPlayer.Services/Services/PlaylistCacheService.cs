using MusicPlayer.Core.Data;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using System.IO;
using System.Runtime.Caching;
 
using System.Text.Json;
using System.Text.Json.Serialization;
using TagLib;
using File = System.IO.File;

namespace MusicPlayer.Services;

/// <summary>
/// 自定义TimeSpan JSON转换器，支持从数字（毫秒）转换为TimeSpan
/// </summary>
public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 尝试从字符串解析
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (TimeSpan.TryParse(value, out var result))
            {
                return result;
            }
        }
        // 尝试从数字（毫秒）解析
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var milliseconds = reader.GetDouble();
            return TimeSpan.FromMilliseconds(milliseconds);
        }
        
        return TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // 序列化为毫秒数
        writer.WriteNumberValue(value.TotalMilliseconds);
    }
}

/// <summary>
/// 播放列表缓存服务实现
/// 负责管理播放列表的内存缓存，确保应用程序中只有一份数据源
/// 
/// 数据流设计：
/// 1. 初始化时：从SQLite数据库加载到内存缓存
/// 2. 运行时：以内存缓存为唯一数据源
/// 3. 持久化：通过SavePlaylist方法将数据写回SQLite
/// 
/// 注意：其他服务不应直接访问SQLite，必须通过此类
/// </summary>
public class PlaylistCacheService : IPlaylistCacheService
{
    private static readonly MemoryCache _cache = new MemoryCache("PlaylistCache");
    private const string PlaylistKey = "MainPlaylist";
    private const string PlaylistsKey = "Playlists";
    private bool _isInitialized = false;
    private readonly object _lock = new object();
    private readonly PlaylistDataDAL _playlistDataDal;
    private static int _nextSongId = 1; // 用于生成唯一ID的计数器
    private static int _nextPlaylistId = 1; // 用于生成唯一播放列表ID的计数器

    public bool IsCacheReady => _isInitialized;

    public int SongCount 
    { 
        get 
        { 
            lock (_lock)
            {
                return GetPlaylist().Count;
            }
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public PlaylistCacheService()
    {
        _playlistDataDal = new PlaylistDataDAL(Paths.PlaylistDatabasePath); 

    }

    /// <summary>
    /// 初始化缓存，从SQLite数据库加载播放列表到内存缓存
    /// </summary>
    public async Task<bool> InitializeCacheAsync()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return true;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("PlaylistCacheService: 开始初始化缓存，从SQLite加载数据");
            
            // 从SQLite数据库加载播放列表
            var songs = _playlistDataDal.LoadAllSongs();
            
            // 设置下一个ID为现有最大ID+1，避免ID冲突
            if (songs.Count > 0)
            {
                _nextSongId = songs.Max(s => s.Id) + 1;
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 设置起始ID为 {_nextSongId}，基于现有歌曲的最大ID");
            }
            
            // 设置所有歌曲为延迟加载专辑封面状态
            // 封面将在需要时通过可视范围检测服务懒加载，而不是一次性全部加载
            foreach (var song in songs)
            {

            }
            
            // 将加载的数据存入缓存
            _cache.Set(PlaylistKey, songs, new CacheItemPolicy());
            
            // 加载所有播放列表
            var playlists = _playlistDataDal.GetAllPlaylists();
            
            // 如果没有播放列表，创建一个收藏列表
            if (playlists.Count == 0)
            {
                var favoritesPlaylist = new Playlist
                {
                    Id = 0,
                    Name = "收藏列表",
                    Description = "包含用户收藏的歌曲",
                    IsDefault = true, // 标记为系统默认列表
                    CreatedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now
                };
                
                _playlistDataDal.InsertPlaylist(favoritesPlaylist);
                playlists.Add(favoritesPlaylist);
                System.Diagnostics.Debug.WriteLine("PlaylistCacheService: 创建了收藏列表");
            }
            
            // 设置下一个播放列表ID为现有最大ID+1，避免ID冲突
            if (playlists.Count > 0)
            {
                _nextPlaylistId = playlists.Max(p => p.Id) + 1;
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 设置起始播放列表ID为 {_nextPlaylistId}，基于现有播放列表的最大ID");
            }
            
            // 将播放列表存入缓存
            _cache.Set(PlaylistsKey, playlists, new CacheItemPolicy());
            
            _isInitialized = true;
            
           
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 缓存初始化完成，加载了 {songs.Count} 首歌曲和 {playlists.Count} 个播放列表");
                 return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 初始化播放列表缓存失败: {ex.Message}");
            
            // 创建空缓存作为后备
            _cache.Set(PlaylistKey, new List<Song>(), new CacheItemPolicy());
            
            _isInitialized = true;
            
            return false;
        }
    }

    /// <summary>
    /// 获取当前播放列表
    /// </summary>
    public List<Song> GetPlaylist()
    {
        lock (_lock)
        {
            if (_cache.Contains(PlaylistKey))
            {
                return new List<Song>((List<Song>)_cache.Get(PlaylistKey));
            }
            return new List<Song>();
        }
    }

    /// <summary>
    /// 更新播放列表到缓存
    /// </summary>
    public void UpdatePlaylist(List<Song> songs)
    {
        lock (_lock)
        {
            _cache.Set(PlaylistKey, new List<Song>(songs), new CacheItemPolicy());
            
            // 计算缓存大小
            long totalMemoryMB = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
            
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 更新缓存，歌曲数量: {songs.Count}");
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 内存占用: {totalMemoryMB} MB");
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            // 清空内存缓存
            _cache.Remove(PlaylistKey);
            _cache.Set(PlaylistKey, new List<Song>(), new CacheItemPolicy());
            
            // 清空数据库中的数据
            try
            { 
                _playlistDataDal.ClearPlaylist( );
                System.Diagnostics.Debug.WriteLine("PlaylistCacheService: 缓存和数据库已清空");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 清空数据库失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 添加歌曲到缓存
    /// </summary>
    public int AddSongs(IEnumerable<Song> songs)
    {
        lock (_lock)
        {
            var currentPlaylist = GetPlaylist();
            var currentPaths = new HashSet<string>(currentPlaylist.Select(s => s.FilePath));
            
            var songsToAdd = new List<Song>();
            int addedCount = 0;
            
            foreach (var song in songs)
            {
                if (!currentPaths.Contains(song.FilePath))
                {
                    // 为新歌曲分配唯一ID
                    song.Id = _nextSongId++;
                    songsToAdd.Add(song);
                    addedCount++;
                }
            }
            
            if (addedCount > 0)
            {
                //currentPlaylist.AddRange(songsToAdd);
                //UpdatePlaylist(currentPlaylist);
                
                // 异步保存新添加的歌曲到数据库
                _ = Task.Run(async () => await SaveNewSongsToDatabaseAsync(songsToAdd));
                currentPlaylist.AddRange(songsToAdd);
                UpdatePlaylist(currentPlaylist);
            }
            
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 添加了 {addedCount} 首新歌曲到缓存");
            return addedCount;
        }
    }
    
    /// <summary>
    /// 异步保存新添加的歌曲到数据库
    /// 只在添加文件时调用，其他操作（如排序）不会触发此方法
    /// </summary>
    /// <param name="newSongs">新添加的歌曲列表</param>
    private async Task SaveNewSongsToDatabaseAsync(List<Song> newSongs)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 开始异步保存 {newSongs.Count} 首新歌曲到数据库");
            
            // 使用LiteDB保存新添加的歌曲
            _playlistDataDal.InsertSongs(newSongs);
                
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 成功保存 {newSongs.Count} 首新歌曲到数据库");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 异步保存歌曲到数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从缓存中移除歌曲
    /// </summary>
    public int RemoveSongs(IEnumerable<string> filePaths)
    {
        lock (_lock)
        {
            var currentPlaylist = GetPlaylist();
            var pathsToRemove = filePaths.ToHashSet();
            
            int removedCount = currentPlaylist.RemoveAll(s => pathsToRemove.Contains(s.FilePath));
            
            if (removedCount > 0)
            {
                UpdatePlaylist(currentPlaylist);
            }
            
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 从缓存中移除了 {removedCount} 首歌曲");
            return removedCount;
        }
    }

    /// <summary>
    /// 根据文件路径获取歌曲
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>匹配的歌曲，找不到则返回null</returns>
    public Song? GetSongByFilePath(string filePath)
    {
        lock (_lock)
        {
            var playlist = GetPlaylist();
            return playlist.FirstOrDefault(s => s.FilePath == filePath);
        }
    }

    /// <summary>
    /// 检查歌曲是否存在于缓存中
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>歌曲是否存在</returns>
    public bool ContainsSong(string filePath)
    {
        lock (_lock)
        {
            var playlist = GetPlaylist();
            return playlist.Any(s => s.FilePath == filePath);
        }
    }

    /// <summary>
    /// 根据当前排序规则获取排序后的播放列表
    /// </summary>
    /// <param name="sortRule">排序规则</param>
    /// <returns>排序后的播放列表</returns>
    public List<Song> GetSortedPlaylist( SortRule sortRule)
    {
        lock (_lock)
        {
            var playlist = GetPlaylist();
            if (playlist == null || playlist.Count == 0)
            {
                return new List<Song>();
            }
            
            return sortRule switch
            {
                SortRule.ByTitle => playlist.OrderBy(s => s.Title).ToList(),
                SortRule.ByArtist => playlist.OrderBy(s => s.Artist).ToList(),
                SortRule.ByAlbum => playlist.OrderBy(s => s.Album).ThenBy(s => s.Title).ToList(),
                SortRule.ByDuration => playlist.OrderBy(s => s.Duration).ToList(),
                SortRule.ByFileSize => playlist.OrderBy(s => s.FileSize).ToList(),
                SortRule.ByAddedTime => playlist.OrderByDescending(s => s.AddedTime).ToList(),
                _ => playlist
            };
        }
    }
    
        /// <summary>
        /// 保存播放列表到数据库
        /// 注意：此方法现在主要用于特殊情况，如导入播放列表或手动同步
        /// 新添加的歌曲通过AddSongs方法自动异步保存到数据库
        /// </summary>
        /// <param name="playlist">要保存的播放列表</param>
        /// <param name="sortRule">排序规则</param>
        public void SavePlaylist(List<Song> playlist, SortRule sortRule)
        {
            lock (_lock)
            {
                try
                {
                    // 使用LiteDB保存播放列表
                    _playlistDataDal.InsertSongs(playlist);
                        
                    System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 已保存 {playlist.Count} 首歌曲到数据库，排序规则: {sortRule}");

                    // 更新缓存
                    UpdatePlaylist(_playlistDataDal.LoadAllSongs());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 保存播放列表失败: {ex.Message}");
                    throw;
                }
            }
        }

    /// <summary>
    /// 异步更新单个歌曲状态到数据库
    /// </summary>
    /// <param name="song">要更新的歌曲</param>
    /// <returns>异步任务</returns>
    public Task UpdateSongStatusInDatabaseAsync(Song song)
    {
        return Task.Run(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 开始异步更新歌曲状态到数据库: {song.Title}");
                
                // 使用LiteDB更新单个歌曲状态
                _playlistDataDal.UpdateSong(song);
                    
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 成功更新歌曲状态到数据库: {song.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 异步更新歌曲状态到数据库失败: {ex.Message}");
            }
        });
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cache?.Dispose();
        _playlistDataDal?.Dispose();
    }

    #region 播放列表管理方法实现

    /// <summary>
    /// 创建播放列表
    /// </summary>
    public async Task<int> InsertPlaylistAsync(Playlist playlist)
    {
        lock (_lock)
        {
            try
            {
                // 为新播放列表分配唯一ID
                playlist.Id = _nextPlaylistId++;
                
                // 保存到数据库
                _playlistDataDal.InsertPlaylist(playlist);
                
                // 更新缓存
                var playlists = GetPlaylistsFromCache();
                playlists.Add(playlist);
                _cache.Set(PlaylistsKey, playlists, new CacheItemPolicy());
                
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 创建了新的播放列表: {playlist.Name}");
                return playlist.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 创建播放列表失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 更新播放列表
    /// </summary>
    public async Task<int> UpdatePlaylistAsync(Playlist playlist)
    {
        lock (_lock)
        {
            try
            {
                // 保存到数据库
                int result = _playlistDataDal.UpdatePlaylist(playlist);
                
                if (result > 0)
                {
                    // 更新缓存
                    var playlists = GetPlaylistsFromCache();
                    var index = playlists.FindIndex(p => p.Id == playlist.Id);
                    if (index >= 0)
                    {
                        playlists[index] = playlist;
                        _cache.Set(PlaylistsKey, playlists, new CacheItemPolicy());
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 更新了播放列表: {playlist.Name}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 更新播放列表失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 删除播放列表
    /// </summary>
    public async Task<int> DeletePlaylistAsync(int playlistId)
    {
        lock (_lock)
        {
            try
            {
                // 保存到数据库
                int result = _playlistDataDal.DeletePlaylist(playlistId);
                
                if (result > 0)
                {
                    // 更新缓存
                    var playlists = GetPlaylistsFromCache();
                    playlists.RemoveAll(p => p.Id == playlistId);
                    _cache.Set(PlaylistsKey, playlists, new CacheItemPolicy());
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 删除了播放列表: {playlistId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 删除播放列表失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 根据ID获取播放列表详情
    /// </summary>
    public async Task<Playlist?> GetPlaylistByIdAsync(int playlistId)
    {
        lock (_lock)
        {
            try
            {
                var playlists = GetPlaylistsFromCache();
                return playlists.FirstOrDefault(p => p.Id == playlistId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 根据ID获取播放列表失败: {ex.Message}");
                // 从数据库重试
                return _playlistDataDal.GetPlaylistById(playlistId);
            }
        }
    }

    /// <summary>
    /// 获取所有播放列表
    /// </summary>
    public async Task<List<Playlist>> GetAllPlaylistsAsync()
    {
        lock (_lock)
        {
            try
            {
                return new List<Playlist>(GetPlaylistsFromCache());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 获取所有播放列表失败: {ex.Message}");
                // 从数据库重试
                return _playlistDataDal.GetAllPlaylists();
            }
        }
    }

    /// <summary>
    /// 获取默认播放列表
    /// </summary>
    public async Task<Playlist?> GetDefaultPlaylistAsync()
    {
        lock (_lock)
        {
            try
            {
                var playlists = GetPlaylistsFromCache();
                return playlists.FirstOrDefault(p => p.IsDefault);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 获取默认播放列表失败: {ex.Message}");
                // 从数据库重试
                return _playlistDataDal.GetDefaultPlaylist();
            }
        }
    }

    #endregion

    #region 播放列表歌曲管理方法实现

    /// <summary>
    /// 添加歌曲到播放列表
    /// </summary>
    public async Task<int> AddSongToPlaylistAsync(int playlistId, int songId)
    {
        lock (_lock)
        {
            try
            {
                // 保存到数据库
                int result = _playlistDataDal.AddSongToPlaylist(playlistId, songId);
               
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 添加歌曲到播放列表: 播放列表ID={playlistId}, 歌曲ID={songId}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 添加歌曲到播放列表失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 从播放列表移除歌曲
    /// </summary>
    public async Task<int> RemoveSongFromPlaylistAsync(int playlistId, int songId)
    {
        lock (_lock)
        {
            try
            {
                // 保存到数据库
                int result = _playlistDataDal.RemoveSongFromPlaylist(playlistId, songId);
                
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 从播放列表移除歌曲: 播放列表ID={playlistId}, 歌曲ID={songId}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 从播放列表移除歌曲失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 获取播放列表中的所有歌曲
    /// </summary>
    public async Task<List<Song>> GetSongsByPlaylistIdAsync(int playlistId)
    {
        lock (_lock)
        {
            try
            {
                // 从数据库获取
                var songs = _playlistDataDal.GetSongsByPlaylistId(playlistId);
                
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 获取播放列表中的歌曲: 播放列表ID={playlistId}, 歌曲数量={songs.Count}");
                return songs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 获取播放列表中的歌曲失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 清空播放列表中的歌曲
    /// </summary>
    public async Task<int> ClearPlaylistSongsAsync(int playlistId)
    {
        lock (_lock)
        {
            try
            {
                // 保存到数据库
                int result = _playlistDataDal.ClearPlaylistSongs(playlistId);
                
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 清空播放列表中的歌曲: 播放列表ID={playlistId}, 清空数量={result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 清空播放列表中的歌曲失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 检查歌曲是否在播放列表中
    /// </summary>
    public async Task<bool> IsSongInPlaylistAsync(int playlistId, int songId)
    {
        lock (_lock)
        {
            try
            {
                // 从数据库检查
                return _playlistDataDal.IsSongInPlaylist(playlistId, songId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 检查歌曲是否在播放列表中失败: {ex.Message}");
                return false;
            }
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 从缓存获取播放列表
    /// </summary>
    private List<Playlist> GetPlaylistsFromCache()
    {
        if (_cache.Contains(PlaylistsKey))
        {
            return (List<Playlist>)_cache.Get(PlaylistsKey);
        }
        return new List<Playlist>();
    }

    #endregion

    #region 统计方法实现

    /// <summary>
    /// 获取音乐库统计信息
    /// </summary>
    /// <returns>包含总歌曲数、总歌单数、总歌手数、总专辑数的统计对象</returns>
    public async Task<LibraryStatistics> GetLibraryStatisticsAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 获取所有歌曲
                    var songs = GetPlaylist();
                    var totalSongs = songs.Count;

                    // 计算不重复的歌手数（去除空字符串）
                    var totalArtists = songs
                        .Where(s => !string.IsNullOrWhiteSpace(s.Artist))
                        .Select(s => s.Artist.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();

                    // 计算不重复的专辑数（去除空字符串）
                    var totalAlbums = songs
                        .Where(s => !string.IsNullOrWhiteSpace(s.Album))
                        .Select(s => s.Album.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();

                    // 获取所有播放列表
                    var playlists = GetPlaylistsFromCache();
                    var totalPlaylists = playlists.Count;

                    var statistics = new LibraryStatistics
                    {
                        TotalSongs = totalSongs,
                        TotalPlaylists = totalPlaylists,
                        TotalArtists = totalArtists,
                        TotalAlbums = totalAlbums
                    };

                    System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 获取音乐库统计 - 歌曲:{totalSongs}, 歌单:{totalPlaylists}, 歌手:{totalArtists}, 专辑:{totalAlbums}");

                    return statistics;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 获取音乐库统计失败: {ex.Message}");
                    return LibraryStatistics.Empty;
                }
            }
        });
    }

    #endregion
}
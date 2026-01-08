using System.Runtime.Caching;
using System.IO;
using System.Text.Json.Serialization;
using TagLib;
using MusicPlayer.Core.Models;
 
using System.Text.Json;
using File = System.IO.File;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Enums;

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
    private bool _isInitialized = false;
    private readonly object _lock = new object();
    private readonly PlaylistDataDAL _playlistDataDal;
    private static int _nextSongId = 1; // 用于生成唯一ID的计数器

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
                song.DelayAlbumArtLoading = true;
            }
            
            // 将加载的数据存入缓存
            _cache.Set(PlaylistKey, songs, new CacheItemPolicy());
            
            _isInitialized = true;
            
           
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 缓存初始化完成，加载了 {songs.Count} 首歌曲");
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
            long albumArtSize = 0;
            int albumArtCount = 0;
            
            foreach (var song in songs)
            {
                if (song.AlbumArtData != null)
                {
                    albumArtSize += song.AlbumArtData.Length;
                    albumArtCount++;
                }
            }
            
            long albumArtSizeMB = albumArtSize / (1024 * 1024);
            
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 更新缓存，歌曲数量: {songs.Count}");
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 内存占用: {totalMemoryMB} MB");
            System.Diagnostics.Debug.WriteLine($"PlaylistCacheService: 专辑封面数据: {albumArtCount} 个文件，占用 {albumArtSizeMB} MB");
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
                currentPlaylist.AddRange(songsToAdd);
                UpdatePlaylist(currentPlaylist);
                
                // 异步保存新添加的歌曲到数据库
                _ = Task.Run(async () => await SaveNewSongsToDatabaseAsync(songsToAdd));
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
}
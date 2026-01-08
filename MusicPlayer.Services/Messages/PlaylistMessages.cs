using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 播放列表数据变化消息 - 统一的播放列表通知机制
    /// </summary>
    public class PlaylistDataChangedMessage
    {
        public DataChangeType Type { get; set; }
        public SortRule CurrentSortRule { get; set; }
        public List<Song> Data { get; set; }
        public Song? CurrentSong { get; set; }

        public PlaylistDataChangedMessage(DataChangeType type, SortRule sortRule, List<Song> data, Song? currentSong = null)
        {
            Type = type;
            CurrentSortRule = sortRule;
            Data = data ?? new List<Song>();
            CurrentSong = currentSong;
        }
    }

    /// <summary>
    /// 添加文件消息 - 替代原有的复杂添加机制
    /// </summary>
    public class AddFilesMessage
    {
        public IEnumerable<string> FilePaths { get; set; } = new List<string>();
    }

    /// <summary>
    /// 排序播放列表消息 - 使用SortRule枚举
    /// </summary>
    public class SortPlaylistMessage
    {
        public SortRule SortRule { get; set; }
        
        public SortPlaylistMessage(SortRule sortRule)
        {
            SortRule = sortRule;
        }
    }

    /// <summary>
    /// 添加音乐文件消息
    /// </summary>
    public class AddMusicFilesMessage : RequestMessage<bool> { }

    /// <summary>
    /// 切换播放列表消息
    /// </summary>
    public class TogglePlaylistMessage : RequestMessage<bool> { }

    /// <summary>
    /// 搜索文本变化消息
    /// </summary>
    public class SearchTextChangedMessage : ValueChangedMessage<string> 
    { 
        public SearchTextChangedMessage(string value) : base(value) { }
    }

    /// <summary>
    /// 删除歌曲消息
    /// </summary>
    public class DeleteSongMessage
    {
        public Song Song { get; }
        
        public DeleteSongMessage(Song song)
        {
            Song = song;
        }
    }

    /// <summary>
    /// 清空播放列表消息
    /// </summary>
    public class ClearPlaylistMessage { }

    /// <summary>
    /// 歌曲被移除消息
    /// </summary>
    public class SongRemovedMessage : ValueChangedMessage<Song> 
    { 
        public SongRemovedMessage(Song value) : base(value) { }
    }

    /// <summary>
    /// 播放列表被清空消息
    /// </summary>
    public class PlaylistClearedMessage : RequestMessage<bool> { }

    /// <summary>
    /// 播放列表已更新消息
    /// </summary>
    public class PlaylistUpdatedMessage
    {
        public ObservableCollection<Song> Playlist { get; }
        
        public PlaylistUpdatedMessage(ObservableCollection<Song> playlist)
        {
            Playlist = playlist;
        }
    }

    /// <summary>
    /// 播放列表折叠状态变化消息
    /// </summary>
    public class PlaylistCollapsedChangedMessage : ValueChangedMessage<bool> 
    { 
        public PlaylistCollapsedChangedMessage(bool value) : base(value) { }
    }

    /// <summary>
    /// 播放列表排序消息
    /// </summary>
    public class PlaylistSortMessage : RequestMessage<bool>
    {
        public string SortBy { get; }
        public bool Ascending { get; }
        
        public PlaylistSortMessage(string sortBy, bool ascending = true)
        {
            SortBy = sortBy;
            Ascending = ascending;
        }
    }
    
    /// <summary>
    /// 播放列表过滤消息
    /// </summary>
    public class PlaylistFilterMessage : RequestMessage<bool>
    {
        public string FilterText { get; }
        public string FilterType { get; }
        
        public PlaylistFilterMessage(string filterText, string filterType = "All")
        {
            FilterText = filterText;
            FilterType = filterType;
        }
    }
    
    /// <summary>
    /// 移除歌曲消息
    /// </summary>
    public class RemoveSongMessage : RequestMessage<bool>
    {
        public Song Song { get; }
        
        public RemoveSongMessage(Song song)
        {
            Song = song;
        }
    }
    
    /// <summary>
    /// 批量移除歌曲消息
    /// </summary>
    public class RemoveSongsMessage : RequestMessage<int>
    {
        public List<Song> Songs { get; }
        
        public RemoveSongsMessage(List<Song> songs)
        {
            Songs = songs ?? new List<Song>();
        }
    }
    
    /// <summary>
    /// 滚动到当前播放歌曲消息
    /// </summary>
    public class ScrollToCurrentSongMessage
    {
        public Song CurrentSong { get; }
        
        public ScrollToCurrentSongMessage(Song currentSong)
        {
            CurrentSong = currentSong;
        }
    }
    
    /// <summary>
    /// 添加歌曲到播放列表消息
    /// </summary>
    public class AddToPlaylistMessage
    {
        public Song Song { get; }
        
        public AddToPlaylistMessage(Song song)
        {
            Song = song;
        }
    }
}
using System.Collections.ObjectModel;
using MusicPlayer.Core.Models;

using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 播放列表视图模型接口
    /// </summary>
    public interface IPlaylistViewModel : IViewModelLifecycle
    {
        
        /// <summary>
        /// 当前播放歌曲
        /// </summary>
        Core.Models.Song? CurrentSong { get; }

        /// <summary>
        /// 过滤后的播放列表
        /// </summary>
        ObservableCollection<Core.Models.Song> FilteredPlaylist { get; }

        /// <summary>
        /// 专辑加载请求事件
        /// </summary>
        event EventHandler<AlbumLoadRequestEventArgs> AlbumLoadRequested;

        
    }
}
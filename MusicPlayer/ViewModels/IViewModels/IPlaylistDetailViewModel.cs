using MusicPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌单详情页面视图模型接口
    /// </summary>
    public interface IPlaylistDetailViewModel : IViewModel
    {
        /// <summary>
        /// 当前歌单
        /// </summary>
        Playlist CurrentPlaylist { get; set; }

        /// <summary>
        /// 过滤后的播放列表
        /// </summary>
        ObservableCollection<Song> FilteredPlaylist { get; }

        /// <summary>
        /// 播放列表歌曲总数
        /// </summary>
        int SongCount { get; }

        /// <summary>
        /// 搜索文本
        /// </summary>
        string SearchText { get; set; }

        /// <summary>
        /// 播放全部命令
        /// </summary>
        ICommand PlayAllCommand { get; }

        /// <summary>
        /// 随机播放命令
        /// </summary>
        ICommand ShufflePlayCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 播放选中歌曲命令
        /// </summary>
        ICommand PlaySelectedSongCommand { get; }

        /// <summary>
        /// 删除选中歌曲命令
        /// </summary>
        ICommand DeleteSelectedSongCommand { get; }

        /// <summary>
        /// 切换歌曲收藏状态命令
        /// </summary>
        ICommand ToggleSongHeartCommand { get; }

        /// <summary>
        /// 添加歌曲到歌单命令
        /// </summary>
        ICommand AddSongToPlaylistCommand { get; }

        /// <summary>
        /// 所有歌单列表
        /// </summary>
        ObservableCollection<Playlist> AllPlaylists { get; }
    }
}
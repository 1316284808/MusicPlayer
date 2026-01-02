
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌词视图模型接口
    /// </summary>
    public interface ILyricsViewModel : IViewModelLifecycle
    {
        /// <summary>
        /// 当前显示的歌词
        /// </summary>
        string CurrentLyrics { get; }
    }
}
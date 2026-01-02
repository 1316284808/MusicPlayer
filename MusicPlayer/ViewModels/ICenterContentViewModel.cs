

using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 中心内容视图模型接口
    /// </summary>
    public interface ICenterContentViewModel : IViewModelLifecycle
    {
        /// <summary>
        /// 当前歌曲
        /// </summary>
        Core.Models.Song? CurrentSong { get; }
        
        /// <summary>
        /// 播放状态
        /// </summary>
        bool IsPlaying { get; }
        
        /// <summary>
        /// 当前歌曲标题
        /// </summary>
        string CurrentSongTitle { get; }
        
        /// <summary>
        /// 当前歌曲艺术家
        /// </summary>
        string CurrentSongArtist { get; }
        
        /// <summary>
        /// 当前歌曲专辑
        /// </summary>
        string CurrentSongAlbum { get; }
        
        /// <summary>
        /// 当前歌曲专辑封面
        /// </summary>
        System.Windows.Media.Imaging.BitmapImage? CurrentSongAlbumArt { get; }
        
        /// <summary>
        /// 当前歌曲原始专辑封面
        /// </summary>
        System.Windows.Media.Imaging.BitmapImage? CurrentSongOriginalAlbumArt { get; }
    }
}
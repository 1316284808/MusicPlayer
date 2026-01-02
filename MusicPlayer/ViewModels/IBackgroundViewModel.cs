using System.ComponentModel;
using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 背景视图模型接口
    /// </summary>
    public interface IBackgroundViewModel : IViewModelLifecycle, INotifyPropertyChanged
    {
        /// <summary>
        /// 当前播放的歌曲
        /// </summary>
        Core.Models.Song? CurrentSong { get; }
        
        // 移除BitmapImage属性，改用Converter处理UI逻辑
        // 背景图像显示应由View层的Converter处理
        
        /// <summary>
        /// 背景模糊半径
        /// </summary>
        double BackgroundBlurRadius { get; set; }
    }
}
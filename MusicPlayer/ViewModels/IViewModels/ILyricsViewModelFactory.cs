using MusicPlayer.Core.Interface;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌词视图模型工厂接口
    /// </summary>
    public interface ILyricsViewModelFactory
    {
        /// <summary>
        /// 创建新的歌词视图模型实例
        /// </summary>
        /// <returns>新的歌词视图模型实例</returns>
        ILyricsViewModel CreateLyricsViewModel();
    }
}
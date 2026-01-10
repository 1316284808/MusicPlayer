

using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 控制栏视图模型接口
    /// </summary>
    public interface IControlBarViewModel : IViewModelLifecycle
    {
        // 播放列表视图模型
        IPlaylistViewModel PlaylistViewModel { get; }
    }
}
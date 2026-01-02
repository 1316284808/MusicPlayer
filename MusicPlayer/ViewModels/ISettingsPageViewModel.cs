

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 设置页面视图模型接口
    /// </summary>
    public interface ISettingsPageViewModel : IViewModel
    {
        IWindowSettingsViewModel WindowSettingsViewModel { get; }
        PlaylistSettingViewModel PlaylistSettingViewModel { get; }
    }
}
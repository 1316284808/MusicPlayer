using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 设置页面视图模型
    /// </summary>
    public partial class SettingsPageViewModel : ObservableObject, ISettingsPageViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IWindowSettingsViewModel _windowSettingsViewModel;
        private readonly PlaylistSettingViewModel _playlistSettingViewModel;
        private readonly ISoundSettingsViewModel _soundSettingsViewModel;

        public SettingsPageViewModel(
            IMessagingService messagingService, 
            IWindowSettingsViewModel windowSettingsViewModel,
            PlaylistSettingViewModel playlistSettingViewModel,
            ISoundSettingsViewModel soundSettingsViewModel)
        {
            _messagingService = messagingService;
            _windowSettingsViewModel = windowSettingsViewModel;
            _playlistSettingViewModel = playlistSettingViewModel;
            _soundSettingsViewModel = soundSettingsViewModel;
        }

        /// <summary>
        /// 窗体设置视图模型
        /// </summary>
        public IWindowSettingsViewModel WindowSettingsViewModel => _windowSettingsViewModel;
        
        /// <summary>
        /// 播放列表设置视图模型
        /// </summary>
        public PlaylistSettingViewModel PlaylistSettingViewModel => _playlistSettingViewModel;
        
        /// <summary>
        /// 音频设置视图模型
        /// </summary>
        public ISoundSettingsViewModel SoundSettingsViewModel => _soundSettingsViewModel;
        /// <summary>
        /// 执行返回首页操作
        /// </summary>
        public void ExecuteBackToHome()
        {
            try
            {
                // 通过消息服务请求导航
                _messagingService.Send(new NavigateToPageMessage("Page/PlaylistPage.xaml"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到首页失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void BackToHome()
        {
            ExecuteBackToHome();
        }



        /// <summary>
        /// 处理导航消息
        /// </summary>
        /// <param name="message">导航消息</param>
        private void OnNavigateToPageMessage(NavigateToPageMessage message)
        {
            try
            {
                // 通过消息服务发送导航请求，让导航服务处理
                _messagingService.Send(new NavigateToPageMessage(message.PageUri));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理导航消息失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ApplySettings()
        {
            try
            {
                // 应用设置并返回首页
                BackToHome();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 取消注册所有消息处理器
            _messagingService.Unregister(this);
            
            // 清理子ViewModel
            if (_windowSettingsViewModel is ObservableObject windowSettingsViewModel)
            {
                windowSettingsViewModel.Cleanup();
            }
            
            if (_playlistSettingViewModel is ObservableObject playlistSettingViewModel)
            {
                playlistSettingViewModel.Cleanup();
            }
            
            if (_soundSettingsViewModel is ObservableObject soundSettingsViewModel)
            {
                soundSettingsViewModel.Cleanup();
            }
        }
    }

    /// <summary>
    /// 页面导航消息
    /// </summary>
    public class NavigateToPageMessage
    {
        public string PageUri { get; }

        public NavigateToPageMessage(string pageUri)
        {
            PageUri = pageUri;
        }
    }
}
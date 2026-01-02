using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// 播放列表设置控件视图模型
    /// 负责播放列表设置相关操作，如清空播放列表
    /// </summary>
    public class PlaylistSettingViewModel : ObservableObject
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IDispatcherService _dispatcherService;
        private bool _isClearingPlaylist;

        /// <summary>
        /// 是否正在清空播放列表
        /// </summary>
        public bool IsClearingPlaylist
        {
            get => _isClearingPlaylist;
            set
            {
                if (_isClearingPlaylist != value)
                {
                    _isClearingPlaylist = value;
                    OnPropertyChanged(nameof(IsClearingPlaylist));
                    OnPropertyChanged(nameof(CanClearPlaylist));
                }
            }
        }

        /// <summary>
        /// 是否可以清空播放列表
        /// </summary>
        public bool CanClearPlaylist => !IsClearingPlaylist;

        /// <summary>
        /// 清空播放列表命令
        /// </summary>
        public ICommand ClearPlaylistCommand { get; }

        /// <summary>
        /// 初始化 PlaylistSettingViewModel 类的新实例
        /// </summary>
        /// <param name="messagingService">消息服务</param>
        /// <param name="playlistDataService">播放列表数据服务</param>
        /// <param name="dispatcherService">UI线程调度服务</param>
        public PlaylistSettingViewModel(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IDispatcherService dispatcherService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            ClearPlaylistCommand = new RelayCommand(async () => await ExecuteClearPlaylist(), () => CanClearPlaylist);
        }

        /// <summary>
        /// 执行清空播放列表操作
        /// </summary>
        private async Task ExecuteClearPlaylist()
        {
            try
            {
                // 检查播放列表是否为空
                var playlist = _playlistDataService.DataSource;
                bool isEmpty = playlist == null || playlist.Count == 0;
                
                // 清除临时数据缓存
                _playlistDataService.ClearDataSource();
                
                if (isEmpty)
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistSettingViewModel: 播放列表为空，无需清空");
                    await ShowMessageAsync("播放列表已经是空的", MessageType.Information);
                    return;
                }

                // 显示确认对话框
                var confirmed = await ShowConfirmDialogAsync("确认清空播放列表", $"将删除播放列表中的所有 {playlist.Count} 首歌曲，但不会影响电脑本地文件。");
                if (!confirmed)
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistSettingViewModel: 用户取消了清空播放列表操作");
                    return;
                }

                // 设置正在清空状态
                IsClearingPlaylist = true;
                System.Diagnostics.Debug.WriteLine("PlaylistSettingViewModel: 开始清空播放列表");

                // 发送清空播放列表消息
                _messagingService.Send(new ClearPlaylistMessage());

                // 等待一段时间以确保操作完成
                await Task.Delay(500);

                System.Diagnostics.Debug.WriteLine("PlaylistSettingViewModel: 清空播放列表操作完成");
                await ShowMessageAsync("播放列表已清空", MessageType.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistSettingViewModel: 清空播放列表失败: {ex.Message}");
                await ShowMessageAsync($"清空播放列表失败: {ex.Message}", MessageType.Error);
            }
            finally
            {
                // 重置状态
                IsClearingPlaylist = false;
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">对话框消息</param>
        /// <returns>用户是否确认</returns>
        private async Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            return await _dispatcherService.InvokeAsync(() =>
            {
                // 这里应该显示一个确认对话框
                // 由于项目中可能没有统一的对话框服务，这里使用简单的实现
                // 在实际项目中，可以注入 IDialogService 并使用它
                
                // 暂时使用消息框实现
                var result = System.Windows.MessageBox.Show(
                    message,
                    title,
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                return result == System.Windows.MessageBoxResult.Yes;
            });
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="type">消息类型</param>
        private async Task ShowMessageAsync(string message, MessageType type)
        {
            await _dispatcherService.InvokeAsync(() =>
            {
                // 这里应该显示一个通知消息
                // 由于项目中可能没有统一的通知服务，这里使用简单的实现
                // 在实际项目中，可以注入 INotificationService 并使用它
                
                // 暂时使用消息框实现
                var icon = type switch
                {
                    MessageType.Information => System.Windows.MessageBoxImage.Information,
                    MessageType.Error => System.Windows.MessageBoxImage.Error,
                    MessageType.Warning => System.Windows.MessageBoxImage.Warning,
                    _ => System.Windows.MessageBoxImage.Information
                };
                
                System.Windows.MessageBox.Show(message, "提示", System.Windows.MessageBoxButton.OK, icon);
            });
        }
    }
}
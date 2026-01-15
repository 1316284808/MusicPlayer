using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.ViewModels
{
    
    /// <summary>
    /// 播放列表设置控件视图模型
    /// 负责播放列表设置相关操作，如清空播放列表
    /// </summary>
    public class PlaylistSettingViewModel : ObservableObject
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IConfigurationService _configurationService;
        private bool _isClearingPlaylist;
        private bool _isCoverCacheEnabled;
        private string _lyricDirectory;

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
        /// 缓存路径
        /// </summary>
        public string AlbumArtCachePath {
            get {
                if (!IsCoverCacheEnabled) { return ""; }
                return   Paths.AlbumArtCacheDirectory; 
            } 
        }
        /// <summary>
        /// 是否可以清空播放列表
        /// </summary>
        public bool CanClearPlaylist => !IsClearingPlaylist;


        public string IsCoverCacheEnabledText => IsCoverCacheEnabled ? "开启" : "禁用";

        /// <summary>
        /// 是否启用封面缓存
        /// </summary>
        public bool IsCoverCacheEnabled
        {
            get => _isCoverCacheEnabled;
            set
            {
                if (_isCoverCacheEnabled != value)
                {
                    _isCoverCacheEnabled = value;
                     OnPropertyChanged(nameof(AlbumArtCachePath));
                    OnPropertyChanged(nameof(IsCoverCacheEnabled));
                    OnPropertyChanged(nameof(IsCoverCacheEnabledText));
                    // 更新配置并保存
                    _configurationService.CurrentConfiguration.IsCoverCacheEnabled = value;
                    _configurationService.SaveCurrentConfiguration();
                }
            }
        }

        /// <summary>
        /// 清空播放列表命令
        /// </summary>
        public ICommand ClearPlaylistCommand { get; }

        /// <summary>
        /// 打开缓存目录命令
        /// </summary>
        public ICommand OpenCacheDirectoryCommand { get; }

        /// <summary>
        /// 歌词文件目录
        /// </summary>
        public string LyricDirectory
        {
            get => _lyricDirectory;
            set
            {
                if (_lyricDirectory != value)
                {
                    _lyricDirectory = value;
                    OnPropertyChanged(nameof(LyricDirectory));
                }
            }
        }

        /// <summary>
        /// 选择歌词目录命令
        /// </summary>
        public ICommand SelectLyricDirectoryCommand { get; }

        /// <summary>
        /// 初始化 PlaylistSettingViewModel 类的新实例
        /// </summary>
        /// <param name="messagingService">消息服务</param>
        /// <param name="playlistDataService">播放列表数据服务</param>
        /// <param name="dispatcherService">UI线程调度服务</param>
        /// <param name="configurationService">配置服务</param>
        public PlaylistSettingViewModel(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IDispatcherService dispatcherService,
            IConfigurationService configurationService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            ClearPlaylistCommand = new RelayCommand(async () => await ExecuteClearPlaylist(), () => CanClearPlaylist);
            OpenCacheDirectoryCommand = new RelayCommand(ExecuteOpenCacheDirectory);
            SelectLyricDirectoryCommand = new RelayCommand(ExecuteSelectLyricDirectory);

            // 从配置初始化封面缓存开关状态
            _isCoverCacheEnabled = _configurationService.CurrentConfiguration.IsCoverCacheEnabled;
            _lyricDirectory = _configurationService.CurrentConfiguration.LyricDirectory;
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

        /// <summary>
        /// 执行打开缓存目录操作
        /// </summary>
        private void ExecuteOpenCacheDirectory()
        {
            try
            {
                // 获取封面缓存目录路径
                string cacheDirectory = Paths.AlbumArtCacheDirectory;

                // 确保目录存在
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }
                
                // 打开目录
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cacheDirectory,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistSettingViewModel: 打开缓存目录失败: {ex.Message}");
                _ = ShowMessageAsync("打开缓存目录失败", MessageType.Error);
            }
        }

        /// <summary>
        /// 执行选择歌词目录操作
        /// </summary>
        private void ExecuteSelectLyricDirectory()
        {
            try
            {
                // 创建文件夹选择器
                var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderBrowserDialog.Description = "请选择歌词文件目录";
                folderBrowserDialog.SelectedPath = string.IsNullOrEmpty(_lyricDirectory) ? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) : _lyricDirectory;
                
                // 显示文件夹选择器
                var result = folderBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // 更新歌词目录
                    LyricDirectory = folderBrowserDialog.SelectedPath;
                    
                    // 保存到配置
                    _configurationService.CurrentConfiguration.LyricDirectory = folderBrowserDialog.SelectedPath;
                    _configurationService.SaveCurrentConfiguration();
                    
                    // 显示成功消息
                    _ = ShowMessageAsync("歌词目录已设置", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistSettingViewModel: 选择歌词目录失败: {ex.Message}");
                _ = ShowMessageAsync("选择歌词目录失败", MessageType.Error);
            }
        }
    }
}
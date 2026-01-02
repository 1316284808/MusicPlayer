using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using System.Threading.Tasks;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 背景视图模型 - 负责背景效果和壁纸切换逻辑
    /// 使用 MusicPlayer.Core.Interface.IMessagingService与MainViewModel进行通信
    /// </summary>
    public class BackgroundViewModel : ObservableObject, IBackgroundViewModel
    {
    private readonly IMessagingService _messagingService;
    private Core.Models.Song? _currentSong;
    private double _backgroundBlurRadius = 20.0;
    private bool _isUpdatingBackground = false; // 防止重复更新

        /// <summary>
        /// 当前播放的歌曲
        /// </summary>
        public Core.Models.Song? CurrentSong
        {
            get => _currentSong;
            set
            {
                if (_currentSong != value)
                {
                    var oldTitle = _currentSong?.Title ?? "null";
                    var newTitle = value?.Title ?? "null";
                    
                    _currentSong = value;
                    OnPropertyChanged(nameof(CurrentSong));
                    
                    System.Diagnostics.Debug.WriteLine($"BackgroundViewModel: CurrentSong 更新 - 旧歌曲: {oldTitle}, 新歌曲: {newTitle}");
                    // AlbumArtConverter会自动处理图像显示
                }
            }
        }

      

        // 移除BitmapImage属性，改用Converter处理UI逻辑
        // 背景图像显示应由View层的Converter处理

        /// <summary>
        /// 背景模糊半径
        /// </summary>
        public double BackgroundBlurRadius
        {
            get => _backgroundBlurRadius;
            set
            {
                if (_backgroundBlurRadius != value)
                {
                    _backgroundBlurRadius = value;
                    OnPropertyChanged(nameof(BackgroundBlurRadius));
                }
            }
        }

        /// <summary>
        /// 切换壁纸命令
        /// </summary>
        public ICommand ToggleWallpaperCommand { get; }

        public BackgroundViewModel(IMessagingService messagingService)
        {
            _messagingService = messagingService;
            
            ToggleWallpaperCommand = new RelayCommand(ExecuteToggleWallpaper);
            
            // 注册消息处理器
            RegisterMessageHandlers();
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 当前歌曲变化消息
            _messagingService.Register<CurrentSongChangedMessage>(this, (r, m) =>
            {
                System.Diagnostics.Debug.WriteLine($"BackgroundViewModel: 接收到CurrentSongChangedMessage - 歌曲标题: {m.Value?.Title}");
                
                // 确保在UI线程上更新
                if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => CurrentSong = m.Value);
                }
                else
                {
                    CurrentSong = m.Value;
                }
            });
        }

        /// <summary>
        /// 执行切换壁纸操作
        /// </summary>
        private void ExecuteToggleWallpaper()
        {
            // ToggleWallpaperMessage 是 RequestMessage<bool>，需要使用带返回值的 Send 方法
            _messagingService.Send<ToggleWallpaperMessage, bool>(new ToggleWallpaperMessage());
        }

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public override void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("BackgroundViewModel: Initialize 方法被调用");
            // BackgroundViewModel现在通过AlbumArtConverter处理图像显示
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 注销消息处理器
            _messagingService.Unregister(this);
        }
         
    }

}
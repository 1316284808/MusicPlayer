using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Controls;
using MusicPlayer.Core.Interface;
using MusicPlayer.Navigation;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using MusicPlayer.ViewModels;
using Wpf.Ui.Controls;

namespace MusicPlayer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
    public partial class MainWindow : FluentWindow
{
        private readonly IMainViewModel _mainViewModel;
        
        /// <summary>
        /// 公开MainViewModel以便外部访问
        /// </summary>
        public IMainViewModel ViewModel => _mainViewModel;
        private readonly WindowManagerService _windowManagerService;
        private readonly NavigationService _navigationService;
        private readonly IConfigurationService _configurationService;
        private readonly IMessagingService _messagingService;
        private readonly INotificationService _notificationService;
        //private readonly IBackgroundViewModel _backgroundViewModel;
        private readonly IPlaylistViewModel _playlistViewModel;
        private readonly IControlBarViewModel _controlBarViewModel;
    private readonly ISettingsBarViewModel _settingsBarViewModel; 
        private readonly ITitleBarViewModel _titleBarViewModel;

    public MainWindow(
        IMainViewModel mainViewModel,
        NavigationService navigationService,
        IConfigurationService configurationService,
        IMessagingService messagingService,
        INotificationService notificationService,
        //IBackgroundViewModel backgroundViewModel,
        IPlaylistViewModel playlistViewModel,
        IControlBarViewModel controlBarViewModel,
        ITitleBarViewModel titleBarViewModel,
        ISettingsBarViewModel settingsBarViewModel,
        WindowManagerService windowManagerService)
    {
        InitializeComponent();
        
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        //_backgroundViewModel = backgroundViewModel ?? throw new ArgumentNullException(nameof(backgroundViewModel));
        _playlistViewModel = playlistViewModel ?? throw new ArgumentNullException(nameof(playlistViewModel));
        _controlBarViewModel = controlBarViewModel ?? throw new ArgumentNullException(nameof(controlBarViewModel));
        _titleBarViewModel = titleBarViewModel ?? throw new ArgumentNullException(nameof(titleBarViewModel));
        _settingsBarViewModel = settingsBarViewModel ?? throw new ArgumentNullException(nameof(settingsBarViewModel));
        // 初始化窗口管理服务（使用依赖注入的单例实例）- 确保在ViewModel初始化前设置窗口
        _windowManagerService = windowManagerService ?? throw new ArgumentNullException(nameof(windowManagerService));
        System.Diagnostics.Debug.WriteLine($"MainWindow: _windowManagerService is null: {_windowManagerService == null}");
        System.Diagnostics.Debug.WriteLine($"MainWindow: this window is null: {this == null}");
        _windowManagerService.SetWindow(this);
        System.Diagnostics.Debug.WriteLine("MainWindow: WindowManagerService.SetWindow called");
        
        // 直接设置DataContext，通过构造函数注入
        this.DataContext = _mainViewModel;
        
        // 初始化ViewModels
        InitializeViewModels();
        
        // 初始化Controls（通过构造函数注入ViewModels）
        InitializeControls();
        
        // 从配置中加载主题设置
        LoadThemeFromConfiguration();
        
        // 初始化系统托盘
        InitializeSystemTray();
        
        // 初始化导航服务
        InitializeNavigation();
        
        // 注册导航消息处理器
        RegisterNavigationMessageHandler();
        
        // 导航到首页
        _navigationService.NavigateToPlaylist();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        try
        {
            base.OnStateChanged(e);
            
            // 确保在UI线程上执行
            if (Dispatcher.CheckAccess())
            {
                _windowManagerService.OnWindowStateChanged();
            }
            else
            {
                Dispatcher.Invoke(() => _windowManagerService.OnWindowStateChanged());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"窗口状态变化处理失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            base.OnClosing(e);
            
            // 检查配置中的关闭行为
            var closeBehavior = _configurationService.CurrentConfiguration.CloseBehavior;
            
            // 如果配置为最小化到托盘
            if (closeBehavior)
            {
                // 如果用户按住Shift键点击关闭按钮，则强制退出
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    System.Diagnostics.Debug.WriteLine("用户按住Shift键关闭窗口，直接退出");
                    // 关闭歌词窗口
                    CloseLyricsWindow();
                    // 不取消关闭，允许退出
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("最小化到托盘");
                    // 取消关闭事件
                    e.Cancel = true;
                    // 最小化到托盘
                    _windowManagerService.MinimizeToTray();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("配置为直接退出");
                // 系统托盘未启用，关闭歌词窗口
                CloseLyricsWindow();
                // 不取消关闭，允许退出
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"窗口关闭事件处理失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // 注销消息处理器
        _windowManagerService.Unregister();
        
        // 如果系统托盘未启用，确保完全退出应用程序
        if (!_configurationService.CurrentConfiguration.CloseBehavior)
        {
            System.Diagnostics.Debug.WriteLine("系统托盘未启用，确保完全退出应用程序");
            Application.Current.Shutdown();
        }
    }
    
    /// <summary>
    /// 关闭歌词窗口
    /// </summary>
    private void CloseLyricsWindow()
    {
        try
        {
            // 查找并关闭歌词窗口
            var lyricsWindow = Application.Current.Windows.OfType<LyricsWindow>().FirstOrDefault();
            if (lyricsWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("关闭歌词窗口");
                lyricsWindow.Close();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"关闭歌词窗口失败: {ex.Message}");
        }
    }

  
    
    
    /// <summary>
    /// 初始化导航服务
    /// </summary>
    private void InitializeNavigation()
    {
        // 设置主框架
        _navigationService.SetMainFrame(MainFrame);
    }

    /// <summary>
    /// 注册导航消息处理器
    /// </summary>
    private void RegisterNavigationMessageHandler()
    {
        // 注册导航到设置页面的消息处理器
        _messagingService.Register<NavigateToSettingsMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateToSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到设置页面失败: {ex.Message}");
            }
        });
        
        // 注册导航到主页的消息处理器
        _messagingService.Register<NavigateToHomeMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateToPlaylist();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到主页失败: {ex.Message}");
            }
        });
        
        // 注册导航到歌手页面的消息处理器
        _messagingService.Register<NavigateToSingerPageMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateToSinger();
                m.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到歌手页面失败: {ex.Message}");
                m.Reply(false);
            }
        });
        
        // 注册导航到专辑页面的消息处理器
        _messagingService.Register<NavigateToAlbumPageMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateToAlbum();
                m.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到专辑页面失败: {ex.Message}");
                m.Reply(false);
            }
        });
        
        // 注册导航到歌单页面的消息处理器
        _messagingService.Register<NavigateToHeartMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateToHeart();
                m.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到歌单页面失败: {ex.Message}");
                m.Reply(false);
            }
        });
        
        // 注册导航到歌单详情页面的消息处理器
        _messagingService.Register<NavigateToPlaylistDetailMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateToPlaylistDetail();
                m.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到歌单详情页面失败: {ex.Message}");
                m.Reply(false);
            }
        });
        
        // 注册返回上一页的消息处理器
        _messagingService.Register<GoBackMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.GoBack();
                m.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"返回上一页失败: {ex.Message}");
                m.Reply(false);
            }
        });
        
        // 注册显示桌面歌词的消息处理器
        _messagingService.Register<ShowLyricsMessage>(this, (r, m) =>
        {
            try
            {
                ShowLyricsWindow();
                m.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示桌面歌词失败: {ex.Message}");
                m.Reply(false);
            }
        });
        
        // 注册通用页面导航消息处理器
        _messagingService.Register<NavigateToPageMessage>(this, (r, m) =>
        {
            try
            {
                _navigationService.NavigateTo(m.PageUri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到页面失败: {m.PageUri}, 错误: {ex.Message}");
            }
        });
        
        // 注册主题切换消息处理器
        _messagingService.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            try
            {
                // 更新窗口背景类型
                UpdateWindowBackdrop(m.Theme);
                System.Diagnostics.Debug.WriteLine($"主题已切换为: {m.Theme}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"切换主题失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    private void InitializeSystemTray()
    {
        // 初始化通知系统（包括系统托盘）
        _notificationService.Initialize();
    }
    
    /// <summary>
    /// 初始化所有ViewModels
    /// </summary>
    private void InitializeViewModels()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainWindow: 开始初始化ViewModels");
            
            // 初始化各个ViewModel
            _mainViewModel.Initialize();
            //_backgroundViewModel.Initialize();
            _playlistViewModel.Initialize();
            _controlBarViewModel.Initialize();
            _titleBarViewModel.Initialize();
            _settingsBarViewModel.Initialize();


            System.Diagnostics.Debug.WriteLine("MainWindow: ViewModels初始化完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow: ViewModels初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化所有Controls的DataContext
    /// </summary>
    private void InitializeControls()
    {
        // 使用构造函数注入的ViewModels设置Controls的DataContext
        //this.BackgroundControl.DataContext = _backgroundViewModel;
        this.SettingsBarControl.DataContext = _settingsBarViewModel;
        this.ControlBarControl.DataContext = _controlBarViewModel;
        this.TitleBarControl.DataContext = _titleBarViewModel;
    }
    
/// <summary>
        /// 从配置中加载主题设置
        /// </summary>
        private void LoadThemeFromConfiguration()
        {
            try
            {
                var theme = _configurationService.CurrentConfiguration.Theme;
                UpdateWindowBackdrop(theme);
                System.Diagnostics.Debug.WriteLine($"从配置中加载主题: {theme}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载主题失败: {ex.Message}");
                // 使用默认主题
                UpdateWindowBackdrop(MusicPlayer.Core.Enums.Theme.Mica);
            }
        }



        /// <summary>
        /// 从系统托盘恢复窗口
        /// </summary>
        public void RestoreFromTray()
        {
            _windowManagerService.RestoreFromTray();
        }
        
        /// <summary>
        /// 更新窗口背景类型
        /// </summary>
        private void UpdateWindowBackdrop(MusicPlayer.Core.Enums.Theme theme)
        {
            // 确保在UI线程上执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateWindowBackdrop(theme));
                return;
            }
            
            // 根据主题枚举值设置WindowBackdropType
            switch (theme)
            {
                case MusicPlayer.Core.Enums.Theme.None:
                    this.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.None;
                    break;
                case MusicPlayer.Core.Enums.Theme.Acrylic:
                    this.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Acrylic;
                    break;
                case MusicPlayer.Core.Enums.Theme.Mica:
                    this.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Mica;
                    break;
                case MusicPlayer.Core.Enums.Theme.MicaAlt:
                    this.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Tabbed;
                    break;
                default:
                    this.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Mica;
                    break;
            }
        }
        
        /// <summary>
        /// 显示桌面歌词窗口
        /// </summary>
        private void ShowLyricsWindow()
        {
            try
            {
                // 检查是否已有歌词窗口打开
                var existingWindow = Application.Current.Windows.OfType<LyricsWindow>().FirstOrDefault();
                
                // 如果已有窗口打开，直接激活它
                if (existingWindow != null)
                {
                    existingWindow.Activate();
                    return;
                }
                
                // 创建新的歌词窗口和ViewModel
                var app = App.Current as App;
                var factory = app?.ServiceProvider?.GetService<ILyricsViewModelFactory>();
                var messagingService = app?.ServiceProvider?.GetService<IMessagingService>();
                
                if (factory != null && messagingService != null)
                {
                    // 每次都创建新的LyricsViewModel实例
                    var lyricsViewModel = factory.CreateLyricsViewModel();
                    var lyricsWindow = new LyricsWindow(lyricsViewModel, messagingService);
                    lyricsWindow.Show();
                    
                    System.Diagnostics.Debug.WriteLine("创建新的歌词窗口和ViewModel实例");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("无法获取必要的服务来创建歌词窗口");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示歌词窗口失败: {ex.Message}");
            }
        }
    }
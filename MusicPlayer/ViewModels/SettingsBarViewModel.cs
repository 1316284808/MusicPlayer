using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using MusicPlayer.Page;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 导航项类
    /// </summary>
    public class NavigationItem
    {
        public PageEnums Page { get; set; }
        public string Name { get; set; }
        public string IconKind { get; set; }
        public string SelectedIconKind { get; set; }
    }

    /// <summary>
    /// 设置栏视图模型
    /// </summary>
    public class SettingsBarViewModel : ObservableObject, ISettingsBarViewModel
    {
        private readonly IMessagingService _messagingService;
        
        private bool _areOtherButtonsVisible = true;
        private int _currentFilterMode = 0; // 0=全部，1=收藏
        private bool _areAllButtonsVisible = true; // 控制所有按钮的可见性
        private double _buttonsOpacity = 1.0; // 控制所有按钮的透明度
        private System.Timers.Timer _hideButtonsTimer; // 隐藏按钮的计时器
        
        // 导航项集合
        private ObservableCollection<NavigationItem> _navigationItems;
        private NavigationItem _selectedNavigationItem;

        /// <summary>
        /// 导航项集合
        /// </summary>
        public ObservableCollection<NavigationItem> NavigationItems
        {
            get => _navigationItems;
            set
            {
                if (_navigationItems != value)
                {
                    _navigationItems = value;
                    OnPropertyChanged(nameof(NavigationItems));
                }
            }
        }

        /// <summary>
        /// 选中的导航项
        /// </summary>
        public NavigationItem SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                if (_selectedNavigationItem != value)
                {
                    _selectedNavigationItem = value;
                    OnPropertyChanged(nameof(SelectedNavigationItem));
                    // 当选中项变化时，执行导航
                    if (_selectedNavigationItem != null)
                    {
                        ExecuteNavigate(_selectedNavigationItem);
                    }
                }
            }
        }

        /// <summary>
        /// 控制其他按钮是否可见
        /// </summary>
        public bool AreOtherButtonsVisible
        {
            get => _areOtherButtonsVisible;
            set
            {
                if (_areOtherButtonsVisible != value)
                {
                    _areOtherButtonsVisible = value;
                    OnPropertyChanged(nameof(AreOtherButtonsVisible));
                }
            }
        }
        
        /// <summary>
        /// 当前过滤模式：0=全部，1=收藏
        /// </summary>
        public int CurrentFilterMode
        {
            get => _currentFilterMode;
            set
            {
                if (_currentFilterMode != value)
                {
                    _currentFilterMode = value;
                    OnPropertyChanged(nameof(CurrentFilterMode));
                }
            }
        }

        /// <summary>
        /// 收纳按钮
        /// </summary>
        public ICommand ToggleButtonsVisibilityCommand { get; }
        
        /// <summary>
        /// 导航命令
        /// </summary>
        public ICommand NavigateCommand { get; }
        
        /// <summary>
        /// 鼠标进入命令
        /// </summary>
        public ICommand MouseEnterCommand { get; }
        
        /// <summary>
        /// 鼠标离开命令
        /// </summary>
        public ICommand MouseLeaveCommand { get; }

        /// <summary>
        /// 控制所有按钮是否可见
        /// </summary>
        public bool AreAllButtonsVisible
        {
            get => _areAllButtonsVisible;
            set
            {
                if (_areAllButtonsVisible != value)
                {
                    _areAllButtonsVisible = value;
                    OnPropertyChanged(nameof(AreAllButtonsVisible));
                    
                    // 同时更新按钮透明度
                    ButtonsOpacity = _areAllButtonsVisible ? 1.0 : 0.0;
                }
            }
        }

        /// <summary>
        /// 控制所有按钮的透明度
        /// </summary>
        public double ButtonsOpacity
        {
            get => _buttonsOpacity;
            set
            {
                if (Math.Abs(_buttonsOpacity - value) > 0.01)
                {
                    _buttonsOpacity = value;
                    OnPropertyChanged(nameof(ButtonsOpacity));
                }
            }
        }

        /// <summary>
        /// 执行切换按钮可见性操作
        /// </summary>
        private void ExecuteToggleButtonsVisibility()
        {
            AreOtherButtonsVisible = !AreOtherButtonsVisible;
            
            // 如果文本被隐藏，启动计时器，3秒后隐藏所有按钮
            if (!AreOtherButtonsVisible)
            {
                _hideButtonsTimer.Stop();
                _hideButtonsTimer.Start();
                System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 文本隐藏，启动3秒自动隐藏计时器");
            }
            else
            {
                // 如果文本显示，立即显示所有按钮并停止计时器
                _hideButtonsTimer.Stop();
                AreAllButtonsVisible = true;
                System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 文本显示，永久显示所有按钮");
            }
        }
        
        /// <summary>
        /// 计时器事件处理
        /// </summary>
        private void HideButtonsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 确保在UI线程上执行
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AreAllButtonsVisible = false;
                System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 3秒计时器到期，隐藏所有按钮");
            });
        }
        
        /// <summary>
        /// 执行导航操作
        /// </summary>
        /// <param name="navigationItem">导航项</param>
        private void ExecuteNavigate(NavigationItem navigationItem)
        {
            if (navigationItem == null) return;
            
            // 根据导航项类型执行不同的导航逻辑
            switch (navigationItem.Page)
            {
                case PageEnums.PlaylistPage:
                    // 导航到主页
                    _messagingService?.Send<NavigateToHomeMessage, bool>(new NavigateToHomeMessage());
                    
                    // 默认列表
                    CurrentFilterMode = 0;
                    _messagingService?.Send<ShowAllSongsMessage, bool>(new ShowAllSongsMessage());
                    System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到默认列表");
                    break;
                case PageEnums.HeartPage:
                    // 导航到歌单页面
                    _messagingService?.Send<NavigateToHeartMessage, bool>(new NavigateToHeartMessage());
                    System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到歌单页面");
                    break;
                case PageEnums.SingerPage:
                    _messagingService?.Send<NavigateToSingerPageMessage, bool>(new NavigateToSingerPageMessage());
                    System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到歌手页面");
                    break;
                case PageEnums.AlbumPage:
                    _messagingService?.Send<NavigateToAlbumPageMessage, bool>(new NavigateToAlbumPageMessage());
                    System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到专辑页面");
                    break;
                case PageEnums.SettingsPage:
                    _messagingService?.Send<NavigateToSettingsMessage, bool>(new NavigateToSettingsMessage());
                    System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到设置页面");
                    break;
            }
        }
        
        /// <summary>
        /// 执行鼠标进入操作
        /// </summary>
        private void ExecuteMouseEnter()
        {
            // 当鼠标进入时，停止计时器并显示所有按钮
            _hideButtonsTimer.Stop();
            AreAllButtonsVisible = true;
        }
        
        /// <summary>
        /// 执行鼠标离开操作
        /// </summary>
        private void ExecuteMouseLeave()
        {
            // 只有在文本隐藏状态下才启动计时器，3秒后隐藏所有按钮
            if (!AreOtherButtonsVisible)
            {
                _hideButtonsTimer.Stop();
                _hideButtonsTimer.Start();
            }
        }
        
        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public override void Initialize()
        {
            // SettingsBarViewModel的初始化逻辑
        }
        
        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 注销消息处理器
            if (_messagingService != null)
            {
                _messagingService.Unregister(this);
            }
            
            // 停止并释放计时器
            if (_hideButtonsTimer != null)
            {
                _hideButtonsTimer.Stop();
                _hideButtonsTimer.Dispose();
            }
        }
        
        /// <summary>
        /// 处理导航完成消息
        /// </summary>
        private void OnNavigationCompleted(object recipient, NavigationCompletedMessage message)
        {
            // 可以在这里添加导航完成后的处理逻辑
            System.Diagnostics.Debug.WriteLine($"SettingsBarViewModel: 导航完成，目标页面: {message.Value}");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messagingService">消息服务</param>
        public SettingsBarViewModel(IMessagingService messagingService)
        {
            // 初始化计时器
            _hideButtonsTimer = new System.Timers.Timer(3000); // 3秒
            _hideButtonsTimer.Elapsed += HideButtonsTimer_Elapsed;
            _hideButtonsTimer.AutoReset = false;

            // 初始化导航项
            InitializeNavigationItems();
            
            // 初始化命令
            ToggleButtonsVisibilityCommand = new RelayCommand(ExecuteToggleButtonsVisibility);
            NavigateCommand = new RelayCommand<NavigationItem>(ExecuteNavigate);
            MouseEnterCommand = new RelayCommand(ExecuteMouseEnter);
            MouseLeaveCommand = new RelayCommand(ExecuteMouseLeave);
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            
            // 设置默认选中的导航项
            SelectedNavigationItem = NavigationItems[0];
            
            // 订阅导航完成消息
            _messagingService.Register<NavigationCompletedMessage>(this, OnNavigationCompleted);
        }

        /// <summary>
        /// 初始化导航项
        /// </summary>
        private void InitializeNavigationItems()
        {
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Page = PageEnums.PlaylistPage, Name = "全部歌曲", IconKind = "List", SelectedIconKind = "InList" },
                new NavigationItem { Page = PageEnums.HeartPage, Name = "音乐库", IconKind = "HeartFill", SelectedIconKind = "Heart" },
                new NavigationItem { Page = PageEnums.SingerPage, Name = "歌手列表", IconKind = "Person", SelectedIconKind = "InPerson" },
                new NavigationItem { Page = PageEnums.AlbumPage, Name = "专辑列表", IconKind = "Album", SelectedIconKind = "InAlbum" },
                new NavigationItem { Page = PageEnums.SettingsPage, Name = "设置", IconKind = "Settings", SelectedIconKind = "InSettings" }
            };
        }
    }
}
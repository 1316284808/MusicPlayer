using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using MusicPlayer.Page;

namespace MusicPlayer.ViewModels
{
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
        private bool _isDefaultListSelected = true; // 默认列表是否选中
        private bool _isFavoriteListSelected = false; // 收藏列表是否选中
        private bool _isSettingsSelected = false; // 设置是否选中
        private System.Timers.Timer _hideButtonsTimer; // 隐藏按钮的计时器
        
        // 直接控制每个导航项的图标状态
        private bool _defaultListIconState = true;  // true表示开，false表示关
        private bool _favoriteListIconState = false;
        private bool _settingsIconState = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messagingService">消息服务</param>
        public SettingsBarViewModel(IMessagingService messagingService)
        {
            // 初始化命令
            ToggleButtonsVisibilityCommand = new RelayCommand(ExecuteToggleButtonsVisibility);
            NavigateToDefaultListCommand = new RelayCommand(ExecuteNavigateToDefaultList);
            NavigateToFavoriteListCommand = new RelayCommand(ExecuteNavigateToFavoriteList);
            NavigateToSettingsCommand = new RelayCommand(ExecuteNavigateToSettings);
            MouseEnterCommand = new RelayCommand(ExecuteMouseEnter);
            MouseLeaveCommand = new RelayCommand(ExecuteMouseLeave);
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            
            // 初始化计时器
            _hideButtonsTimer = new System.Timers.Timer(3000); // 3秒
            _hideButtonsTimer.Elapsed += HideButtonsTimer_Elapsed;
            _hideButtonsTimer.AutoReset = false;

            // 设置默认选中的导航项
            UpdateIconsByIndex(0); // 默认选中第一个导航项
            
            // 订阅导航完成消息
            _messagingService.Register<NavigationCompletedMessage>(this, OnNavigationCompleted);
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
        /// 默认列表是否选中
        /// </summary>
        public bool IsDefaultListSelected
        {
            get => _isDefaultListSelected;
            set
            {
                if (_isDefaultListSelected != value)
                {
                    _isDefaultListSelected = value;
                    OnPropertyChanged(nameof(IsDefaultListSelected));
                }
            }
        }
        
        /// <summary>
        /// 收藏列表是否选中
        /// </summary>
        public bool IsFavoriteListSelected
        {
            get => _isFavoriteListSelected;
            set
            {
                if (_isFavoriteListSelected != value)
                {
                    _isFavoriteListSelected = value;
                    OnPropertyChanged(nameof(IsFavoriteListSelected));
                }
            }
        }
        
        /// <summary>
        /// 设置是否选中
        /// </summary>
        public bool IsSettingsSelected
        {
            get => _isSettingsSelected;
            set
            {
                if (_isSettingsSelected != value)
                {
                    _isSettingsSelected = value;
                    OnPropertyChanged(nameof(IsSettingsSelected));
                }
            }
        }

        /// <summary>
        /// 默认列表图标状态
        /// </summary>
        public bool DefaultListIconState
        {
            get => _defaultListIconState;
            set
            {
                if (_defaultListIconState != value)
                {
                    _defaultListIconState = value;
                    OnPropertyChanged(nameof(DefaultListIconState));
                }
            }
        }

        /// <summary>
        /// 收藏列表图标状态
        /// </summary>
        public bool FavoriteListIconState
        {
            get => _favoriteListIconState;
            set
            {
                if (_favoriteListIconState != value)
                {
                    _favoriteListIconState = value;
                    OnPropertyChanged(nameof(FavoriteListIconState));
                }
            }
        }

        /// <summary>
        /// 设置图标状态
        /// </summary>
        public bool SettingsIconState
        {
            get => _settingsIconState;
            set
            {
                if (_settingsIconState != value)
                {
                    _settingsIconState = value;
                    OnPropertyChanged(nameof(SettingsIconState));
                }
            }
        }

        /// <summary>
        /// 收纳按钮
        /// </summary>
        public ICommand ToggleButtonsVisibilityCommand { get; }
        
        /// <summary>
        /// 导航到默认列表命令
        /// </summary>
        public ICommand NavigateToDefaultListCommand { get; }
        
        /// <summary>
        /// 导航到收藏列表命令
        /// </summary>
        public ICommand NavigateToFavoriteListCommand { get; }
        
        /// <summary>
        /// 导航到设置页面命令
        /// </summary>
        public ICommand NavigateToSettingsCommand { get; }
        
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
        /// 执行导航到默认列表操作
        /// </summary>
        private void ExecuteNavigateToDefaultList()
        {
            // 更新图标状态
            UpdateIconsByIndex(0);
            
        
            // 更新过滤模式
            CurrentFilterMode = 0;
            
            // 导航到主页
            _messagingService?.Send<NavigateToHomeMessage, bool>(new NavigateToHomeMessage());
            
            // 发送显示所有歌曲的消息
            _messagingService?.Send<ShowAllSongsMessage, bool>(new ShowAllSongsMessage());
            
            System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到默认列表");
        }
        
        /// <summary>
        /// 执行导航到收藏列表操作
        /// </summary>
        private void ExecuteNavigateToFavoriteList()
        {
            // 更新图标状态
            UpdateIconsByIndex(1);
            
         
            
            // 更新过滤模式
            CurrentFilterMode = 1;
            
            // 导航到主页
            _messagingService?.Send<NavigateToHomeMessage, bool>(new NavigateToHomeMessage());
            
            // 发送过滤收藏歌曲的消息
            _messagingService?.Send<FilterFavoriteSongsMessage, bool>(new FilterFavoriteSongsMessage());
            
            System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 导航到收藏列表");
        }
        
        /// <summary>
        /// 执行导航到设置页面操作
        /// </summary>
        private void ExecuteNavigateToSettings()
        {
            // 更新图标状态
            UpdateIconsByIndex(2);
            
         
            
            _messagingService?.Send<NavigateToSettingsMessage, bool>(new NavigateToSettingsMessage());
        }
        
        /// <summary>
        /// 根据选中项的下标更新所有导航项的图标状态
        /// </summary>
        private void UpdateIconsByIndex(int selectedIndex)
        { 
            if (selectedIndex == 0)
            {
                DefaultListIconState = true;   // 开
                FavoriteListIconState = false;  // 关
                SettingsIconState = false;      // 关
                return;
            }
         if (selectedIndex == 1)
            {
                DefaultListIconState = false;   // 关
                FavoriteListIconState = true;   // 开
                SettingsIconState = false;      // 关
                return;
            }
           if (selectedIndex == 2)
            {
                DefaultListIconState = false;   // 关
                FavoriteListIconState = false;  // 关
                SettingsIconState = true;       // 开
                return;
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
            System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 鼠标进入，显示所有按钮");
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
                System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 鼠标离开，启动3秒隐藏计时器");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SettingsBarViewModel: 鼠标离开，但文本可见，不启动计时器");
            }
        }
        
        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public void Initialize()
        {
            // SettingsBarViewModel的初始化逻辑
        }
        
        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public void Cleanup()
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
            var pageType = message.Value;
            System.Diagnostics.Debug.WriteLine($"SettingsBarViewModel: 收到导航完成消息，页面类型: {pageType?.Name}");
            
            // 根据页面类型更新图标状态
            if (pageType == typeof(HomePage))
            {
                // 根据当前过滤模式判断是默认列表还是收藏列表
                if (CurrentFilterMode == 0)
                {
                    // 默认列表
                    UpdateIconsByIndex(0);
                }
                else
                {
                    // 收藏列表
                    UpdateIconsByIndex(1);
                }
            }
            else if (pageType == typeof(SettingsPage))
            {
                // 设置页面
                UpdateIconsByIndex(2);
            }
            else if (pageType == typeof(PlayerPage))
            {
                // 播放页面，不更新图标状态，因为播放页面不在导航栏中
                // 这里可以选择将所有图标状态设置为false
                DefaultListIconState = false;
                FavoriteListIconState = false;
                SettingsIconState = false;
            }
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Models;
using MusicPlayer.ViewModels;
using MusicPlayer.Services;
using MusicPlayer.Services.Handlers;
using MusicPlayer.Services.Services;
using MusicPlayer.Services.Providers;
 
using MusicPlayer.Core.Interface;
using MusicPlayer.Helper;
using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Enums;
using System.Linq;
using MusicPlayer.Navigation;

namespace MusicPlayer.Config
{
    /// <summary>
    /// 服务集合扩展方法
    /// 提供便捷的依赖注入配置，采用分层注册策略解决循环依赖
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加所有应用服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddMusicPlayerServices(this IServiceCollection services)
        {
            // 日志配置 - 只记录错误信息
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Error);
            });
            services.AddMessageServices();
            services.AddCoreServices();
            services.AddSingleton<IViewModelLifecycleManager, ViewModelLifecycleManager>();
            services.AddViewModels();

            return services;
        }

        /// <summary>
        /// 核心业务服务 
        /// 注意：必须先添加基础服务，再添加业务服务，最后添加协调器服务
        /// 确保依赖关系正确，避免重复创建实例
        /// </summary>
        private static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // 第一步：添加基础服务（无依赖的服务）
            services.AddInfrastructureServices();
            
            // 第二步：添加业务服务（依赖基础服务）
            services.AddBusinessServices();
            
            // 第三步：添加协调器服务（依赖业务服务）
            services.AddCoordinatorServices();
            
            return services;
        }

        /// <summary>
        /// 注册基础服务
        /// </summary>
        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // 播放列表缓存服务 - 全部使用单例模式
            services.AddSingleton<IPlaylistCacheService, PlaylistCacheService>();
            services.AddSingleton<IPlaylistService>(provider => new PlaylistService(
                provider.GetRequiredService<IConfigurationService>()));
            services.AddSingleton<IPlaylistDataService>(provider => {
                var instance = new PlaylistDataService(
                    provider.GetRequiredService<IPlaylistCacheService>(),
                    provider.GetRequiredService<IPlaylistService>(),
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IDispatcherService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<IPlaybackContextService>());
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            // 播放上下文服务 - 单例模式
            services.AddSingleton<IPlaybackContextService, PlaybackContextService>();
            
            // 播放上下文提供者 - 单例模式
            services.AddSingleton<IPlaybackContextProvider, DefaultPlaylistProvider>();
            services.AddSingleton<FavoritesProvider>();
            services.AddSingleton<ArtistProvider>();
            services.AddSingleton<AlbumProvider>();
            services.AddSingleton<CustomPlaylistProvider>();
            
            // 自定义歌单服务 - 单例模式
            services.AddSingleton<ICustomPlaylistService, CustomPlaylistService>(provider => new CustomPlaylistService(
                new PlaylistDataDAL(Paths.PlaylistDatabasePath)));
            
            // 消息服务必须在这里注册，因为PlayerStateService需要它
            services.AddSingleton<IMessagingService, MessagingService>();
            services.AddSingleton<IMessenger>(provider => WeakReferenceMessenger.Default);
            
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<ISystemTrayService, SystemTrayService>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            
            // 手动注册WPF-UI服务
            services.AddSingleton<Wpf.Ui.ISnackbarService, Wpf.Ui.SnackbarService>();
            services.AddSingleton<Wpf.Ui.IContentDialogService, Wpf.Ui.ContentDialogService>();
            
            services.AddSingleton<IUINotificationService, UINotificationService>();
            services.AddSingleton<IDialogService, WpfDialogService>();
            
            // 系统级服务 - 使用单例模式
            services.AddSingleton<ISystemMediaTransportService, SystemMediaTransportService>();
            services.AddSingleton<MemoryMonitorService>(provider => new MemoryMonitorService(500)); // 500MB 阈值
            
            return services;
        }

        /// <summary>
        /// 注册业务服务 
        /// 全部使用单例模式，确保实例唯一性
        /// 注册顺序很重要，确保依赖关系正确
        /// </summary>
        private static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // 1. 先注册ConfigurationService为单例，但不注入PlayerStateService
            services.AddSingleton<IConfigurationService>(provider => {
                var instance = new ConfigurationService(null);
                System.Diagnostics.Debug.WriteLine($"ConfigurationService: 创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            // 2. 注册PlayerStateService为单例，它依赖于 PlaylistDataService, ConfigurationService, MessagingService 和 PlaybackContextService
            services.AddSingleton<IPlayerStateService>(provider => {
                var instance = new PlayerStateService(
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IPlaybackContextService>());
                System.Diagnostics.Debug.WriteLine($"PlayerStateService: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                
                // 在PlayerStateService创建后，设置ConfigurationService的PlayerStateService引用
                var configService = (ConfigurationService)provider.GetRequiredService<IConfigurationService>();
                configService.SetPlayerStateService(instance);
                
                return instance;
            });
            services.AddSingleton<PlayerStateService>(provider => 
                (PlayerStateService)provider.GetRequiredService<IPlayerStateService>());
            
            // 3. 注册EqualizerPresetRepository为单例
            services.AddSingleton<IEqualizerPresetRepository>(provider => {
                var instance = new EqualizerPresetRepository();
                System.Diagnostics.Debug.WriteLine($"EqualizerPresetRepository: 创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            // 4. 注册EqualizerService为单例（注入EqualizerPresetRepository）
            services.AddSingleton<IEqualizerService>(provider => {
                var presetRepository = provider.GetRequiredService<IEqualizerPresetRepository>();
                var instance = new EqualizerService(presetRepository);
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            // 4. 注册PlayerService为单例，它依赖于多个服务
            services.AddSingleton<IPlayerService>(provider => {
                var logger = provider.GetService<Microsoft.Extensions.Logging.ILogger<PlayerService>>();
                var instance = new PlayerService(
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaylistService>(),
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<INotificationService>(),
                    provider.GetRequiredService<ISystemMediaTransportService>(),
                    provider.GetRequiredService<IEqualizerService>(),
                    provider.GetRequiredService<IPlayerStateService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    logger);
                System.Diagnostics.Debug.WriteLine($"PlayerService: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            services.AddSingleton<PlayerService>(provider => 
                (PlayerService)provider.GetRequiredService<IPlayerService>());
            
            // 4. 最后注册NotificationService为单例，使用工厂模式解决循环依赖
            services.AddSingleton<INotificationService>(provider => 
                new NotificationService(
                    () => provider.GetRequiredService<IPlayerService>(),
                    () => provider.GetRequiredService<IPlaylistDataService>(),
                    () => provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IUINotificationService>(),
                    provider.GetRequiredService<IDispatcherService>(),
                    provider.GetRequiredService<ISystemTrayService>(),
                    provider.GetService<ILogger<NotificationService>>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance));
            
            return services;
        }

        /// <summary>
        /// 注册协调器服务 - 全部使用单例模式
        /// </summary>
        private static IServiceCollection AddCoordinatorServices(this IServiceCollection services)
        {
            // 命令处理器 - 单例模式
            services.AddSingleton<IPlaylistCommandHandler, PlaylistCommandHandler>();
            
            // 窗口管理服务 - 单例模式
            services.AddSingleton<WindowManagerService>();
            
            // 导航服务 - 单例模式
            services.AddSingleton<NavigationService>();
            
            // 服务协调器 - 使用单例模式，确保实例唯一性
            // 使用工厂方法确保只创建一个实例
            services.AddSingleton<IServiceCoordinator>(provider => {
                var instance = new ServiceCoordinator(
                    provider.GetRequiredService<IPlayerService>(),
                    provider.GetRequiredService<IPlayerStateService>(),
                    provider.GetRequiredService<INotificationService>(),
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaylistCacheService>(),
                    provider.GetService<ILogger<ServiceCoordinator>>());
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            services.AddSingleton<ServiceCoordinator>(provider => 
                (ServiceCoordinator)provider.GetRequiredService<IServiceCoordinator>());
            
            return services;
        }

        /// <summary>
        /// 添加ViewModel - 全部使用单例模式
        /// </summary>
        private static IServiceCollection AddViewModels(this IServiceCollection services)
        { 
            // 所有ViewModel注册为单例模式，确保实例唯一性
            services.AddSingleton<IControlBarViewModel>(provider => {
                var instance = new ControlBarViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IPlayerStateService>(),
                    provider.GetRequiredService<NavigationService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaylistViewModel>(),
                    provider.GetRequiredService<IPlaybackContextService>(),
                    provider.GetRequiredService<IPlaylistCacheService>());
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            services.AddSingleton<ITitleBarViewModel>(provider => {
                var instance = new TitleBarViewModel(
                    provider.GetRequiredService<IMessagingService>());
                System.Diagnostics.Debug.WriteLine($"TitleBarViewModel: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            services.AddSingleton<ICenterContentViewModel>(provider => {
                var instance = new CenterContentViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IConfigurationService>());
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 通过工厂创建单例实例，ID: {instance.GetHashCode()}");
                return instance;
            });
            
            // 注册歌词视图模型工厂，用于创建新的歌词视图模型实例
            services.AddSingleton<ILyricsViewModelFactory, LyricsViewModelFactory>();
            //  频谱
            services.AddSingleton<ISpectrumAnalyzerViewModel, SpectrumAnalyzerViewModel>();
            services.AddSingleton<ISpectrumAnalyzerManager, SpectrumAnalyzerManager>(); 
            // WindowSettingsViewModel 也改为单例模式
            services.AddSingleton<IWindowSettingsViewModel, WindowSettingsViewModel>();
            
            // SoundSettingsViewModel - 单例模式
            services.AddSingleton<ISoundSettingsViewModel, SoundSettingsViewModel>();
            
            services.AddSingleton<IPlaylistViewModel>(provider => 
                new PlaylistViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<ICustomPlaylistService>(),
                    provider.GetRequiredService<IPlaybackContextService>(),
                    provider.GetRequiredService<INotificationService>()));

            
            // SettingsPageViewModel - 单例模式
            services.AddSingleton<ISettingsPageViewModel, SettingsPageViewModel>(provider => 
                new SettingsPageViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IWindowSettingsViewModel>(),
                    provider.GetRequiredService<PlaylistSettingViewModel>(),
                    provider.GetRequiredService<ISoundSettingsViewModel>()));
            
            // SettingsBarViewModel - 单例模式
            services.AddSingleton<ISettingsBarViewModel, SettingsBarViewModel>(provider => 
                new SettingsBarViewModel(
                    provider.GetRequiredService<IMessagingService>()));
            
            // AlbumViewModel - 单例模式
            services.AddSingleton<IAlbumViewModel>(provider => 
                new AlbumViewModel(
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaybackContextService>(),
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<Navigation.NavigationService>()));
            
            // SingerViewModel - 单例模式
            services.AddSingleton<ISingerViewModel>(provider => 
                new SingerViewModel(
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaybackContextService>(),
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<Navigation.NavigationService>())); 
            
            // HeartViewModel - 单例模式
            services.AddSingleton<IHeartViewModel>(provider => 
                new HeartViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaybackContextService>(),
                    provider.GetRequiredService<IDialogService>(),
                    provider.GetRequiredService<IPlaylistCacheService>(),
                    provider.GetRequiredService<IPlaylistService>(),
                    provider.GetRequiredService<INotificationService>())); 
           
            // PlaylistDetailViewModel - 单例模式
            services.AddSingleton<IPlaylistDetailViewModel>(provider => 
                new PlaylistDetailViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IPlaybackContextService>(),
                    provider.GetRequiredService<ICustomPlaylistService>(),
                    provider.GetRequiredService<IPlaylistCacheService>()));
           
            // PlaylistSettingViewModel - 单例模式
            services.AddSingleton<PlaylistSettingViewModel>(provider => 
                new PlaylistSettingViewModel(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<IPlaylistDataService>(),
                    provider.GetRequiredService<IDispatcherService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<IDialogService>(),
                    provider.GetRequiredService<IUINotificationService>()));

            // MainViewModel - 单例模式
            services.AddSingleton<IMainViewModel>(provider => 
                new MainViewModel( 
                    provider.GetRequiredService<IControlBarViewModel>(),
                    provider.GetRequiredService<ITitleBarViewModel>(),
                    provider.GetRequiredService<ICenterContentViewModel>(),
                    provider.GetRequiredService<IPlaylistViewModel>(),
                    provider.GetRequiredService<IServiceCoordinator>(),
                    provider.GetRequiredService<WindowManagerService>()));

            return services;
        }

        /// <summary>
        /// 添加配置服务
        /// </summary>
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, Action<MusicPlayerOptions>? configureOptions = null)
        {
            // 配置选项
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<MusicPlayerOptions>(options =>
                {
                    // 默认配置
                    options.DefaultVolume = 0.5f;
                    options.DefaultPlayMode = PlayMode.RepeatAll;
                    options.EnableSpectrum = true;
                    options.AutoLoadLyrics = true;
                });
            }

            return services;
        }

        /// <summary>
        /// 添加消息服务 - 全部使用单例模式
        /// 注意：核心消息服务已在AddInfrastructureServices中注册
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddMessageServices(this IServiceCollection services)
        {
            // 所有消息处理器注册为单例模式
            services.AddSingleton<PlayerControlMessageHandler>(provider => new PlayerControlMessageHandler(
                provider.GetRequiredService<IMessagingService>(),
                provider.GetRequiredService<IPlayerStateService>(),
                provider.GetRequiredService<IPlayerService>(),
                provider.GetRequiredService<IPlaylistDataService>(),
                provider.GetRequiredService<IPlaybackContextService>()));
            services.AddSingleton<PlaylistMessageHandler>(); 
            services.AddSingleton<SystemMessageHandler>(provider =>
                new SystemMessageHandler(
                    provider.GetRequiredService<IMessagingService>(),
                    provider.GetRequiredService<WindowManagerService>(),
                    provider.GetRequiredService<IConfigurationService>(),
                    provider.GetRequiredService<INotificationService>(),
                    provider.GetRequiredService<IDispatcherService>())); 
            
            // 后台服务也使用单例模式
            services.AddSingleton<IHostedService, MessageHandlerInitializationService>();
            services.AddSingleton<IHostedService, LifecycleManagementService>();
            services.AddSingleton<LifecycleManagementService>();
            
            // 添加ServiceCoordinator初始化服务
            services.AddSingleton<IHostedService, ServiceCoordinatorInitializationService>();
            
            // 添加播放上下文初始化服务
            services.AddSingleton<IHostedService, PlaybackContextInitializationService>();

            return services;
        }
    }

    /// <summary>
    /// 消息处理器初始化服务
    /// 确保所有消息处理器在应用启动时正确注册
    /// </summary>
    public class MessageHandlerInitializationService : IHostedService, IDisposable
    {
        private readonly PlayerControlMessageHandler _playerHandler;
        private readonly PlaylistMessageHandler _playlistHandler;
        private readonly SystemMessageHandler _systemHandler;
        private readonly List<IDisposable> _handlers = new();
        private readonly object _lockObject = new();
        private bool _disposed = false;

        public MessageHandlerInitializationService(
            PlayerControlMessageHandler playerHandler,
            PlaylistMessageHandler playlistHandler,
            SystemMessageHandler systemHandler)
        {
            _playerHandler = playerHandler ?? throw new ArgumentNullException(nameof(playerHandler));
            _playlistHandler = playlistHandler ?? throw new ArgumentNullException(nameof(playlistHandler));
            _systemHandler = systemHandler ?? throw new ArgumentNullException(nameof(systemHandler));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("消息处理器已注册"); 
                lock (_lockObject)
                {
                    if (_disposed) return Task.CompletedTask;
                    
                    _handlers.Add(_playerHandler);
                    _handlers.Add(_playlistHandler);
                    _handlers.Add(_systemHandler);
                }

                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                //System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_disposed) return Task.CompletedTask; 
                    foreach (var handler in _handlers)
                    {
                        try
                        {
                            handler?.Dispose();
                        }
                        catch (Exception handlerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"释放消息处理器时出错: {handlerEx.Message}");
                        }
                    }
                    _handlers.Clear(); 
                } 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StopAsync  CancellationToken 出错 {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 调用StopAsync进行清理
                StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// MusicPlayer默认配置选项，用于初始化
    /// </summary>
    public class MusicPlayerOptions
    {
        /// <summary>
        /// 默认音量
        /// </summary>
        public float DefaultVolume { get; set; } = 0.5f;

        /// <summary>
        /// 默认播放模式
        /// </summary>
        public PlayMode DefaultPlayMode { get; set; } = PlayMode.RepeatAll;

        /// <summary>
        /// 是否启用频谱显示
        /// </summary>
        public bool EnableSpectrum { get; set; } = true;

        /// <summary>
        /// 是否自动加载歌词
        /// </summary>
        public bool AutoLoadLyrics { get; set; } = true;

        /// <summary>
        /// 歌词文件搜索路径
        /// </summary>
        public string[] LyricSearchPaths { get; set; } = { "{filename}.lrc", "{filename}.txt" };

        /// <summary>
        /// 音频文件支持的扩展名
        /// </summary>
        public string[] SupportedAudioExtensions { get; set; } =
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".mp4"
        };

        /// <summary>
        /// 缓存配置
        /// </summary>
        public CacheOptions Cache { get; set; } = new();

        /// <summary>
        /// UI配置
        /// </summary>
        public UIOptions UI { get; set; } = new();
    }

    /// <summary>
    /// 缓存配置选项
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// 最大缓存大小（MB）
        /// </summary>
        public int MaxCacheSizeMB { get; set; } = 100;

        /// <summary>
        /// 缓存过期时间（小时）
        /// </summary>
        public int CacheExpiryHours { get; set; } = 24;

        /// <summary>
        /// 是否启用专辑封面缓存
        /// </summary>
        public bool EnableAlbumArtCache { get; set; } = true;

        /// <summary>
        /// 专辑封面缓存路径
        /// </summary>
        public string AlbumArtCachePath { get; set; } = "Cache/AlbumArt";
    }

    /// <summary>
    /// UI配置选项
    /// </summary>
    public class UIOptions
    {
        /// <summary>
        /// 默认主题
        /// </summary>
        public string DefaultTheme { get; set; } = "Light";

        /// <summary>
        /// 默认语言
        /// </summary>
        public string DefaultLanguage { get; set; } = "zh-CN";

        /// <summary>
        /// 窗口默认宽度
        /// </summary>
        public double DefaultWindowWidth { get; set; } = 1000;

        /// <summary>
        /// 窗口默认高度
        /// </summary>
        public double DefaultWindowHeight { get; set; } = 700;

        /// <summary>
        /// 是否启用窗口记忆
        /// </summary>
        public bool EnableWindowMemory { get; set; } = true;

        /// <summary>
        /// 是否启用系统托盘
        /// </summary>
        public bool EnableSystemTray { get; set; } = true;

        /// <summary>
        /// 频谱分析器配置
        /// </summary>
        public SpectrumOptions Spectrum { get; set; } = new();
    }

    /// <summary>
    /// 频谱分析器配置选项
    /// </summary>
    public class SpectrumOptions
    {
        /// <summary>
        /// FFT大小
        /// </summary>
        public int FFTSize { get; set; } = 2048;

        /// <summary>
        /// 频段数量
        /// </summary>
        public int FrequencyBands { get; set; } = 32;

        /// <summary>
        /// 更新频率（毫秒）
        /// </summary>
        public int UpdateFrequencyMs { get; set; } = 50;

        /// <summary>
        /// 是否启用平滑处理
        /// </summary>
        public bool EnableSmoothing { get; set; } = true;

        /// <summary>
        /// 平滑因子
        /// </summary>
        public float SmoothingFactor { get; set; } = 0.8f;

        /// <summary>
        /// 是否启用对数刻度
        /// </summary>
        public bool EnableLogScale { get; set; } = true;
    }

    /// <summary>
    /// SystemMessageHandler实例持有者，用于跟踪SystemMessageHandler实例
    /// </summary>
    public class SystemMessageHolder
    {
        public SystemMessageHandler? SystemMessageHandler { get; set; }
    }

    /// <summary>
    /// ServiceCoordinator初始化服务
    /// 负责在主机启动时初始化ServiceCoordinator
    /// </summary>
    public class ServiceCoordinatorInitializationService : IHostedService
    {
        private readonly IServiceCoordinator _serviceCoordinator;
        private readonly ILogger<ServiceCoordinatorInitializationService>? _logger;

        public ServiceCoordinatorInitializationService(
            IServiceCoordinator serviceCoordinator,
            ILogger<ServiceCoordinatorInitializationService>? logger = null)
        {
            _serviceCoordinator = serviceCoordinator ?? throw new ArgumentNullException(nameof(serviceCoordinator));
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogError("[INFO] 开始初始化ServiceCoordinator");
                System.Diagnostics.Debug.WriteLine("ServiceCoordinatorInitializationService: 开始初始化ServiceCoordinator");

                // 只有在ServiceCoordinator未初始化时才进行初始化
                if (!_serviceCoordinator.IsInitialized)
                {
                    await _serviceCoordinator.InitializeAsync();
                    _logger?.LogError("[INFO] ServiceCoordinator初始化完成");
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinatorInitializationService: ServiceCoordinator初始化完成");
                }
                else
                {
                    _logger?.LogError("[INFO] ServiceCoordinator已经初始化，跳过");
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinatorInitializationService: ServiceCoordinator已经初始化，跳过");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ServiceCoordinator初始化失败");
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinatorInitializationService: ServiceCoordinator初始化失败: {ex.Message}");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogError("[INFO] 停止ServiceCoordinator初始化服务");
            System.Diagnostics.Debug.WriteLine("ServiceCoordinatorInitializationService: 停止ServiceCoordinator初始化服务");
            return Task.CompletedTask;
        }
    }
    
    
}
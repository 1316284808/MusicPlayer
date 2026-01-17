using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;

using MusicPlayer.Core.Interfaces;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 服务协调器实现 - 统一管理核心服务间的通信和依赖关系
    /// 通过Lazy<T>和协调器模式解决服务间的循环依赖问题
    /// </summary>
    public class ServiceCoordinator : IServiceCoordinator
    {
        private readonly ILogger<ServiceCoordinator>? _logger;
        private readonly object _lockObject = new();
        private bool _disposed = false;
        private bool _initialized = false;
        private readonly ConcurrentDictionary<string, object> _activeOperations = new();
        
        // 直接引用的服务，确保实例唯一
        public IPlayerService PlayerService { get; private set; } = null!;
        public IPlayerStateService PlayerStateService { get; private set; } = null!;
        public INotificationService NotificationService { get; private set; } = null!;
        public IMessagingService MessagingService { get; private set; } = null!;
        public IConfigurationService ConfigurationService { get; private set; } = null!;
        public IPlaylistDataService PlaylistDataService { get; private set; } = null!;
        public IPlaylistCacheService CacheService { get; private set; } = null!;

        /// <summary>
        /// 构造函数 - 接收所有必要的服务引用
        /// </summary>
        public ServiceCoordinator(
            IPlayerService playerService,
            IPlayerStateService playerStateService,
            INotificationService notificationService,
            IMessagingService messagingService,
            IConfigurationService configurationService,
            IPlaylistDataService playlistDataService,
            IPlaylistCacheService cacheService,
            ILogger<ServiceCoordinator>? logger = null)
        {
            // 添加实例ID日志，用于调试单例问题
            System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 创建新实例，ID: {GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: PlayerStateService实例ID: {playerStateService.GetHashCode()}");
            
            PlayerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            PlayerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            MessagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            ConfigurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            PlaylistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            CacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger;

            System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 开始订阅消息");
            // 订阅关键消息以协调服务间通信
            SubscribeToMessages();
            
            System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 构造函数执行完成");
        }

        /// <summary>
        /// 检查服务协调器是否已初始化
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("ServiceCoordinator: InitializeAsync 开始执行");
            System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 当前初始化状态: {_initialized}");

            // 使用双重检查锁定模式确保线程安全
            if (_initialized) 
            {
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 已经初始化，直接返回");
                return;
            }

            // 使用AsyncLock避免并发初始化
            lock (_lockObject)
            {
                if (_initialized) 
                {
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 在锁内检查，已经初始化，直接返回");
                    return;
                }
                
                // 标记为正在初始化，防止重复调用
                _initialized = true;
                
                // 从配置读取封面缓存设置，并应用到Song类的静态属性
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 封面缓存设置已应用，当前状态: {ConfigurationService.CurrentConfiguration.IsCoverCacheEnabled}");
            }

            try
            {
                _logger?.LogInformation("开始初始化服务协调器...");
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 开始初始化服务协调器");

                // 关键服务优先初始化，确保播放功能正常
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 优先初始化关键服务 - PlayerStateService");
                _ = PlayerStateService; // 播放状态服务 - 最高优先级
                
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 优先初始化关键服务 - MessagingService");
                _ = MessagingService; // 消息服务 - 第二优先级
                
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 优先初始化关键服务 - PlayerService");
                _ = PlayerService; // 播放服务 - 第三优先级
                
                // 然后初始化缓存服务，确保单一数据源
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 初始化缓存服务");
                var cacheInitialized = await CacheService.InitializeCacheAsync();
                
                if (!cacheInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 缓存初始化失败，但程序继续运行");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 缓存初始化成功");
                }

                // 最后初始化其他服务
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 初始化其他服务");
                _ = NotificationService;
                _ = ConfigurationService;
                _ = PlaylistDataService;

                // 初始化播放列表数据
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 开始调用 InitializePlaylistAsyncSynchronously");
                await InitializePlaylistAsyncSynchronously();
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: InitializePlaylistAsyncSynchronously 执行完成");

                // 发送协调器初始化完成消息
                // MessagingService.Send(new ServiceCoordinatorInitializedMessage());

                _logger?.LogInformation("服务协调器初始化完成");
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: InitializeAsync 执行完成，_initialized设置为true");
            }
            catch (Exception ex)
            {
                // 如果初始化失败，重置状态以便重试
                lock (_lockObject)
                {
                    _initialized = false;
                }
                
                _logger?.LogError(ex, "服务协调器初始化失败");
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: InitializeAsync 失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 异常详情: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 内部异常: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 内部异常堆栈: {ex.InnerException.StackTrace}");
                }
                
                throw;
            }
        }

        /// <summary>
        /// 同步初始化播放列表数据
        /// 重构后只调用PlaylistDataService的LoadFromJsonAsync，因为缓存已初始化
        /// </summary>
        private async Task InitializePlaylistAsyncSynchronously()
        {
            try
            {
                _logger?.LogInformation("开始同步初始化播放列表数据...");
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 开始同步初始化播放列表数据...");
                
                
                // 检查PlaylistDataService是否可用
                if (PlaylistDataService == null)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: PlaylistDataService 为null");
                    throw new InvalidOperationException("PlaylistDataService不可用");
                }
                
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: PlaylistDataService 可用，准备加载数据");
                
                // 确保PlaylistDataService已初始化（它现在会从缓存加载）
                var loadTask = PlaylistDataService.LoadFromDataAsync ();
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                var timeoutTask = System.Threading.Tasks.Task.Delay(Timeout.Infinite, cts.Token);
                
                var completedTask = await System.Threading.Tasks.Task.WhenAny(loadTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: LoadFromJsonAsync 超时");
                    throw new TimeoutException("加载播放列表数据超时");
                }
                else
                {
                    await loadTask.ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: LoadFromJsonAsync 完成");
                }
                
                // 验证数据加载结果
                var dataSource = PlaylistDataService.DataSource;
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 获取数据源，歌曲数量: {dataSource.Count}");
                
                // 如果数据源为空，记录警告
                if (dataSource.Count == 0)
                {
                    _logger?.LogWarning("播放列表数据为空，可能是首次启动或数据文件丢失");
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 警告 - 播放列表数据为空");
                }
                else
                {
                    _logger?.LogInformation($"播放列表数据初始化完成，共加载 {dataSource.Count} 首歌曲");
                    System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 播放列表数据初始化完成，共加载 {dataSource.Count} 首歌曲");
                }
                
                // 清除临时数据缓存
                PlaylistDataService.ClearDataSource();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "初始化播放列表数据失败");
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 初始化播放列表数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 异常详情: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 内部异常: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 内部异常堆栈: {ex.InnerException.StackTrace}");
                }
                
                // 确保即使失败也设置默认状态
                try
                {
                    // 发送空数据通知，让UI正常显示
                    MessagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.InitialLoad,
                        PlaylistDataService?.CurrentSortRule ?? SortRule.ByAddedTime,
                        new List<MusicPlayer.Core.Models.Song>(),
                        null));
                }
                catch (Exception notifyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 发送空数据通知失败: {notifyEx.Message}");
                }
                
                throw;
            }
        }

       
        

        /// <summary>
        /// 安全地执行需要跨服务协作的操作
        /// </summary>
        public async Task<T> ExecuteOperationAsync<T>(Func<Task<T>> operation)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceCoordinator));

            var operationId = Guid.NewGuid().ToString();
            _activeOperations[operationId] = true;

            try
            {
                _logger?.LogDebug("开始执行协调操作 {OperationId}", operationId);
                var result = await operation();
                _logger?.LogDebug("协调操作 {OperationId} 执行成功", operationId);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "协调操作 {OperationId} 执行失败", operationId);
                throw;
            }
            finally
            {
                _activeOperations.TryRemove(operationId, out _);
            }
        }

        /// <summary>
        /// 同步执行服务间协调操作
        /// </summary>
        public T ExecuteOperation<T>(Func<T> operation)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceCoordinator));

            var operationId = Guid.NewGuid().ToString();
            _activeOperations[operationId] = true;

            try
            {
                _logger?.LogDebug("开始执行同步协调操作 {OperationId}", operationId);
                var result = operation();
                _logger?.LogDebug("同步协调操作 {OperationId} 执行成功", operationId);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "同步协调操作 {OperationId} 执行失败", operationId);
                throw;
            }
            finally
            {
                _activeOperations.TryRemove(operationId, out _);
            }
        }

        /// <summary>
        /// 获取服务的运行时状态信息
        /// </summary>
        public ServiceCoordinatorStatus GetStatus()
        {
            var playlistDataCount = 0;
            var playlistInitialized = false;
            
            try
            {
                if (PlaylistDataService != null)
                {
                    var dataSource = PlaylistDataService.DataSource;
                    playlistDataCount = dataSource.Count;
                    PlaylistDataService.ClearDataSource();
                    
                    // 使用反射检查初始化状态
                    var playlistType = PlaylistDataService.GetType();
                    var initField = playlistType.GetField("_isInitialized", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (initField != null)
                    {
                        playlistInitialized = (bool)initField.GetValue(PlaylistDataService);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 获取PlaylistDataService状态失败: {ex.Message}");
            }
            
            return new ServiceCoordinatorStatus
            {
                IsInitialized = _initialized,
                InitializationTime = DateTime.Now,
                ActiveOperationsCount = _activeOperations.Count,
                ServiceStatuses = new[]
                {
                    $"PlayerService: {PlayerService != null}",
                    $"PlaylistDataService: {PlaylistDataService != null}, 初始化状态: {playlistInitialized}, 数据量: {playlistDataCount}",
                    $"PlayerStateService: {PlayerStateService != null}",
                    $"NotificationService: {NotificationService != null}",
                    $"MessagingService: {MessagingService != null}",
                    $"ConfigurationService: {ConfigurationService != null}"
                }
            };
        }
        
        /// <summary>
        /// 强制初始化PlaylistDataService，用于诊断和修复问题
        /// </summary>
        public async Task<bool> ForceInitializePlaylistDataServiceAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 开始强制初始化 PlaylistDataService");
                
                if (PlaylistDataService == null)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceCoordinator: PlaylistDataService 为null，无法初始化");
                    return false;
                }
                
                await InitializePlaylistAsyncSynchronously();
                System.Diagnostics.Debug.WriteLine("ServiceCoordinator: 强制初始化 PlaylistDataService 完成");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServiceCoordinator: 强制初始化 PlaylistDataService 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 订阅消息以协调服务间通信
        /// </summary>
        private void SubscribeToMessages()
        {
            // 订阅播放状态变更消息，协调相关服务
            MessagingService.Register<CurrentSongChangedMessage>(this, OnCurrentSongChanged);
            MessagingService.Register<PlaybackStateChangedMessage>(this, OnPlaybackStateChanged);
            
            // 订阅播放列表变更消息
            MessagingService.Register<PlaylistUpdatedMessage>(this, OnPlaylistUpdated);
        }

        /// <summary>
        /// 处理当前歌曲变更消息
        /// </summary>
        private void OnCurrentSongChanged(object recipient, CurrentSongChangedMessage message)
        {
            try
            {
                // 协调各个服务对歌曲变更的响应
                if (PlayerService != null)
                {
                    // 确保播放器状态同步
                    var playerService = PlayerService;
                    // 这里可以添加协调逻辑
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理当前歌曲变更消息时发生错误");
            }
        }

        /// <summary>
        /// 处理播放状态变更消息
        /// </summary>
        private void OnPlaybackStateChanged(object recipient, PlaybackStateChangedMessage message)
        {
            try
            {
                // 协调通知服务更新
                if (NotificationService != null)
                {
                    var notificationService = NotificationService;
                    // 这里可以添加协调逻辑
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理播放状态变更消息时发生错误");
            }
        }

        /// <summary>
        /// 处理播放列表更新消息
        /// </summary>
        private void OnPlaylistUpdated(object recipient, PlaylistUpdatedMessage message)
        {
            try
            {
                // 协调播放列表相关服务的响应
                if (PlayerStateService != null)
                {
                    var playerStateService = PlayerStateService;
                    // 这里可以添加协调逻辑
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理播放列表更新消息时发生错误");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_lockObject)
            {
                if (_disposed) return;
            }

            try
            {
                // 取消消息订阅
                MessagingService.Unregister(this);

                _logger?.LogInformation("服务协调器已释放");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "释放服务协调器时发生错误");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
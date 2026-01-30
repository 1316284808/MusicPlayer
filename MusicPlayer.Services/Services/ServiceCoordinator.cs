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
        private readonly ServiceInitializationManager _initManager;
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

        public ServiceCoordinator(
            ServiceInitializationManager initManager,
            IPlayerService playerService,
            IPlayerStateService playerStateService,
            INotificationService notificationService,
            IMessagingService messagingService,
            IConfigurationService configurationService,
            IPlaylistDataService playlistDataService,
            IPlaylistCacheService cacheService,
            ILogger<ServiceCoordinator>? logger = null)
        {
            _initManager = initManager ?? throw new ArgumentNullException(nameof(initManager));
            
            PlayerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            PlayerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            MessagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            ConfigurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            PlaylistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            CacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger;

            _logger?.LogDebug($"ServiceCoordinator创建，初始化管理器ID: {initManager.GetHashCode()}");
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
            if (_initialized) return;

            lock (_lockObject)
            {
                if (_initialized) return;
                _initialized = true;
            }

            try
            {
                _logger?.LogInformation("服务协调器开始初始化...");

              
                await _initManager.InitializeAllServicesAsync();

                _logger?.LogInformation("服务协调器初始化完成");
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    _initialized = false;
                }

                _logger?.LogError(ex, "服务协调器初始化失败");
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
            return new ServiceCoordinatorStatus
            {
                IsInitialized = _initialized,
                InitializationTime = DateTime.Now,
                ActiveOperationsCount = _activeOperations.Count,
                // 不再检查具体服务状态，只报告自身状态
                ServiceStatuses = new[] { $"ServiceCoordinator: {_initialized}" }
            };
        }
        
        /// <summary>
        /// 强制初始化PlaylistDataService，用于诊断和修复问题
        /// </summary>
        public async Task<bool> ForceInitializePlaylistDataServiceAsync()
        {
            return await _initManager.ForceReinitializePlaylistAsync();
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
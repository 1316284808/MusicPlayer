using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;
using System.Collections.Generic;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interfaces;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 服务初始化管理器 - 专门负责服务初始化顺序和协调
    /// </summary>
    public class ServiceInitializationManager
    {
        private readonly ILogger<ServiceInitializationManager> _logger;
        private readonly IPlayerService _playerService;
        private readonly IPlayerStateService _playerStateService;
        private readonly IMessagingService _messagingService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaylistCacheService _cacheService;

        public ServiceInitializationManager(
            IPlayerService playerService,
            IPlayerStateService playerStateService,
            IMessagingService messagingService,
            IConfigurationService configurationService,
            IPlaylistDataService playlistDataService,
            IPlaylistCacheService cacheService,
            ILogger<ServiceInitializationManager> logger = null)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _playerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger;
        }

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        public async Task InitializeAllServicesAsync()
        {
            var sw = Stopwatch.StartNew();
            _logger?.LogInformation("开始初始化所有服务...");

            try
            {
                // Phase 1: 核心服务初始化（无依赖）
                await InitializeCoreServicesAsync();

                // Phase 2: 缓存服务初始化
                await InitializeCacheServiceAsync();

                // Phase 3: 播放列表数据初始化
                await InitializePlaylistDataAsync();

                // Phase 4: 其他服务初始化
                InitializeRemainingServices();

                sw.Stop();
                _logger?.LogInformation($"所有服务初始化完成，耗时: {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger?.LogError(ex, $"服务初始化失败，耗时: {sw.ElapsedMilliseconds}ms");
                throw;
            }
        }

        private async Task InitializeCoreServicesAsync()
        {
            _logger?.LogDebug("初始化核心服务...");

            // PlayerStateService - 最高优先级
            _ = _playerStateService;
            _logger?.LogDebug("PlayerStateService 已初始化");

            // MessagingService - 第二优先级
            _ = _messagingService;
            _logger?.LogDebug("MessagingService 已初始化");

            // PlayerService - 第三优先级
            _ = _playerService;
            _logger?.LogDebug("PlayerService 已初始化");

            _logger?.LogDebug("核心服务初始化完成");
        }

        private async Task InitializeCacheServiceAsync()
        {
            _logger?.LogDebug("初始化缓存服务...");

            var cacheInitialized = await _cacheService.InitializeCacheAsync();
            if (cacheInitialized)
            {
                _logger?.LogDebug("缓存服务初始化成功");
            }
            else
            {
                _logger?.LogWarning("缓存服务初始化失败，程序继续运行");
            }
        }

        private async Task InitializePlaylistDataAsync()
        {
            _logger?.LogDebug("开始加载播放列表数据...");

            try
            {
                var loadTask = _playlistDataService.LoadFromDataAsync();
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));

                var completedTask = await Task.WhenAny(loadTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask == loadTask)
                {
                    await loadTask.ConfigureAwait(false);
                    _logger?.LogDebug($"播放列表数据加载完成，共 {_playlistDataService.DataSource.Count} 首歌曲");
                }
                else
                {
                    _logger?.LogError("加载播放列表数据超时");
                    throw new TimeoutException("加载播放列表数据超时");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载播放列表数据失败");

                // 发送空数据通知，让UI正常显示
                _messagingService.Send(new PlaylistDataChangedMessage(
                    DataChangeType.InitialLoad,
                    _playlistDataService.CurrentSortRule,
                    new List<MusicPlayer.Core.Models.Song>(),
                    null));

                throw;
            }
        }

        private void InitializeRemainingServices()
        {
            _logger?.LogDebug("初始化其他服务...");

            _ = _configurationService;
            _logger?.LogDebug("ConfigurationService 已初始化");

            _logger?.LogDebug("所有服务初始化完成");
        }

        /// <summary>
        /// 强制重新初始化播放列表数据（用于诊断）
        /// </summary>
        public async Task<bool> ForceReinitializePlaylistAsync()
        {
            _logger?.LogWarning("强制重新初始化播放列表数据...");

            try
            {
                await InitializePlaylistDataAsync();
                _logger?.LogWarning("强制重新初始化播放列表数据完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "强制重新初始化播放列表数据失败");
                return false;
            }
        }
    }
}

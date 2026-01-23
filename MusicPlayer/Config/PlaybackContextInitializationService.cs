using Microsoft.Extensions.Hosting;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Config
{
    /// <summary>
    /// 播放上下文初始化服务
    /// 负责在应用启动时初始化播放上下文提供者
    /// </summary>
    public class PlaybackContextInitializationService : IHostedService
    {
        private readonly IPlaybackContextService _playbackContextService;
        private readonly IPlaybackContextProvider _defaultProvider;
        private readonly FavoritesProvider _favoritesProvider;
        private readonly ArtistProvider _artistProvider;
        private readonly AlbumProvider _albumProvider;
        private readonly CustomPlaylistProvider _customPlaylistProvider;

        public PlaybackContextInitializationService(
            IPlaybackContextService playbackContextService,
            IPlaybackContextProvider defaultProvider,
            FavoritesProvider favoritesProvider,
            ArtistProvider artistProvider,
            AlbumProvider albumProvider,
            CustomPlaylistProvider customPlaylistProvider)
        {
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _defaultProvider = defaultProvider ?? throw new ArgumentNullException(nameof(defaultProvider));
            _favoritesProvider = favoritesProvider ?? throw new ArgumentNullException(nameof(favoritesProvider));
            _artistProvider = artistProvider ?? throw new ArgumentNullException(nameof(artistProvider));
            _albumProvider = albumProvider ?? throw new ArgumentNullException(nameof(albumProvider));
            _customPlaylistProvider = customPlaylistProvider ?? throw new ArgumentNullException(nameof(customPlaylistProvider));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PlaybackContextInitializationService: 开始初始化播放上下文提供者");

                // 注册各种播放上下文提供者
                _playbackContextService.RegisterProvider(PlaybackContextType.DefaultPlaylist, _defaultProvider);

                _playbackContextService.RegisterProvider(PlaybackContextType.Artist, _artistProvider);
                _playbackContextService.RegisterProvider(PlaybackContextType.Album, _albumProvider);
                _playbackContextService.RegisterProvider(PlaybackContextType.CustomPlaylist, _customPlaylistProvider);

                System.Diagnostics.Debug.WriteLine("PlaybackContextInitializationService: 播放上下文提供者初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaybackContextInitializationService: 初始化失败: {ex.Message}");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("PlaybackContextInitializationService: 停止播放上下文初始化服务");
            return Task.CompletedTask;
        }
    }
}

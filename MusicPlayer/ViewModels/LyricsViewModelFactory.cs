using MusicPlayer.Core.Interface;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌词视图模型工厂实现类
    /// </summary>
    public class LyricsViewModelFactory : ILyricsViewModelFactory
    {
        private readonly IPlayerStateService _playerStateService;
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistService _playlistService;

        public LyricsViewModelFactory(
            IPlayerStateService playerStateService,
            IMessagingService messagingService,
            IPlaylistService playlistService)
        {
            _playerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
        }

        public ILyricsViewModel CreateLyricsViewModel()
        {
            return new LyricsViewModel(_playerStateService, _messagingService, _playlistService);
        }
    }
}
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
        private readonly ILyricsService _lyricsService;

        public LyricsViewModelFactory(
            IPlayerStateService playerStateService,
            IMessagingService messagingService,
            ILyricsService lyricsService)
        {
            _playerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _lyricsService = lyricsService ?? throw new ArgumentNullException(nameof(lyricsService));
        }

        public ILyricsViewModel CreateLyricsViewModel()
        {
            return new LyricsViewModel(_playerStateService, _messagingService, _lyricsService);
        }
    }
}
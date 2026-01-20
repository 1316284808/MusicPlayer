using CommunityToolkit.Mvvm.ComponentModel;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌单页面视图模型
    /// </summary>
    public class HeartViewModel : ObservableObject, IHeartViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;

        public HeartViewModel(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public override void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("HeartViewModel: Initialize 方法被调用");
            // 这里可以添加初始化逻辑，比如加载歌单数据
        }

        /// <summary>
        /// 清理视图模型资源
        /// </summary>
        public override void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("HeartViewModel: Cleanup 方法被调用");
            // 这里可以添加清理逻辑，比如取消消息注册、释放资源等
            _messagingService.Unregister(this);
        }
    }
}
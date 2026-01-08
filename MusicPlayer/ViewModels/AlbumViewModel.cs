using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;
using System.Windows.Input;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 专辑页面视图模型
    /// </summary>
    public class AlbumViewModel : ObservableObject, IAlbumViewModel
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly IMessagingService _messagingService;

        private ObservableCollection<string> _albums = new();
        private string? _selectedAlbum;

        /// <summary>
        /// 专辑列表
        /// </summary>
        public ObservableCollection<string> Albums => _albums;

        /// <summary>
        /// 当前选中的专辑
        /// </summary>
        public string? SelectedAlbum
        {
            get => _selectedAlbum;
            set
            {
                if (_selectedAlbum != value)
                {
                    _selectedAlbum = value;
                    OnPropertyChanged(nameof(SelectedAlbum));
                    OnPropertyChanged(nameof(HasSelectedAlbum));
                }
            }
        }

        /// <summary>
        /// 是否有选中的专辑
        /// </summary>
        public bool HasSelectedAlbum => !string.IsNullOrEmpty(SelectedAlbum);

        /// <summary>
        /// 播放选中专辑的歌曲命令
        /// </summary>
        public ICommand PlayAlbumCommand { get; }

        public AlbumViewModel(
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService,
            IMessagingService messagingService)
        {
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));

            PlayAlbumCommand = new RelayCommand<string>(ExecutePlayAlbum);

            LoadAlbums();
        }

        /// <summary>
        /// 加载专辑列表
        /// </summary>
        private void LoadAlbums()
        {
            // 从播放列表中获取所有专辑
            var playlist = _playlistDataService.DataSource;
            var albums = playlist
                .Where(song => !string.IsNullOrEmpty(song.Album))
                .Select(song => song.Album!)
                .Distinct()
                .OrderBy(album => album)
                .ToList();

            // 更新UI
            _albums.Clear();
            foreach (var album in albums)
            {
                _albums.Add(album);
            }
        }

        /// <summary>
        /// 执行播放选中专辑的歌曲
        /// </summary>
        /// <param name="album">专辑名称</param>
        private void ExecutePlayAlbum(string? album)
        {
            if (string.IsNullOrEmpty(album))
                return;

            // 获取该专辑的第一首歌
            var playlist = _playlistDataService.DataSource;
            var firstSong = playlist
                .Where(song => !string.IsNullOrEmpty(song.Album) && 
                              string.Equals(song.Album, album, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (firstSong != null)
            {
                // 设置播放上下文为专辑列表
                var context = PlaybackContext.CreateAlbum(album);
                _playbackContextService.SetPlaybackContext(context.Type, context.Identifier, context.DisplayName);
                
                System.Diagnostics.Debug.WriteLine($"AlbumViewModel: 设置播放上下文为专辑: {album}");

                // 发送播放消息
                _messagingService.Send(new PlaySelectedSongMessage(firstSong));
            }
        }
    }
}
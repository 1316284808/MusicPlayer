using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Services.Messages;
 

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放列表命令处理器 - 处理复杂的命令逻辑
    /// 实现命令处理器模式，将复杂逻辑从ViewModel中提取出来
    /// </summary>
    public interface IPlaylistCommandHandler
    {
        Task<bool> HandleAddMusicCommand();
        void HandlePlaySelectedSongCommand(Song? song);
        void HandleDeleteSelectedSongCommand(Song? song);
        void HandleClearPlaylistCommand();
        void HandleTogglePlaylistCommand();
        void HandleNavigateToSettingsCommand();
    }

    /// <summary>
    /// 播放列表命令处理器实现
    /// </summary>
    public class PlaylistCommandHandler : IPlaylistCommandHandler
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistService _playlistService;
        private readonly IMessagingService _messagingService;

        public PlaylistCommandHandler(
            IPlaylistDataService playlistDataService,
            INotificationService notificationService,
            IPlaylistService playlistService,
            IMessagingService messagingService)
        {
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
        }

        /// <summary>
        /// 处理添加音乐命令
        /// </summary>
        public async Task<bool> HandleAddMusicCommand()
        {
            return await Task.Run(() =>
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "音乐文件|*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.oga;*.aac;*.wma|所有文件|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var fileNames = openFileDialog.FileNames;

                    // 限制一次处理的最大文件数量
                    if (fileNames.Length > 1000)
                    {
                        _notificationService.ShowWarning($"一次最多只能添加1000个文件，当前选择了{fileNames.Length}个文件。请分批添加。");
                        fileNames = fileNames.Take(1000).ToArray();
                    }

                    // 显示进度提示
                    _notificationService.ShowInfo($"正在添加{fileNames.Length}个文件，请稍候...");

                    var songs = new List<Song>();
                    int processedCount = 0;

                    // 批量提取歌曲信息
                    foreach (var file in fileNames)
                    {
                        try
                        {
                            var song = _playlistService.ExtractSongInfo(file);
                            if (song != null)
                            {

                                songs.Add(song);
                                processedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing file {file}: {ex.Message}");
                        }
                    }

                    // 批量添加歌曲
                    if (songs.Count > 0)
                    {
                        _playlistDataService.AddSongs(songs);
                    }

                    _notificationService.ShowSuccess($"成功添加{processedCount}个文件到播放列表。");
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        /// 处理播放选中歌曲命令
        /// </summary>
        public void HandlePlaySelectedSongCommand(Song? song)
        {
            if (song != null)
            {
                _playlistDataService.CurrentSong = song;
            }
        }

        /// <summary>
        /// 处理删除选中歌曲命令
        /// </summary>
        public void HandleDeleteSelectedSongCommand(Song? song)
        {
            if (song != null)
            {
                _playlistDataService.RemoveSong(song);
            }
        }

        /// <summary>
        /// 处理清空播放列表命令
        /// </summary>
        public void HandleClearPlaylistCommand()
        {
            _playlistDataService.ClearPlaylist();
        }

        /// <summary>
        /// 处理切换播放列表显示/隐藏命令
        /// </summary>
        public void HandleTogglePlaylistCommand()
        {
            // 通过消息传递机制切换播放列表显示/隐藏
            _messagingService.Send(new TogglePlaylistMessage());
        }

        /// <summary>
        /// 处理导航到设置页面命令
        /// </summary>
        public void HandleNavigateToSettingsCommand()
        {
            try
            {
                // 通过消息传递机制导航到设置页面
                // NavigateToSettingsMessage 是 RequestMessage<bool>，需要使用带返回值的 Send 方法
                _messagingService.Send<NavigateToSettingsMessage, bool>(new NavigateToSettingsMessage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到设置页面失败: {ex.Message}");
                _notificationService.ShowError($"导航失败: {ex.Message}");
            }
        }
    }
}
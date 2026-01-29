using MusicPlayer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Interface
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

}

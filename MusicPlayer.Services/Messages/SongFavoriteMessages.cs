using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 更新歌曲收藏状态消息
    /// </summary>
    public class UpdateSongFavoriteStatusMessage : RequestMessage<bool>
    {
        public Song Song { get; }
        public bool IsFavorite { get; }
        
        public UpdateSongFavoriteStatusMessage(Song song, bool isFavorite)
        {
            Song = song;
            IsFavorite = isFavorite;
        }
    }
}
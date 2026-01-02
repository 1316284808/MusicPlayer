using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 更新歌曲删除状态消息
    /// </summary>
    public class UpdateSongDeletionStatusMessage : RequestMessage<bool>
    {
        public Song Song { get; }
        public bool IsDeleted { get; }
        
        public UpdateSongDeletionStatusMessage(Song song, bool isDeleted)
        {
            Song = song;
            IsDeleted = isDeleted;
        }
    }
}
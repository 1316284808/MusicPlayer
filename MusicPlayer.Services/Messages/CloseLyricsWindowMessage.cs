using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 关闭歌词窗口消息
    /// </summary>
    public class CloseLyricsWindowMessage : RequestMessage<bool>
    {
    }
}
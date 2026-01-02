using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 错误发生消息
    /// </summary>
    public class ErrorMessage : ValueChangedMessage<ErrorInfo>
    {
        public ErrorMessage(ErrorInfo errorInfo) : base(errorInfo) { }
    }
    
    /// <summary>
    /// 警告消息
    /// </summary>
    public class WarningMessage : ValueChangedMessage<WarningInfo>
    {
        public WarningMessage(WarningInfo warningInfo) : base(warningInfo) { }
    }
    
    /// <summary>
    /// 信息消息
    /// </summary>
    public class InfoMessage : ValueChangedMessage<InfoData>
    {
        public InfoMessage(InfoData infoData) : base(infoData) { }
    }
}
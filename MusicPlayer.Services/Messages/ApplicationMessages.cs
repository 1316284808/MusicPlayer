using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 应用初始化完成消息
    /// </summary>
    public class ApplicationInitializationCompletedMessage
    {
        public ApplicationInitializationCompletedMessage()
        {
        }
    }

    /// <summary>
    /// 应用启动完成消息
    /// </summary>
    public class ApplicationStartedMessage : RequestMessage<bool> { }
    
    /// <summary>
    /// 应用即将关闭消息
    /// </summary>
    public class ApplicationClosingMessage : RequestMessage<bool>
    {
        public bool CanCancel { get; set; } = true;
        public string Reason { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 应用暂停消息
    /// </summary>
    public class ApplicationSuspendedMessage : RequestMessage<bool> { }
    
    /// <summary>
    /// 应用恢复消息
    /// </summary>
    public class ApplicationResumedMessage : RequestMessage<bool> { }

    /// <summary>
    /// 关闭应用程序消息
    /// </summary>
    public class CloseApplicationMessage : RequestMessage<bool> { }
}
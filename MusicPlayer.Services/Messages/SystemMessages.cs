using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 配置更新消息
    /// </summary>
    public class ConfigurationUpdatedMessage : ValueChangedMessage<ConfigurationInfo>
    {
        public ConfigurationUpdatedMessage(ConfigurationInfo configInfo) : base(configInfo) { }
    }
    
    /// <summary>
    /// 系统通知消息
    /// </summary>
    public class SystemNotificationMessage : ValueChangedMessage<SystemNotificationInfo>
    {
        public SystemNotificationMessage(SystemNotificationInfo notificationInfo) : base(notificationInfo) { }
    }
}
using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 服务协调器初始化完成消息
    /// </summary>
    public class ServiceCoordinatorInitializedMessage : RequestMessage<bool> { }
    
    /// <summary>
    /// 服务协调器状态查询消息
    /// </summary>
    public class ServiceCoordinatorStatusQueryMessage : RequestMessage<ServiceCoordinatorStatus> { }
    
    /// <summary>
    /// 服务状态变更消息
    /// </summary>
    public class ServiceStateChangedMessage : ValueChangedMessage<ServiceStateInfo>
    {
        public ServiceStateChangedMessage(ServiceStateInfo stateInfo) : base(stateInfo) { }
    }
    
    /// <summary>
    /// 服务间协调操作请求消息
    /// </summary>
    public class ServiceCoordinationRequestMessage<T> : RequestMessage<T>
    {
        public string OperationName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        public ServiceCoordinationRequestMessage(string operationName, Dictionary<string, object>? parameters = null)
        {
            OperationName = operationName;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }
}
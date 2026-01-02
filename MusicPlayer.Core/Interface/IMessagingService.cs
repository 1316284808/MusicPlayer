using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 统一的MVVM消息通信服务接口
    /// 提供跨ViewModel通信的统一接口，支持依赖注入和静态访问两种方式
    /// </summary>
    public interface IMessagingService
    {
        /// <summary>
        /// 发送消息（异步，无返回值）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        void Send<TMessage>(TMessage message) where TMessage : class;

        /// <summary>
        /// 发送消息并等待响应（同步，有返回值）
        /// </summary>
        /// <typeparam name="TRequest">请求消息类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="message">请求消息实例</param>
        /// <returns>响应结果</returns>
        TResponse? Send<TRequest, TResponse>(TRequest message) where TRequest : RequestMessage<TResponse>;

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <param name="handler">处理器</param>
        void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler) where TMessage : class;

        /// <summary>
        /// 注销所有消息处理器
        /// </summary>
        /// <param name="recipient">接收者</param>
        void Unregister(object recipient);

        /// <summary>
        /// 检查是否已注册指定消息类型
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <returns>是否已注册</returns>
        bool IsRegistered<TMessage>(object recipient) where TMessage : class;
    }

    /// <summary>
    /// 消息服务扩展方法
    /// </summary>
    public static class MessagingServiceExtensions
    {
        /// <summary>
        /// 发送值变更消息
        /// </summary>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="messagingService">消息服务</param>
        /// <param name="value">变更的值</param>
        public static void SendValueChanged<TValue>(this IMessagingService messagingService, TValue value)
        {
            var message = new CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<TValue>(value);
            messagingService.Send(message);
        }
    }
}
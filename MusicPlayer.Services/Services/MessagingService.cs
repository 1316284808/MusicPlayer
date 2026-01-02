using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Interface;
using System.Windows;


namespace MusicPlayer.Services
{
    /// <summary>
    /// 统一的消息通信服务实现
    /// 封装CommunityToolkit.Mvvm的消息机制，提供统一的跨ViewModel通信能力
    /// </summary>
    public class MessagingService : IMessagingService
    {
        private readonly IMessenger _messenger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messenger">底层消息传递器实例</param>
        public MessagingService(IMessenger messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        /// <summary>
        /// 发送消息（异步，无返回值）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        public void Send<TMessage>(TMessage message) where TMessage : class
        {
            // 确保消息在UI线程上发送，特别是窗口状态变化相关的消息
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() => _messenger.Send(message));
            }
            else
            {
                _messenger.Send(message);
            }
        }

        /// <summary>
        /// 发送消息并等待响应（同步，有返回值）
        /// </summary>
        /// <typeparam name="TRequest">请求消息类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="message">请求消息实例</param>
        /// <returns>响应结果</returns>
        public TResponse? Send<TRequest, TResponse>(TRequest message) 
            where TRequest : RequestMessage<TResponse>
        {
            // 确保消息在UI线程上发送
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                return Application.Current.Dispatcher.Invoke(() => 
                {
                    _messenger.Send(message);
                    return message.Response;
                });
            }
            else
            {
                _messenger.Send(message);
                return message.Response;
            }
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <param name="handler">处理器</param>
        public void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler) where TMessage : class
        {
            // 创建包装处理器，确保消息处理在UI线程上执行
            MessageHandler<object, TMessage> uiThreadHandler = (r, m) =>
            {
                if (Application.Current?.Dispatcher?.CheckAccess() == false)
                {
                    Application.Current.Dispatcher.Invoke(() => handler(r, m));
                }
                else
                {
                    handler(r, m);
                }
            };
            
            _messenger.Register(recipient, uiThreadHandler);
        }

        /// <summary>
        /// 注销所有消息处理器
        /// </summary>
        /// <param name="recipient">接收者</param>
        public void Unregister(object recipient)
        {
            _messenger.UnregisterAll(recipient);
        }

        /// <summary>
        /// 检查是否已注册指定消息类型
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <returns>是否已注册</returns>
        public bool IsRegistered<TMessage>(object recipient) where TMessage : class
        {
            return _messenger.IsRegistered<TMessage>(recipient);
        }
    }
}
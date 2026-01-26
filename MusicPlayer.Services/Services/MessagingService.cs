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
            try
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
            catch (InvalidOperationException ex)
            {
                // 捕获没有收到响应的异常，返回默认值
                System.Diagnostics.Debug.WriteLine($"MessagingService: 发送请求消息时没有收到响应: {ex.Message}");
                return default;
            }
            catch (Exception ex)
            {
                // 捕获其他异常，返回默认值
                System.Diagnostics.Debug.WriteLine($"MessagingService: 发送请求消息时发生异常: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 异步发送消息
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <returns>任务</returns>
        public async System.Threading.Tasks.Task SendAsync<TMessage>(TMessage message) where TMessage : class
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                Send(message);
            });
        }

        /// <summary>
        /// 异步发送消息并等待响应
        /// </summary>
        /// <typeparam name="TRequest">请求消息类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="message">请求消息实例</param>
        /// <returns>响应结果</returns>
        public async System.Threading.Tasks.Task<TResponse?> SendAsync<TRequest, TResponse>(TRequest message) 
            where TRequest : RequestMessage<TResponse>
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                return Send<TRequest, TResponse>(message);
            });
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
        /// 注册消息处理器（带过滤条件）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <param name="handler">处理器</param>
        /// <param name="filter">过滤条件</param>
        public void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler, Func<TMessage, bool> filter) where TMessage : class
        {
            // 创建包装处理器，确保消息处理在UI线程上执行，并应用过滤条件
            MessageHandler<object, TMessage> uiThreadHandler = (r, m) =>
            {
                if (filter(m))
                {
                    if (Application.Current?.Dispatcher?.CheckAccess() == false)
                    {
                        Application.Current.Dispatcher.Invoke(() => handler(r, m));
                    }
                    else
                    {
                        handler(r, m);
                    }
                }
            };
            
            _messenger.Register(recipient, uiThreadHandler);
        }

        /// <summary>
        /// 注册消息处理器（带优先级）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <param name="handler">处理器</param>
        /// <param name="priority">优先级（值越大，优先级越高）</param>
        public void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler, int priority) where TMessage : class
        {
            // 注意：CommunityToolkit.Mvvm.Messenger 不直接支持优先级，这里我们使用过滤条件参数位置来兼容接口
            // 实际优先级处理需要自定义实现，这里我们先简化处理
            Register(recipient, handler);
        }

        /// <summary>
        /// 注册消息处理器（带过滤条件和优先级）
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        /// <param name="handler">处理器</param>
        /// <param name="filter">过滤条件</param>
        /// <param name="priority">优先级（值越大，优先级越高）</param>
        public void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler, Func<TMessage, bool> filter, int priority) where TMessage : class
        {
            // 注意：CommunityToolkit.Mvvm.Messenger 不直接支持优先级，这里我们先应用过滤条件
            // 实际优先级处理需要自定义实现，这里我们先简化处理
            Register(recipient, handler, filter);
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
        /// 注销特定消息类型的处理器
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <param name="recipient">接收者</param>
        public void Unregister<TMessage>(object recipient) where TMessage : class
        {
            _messenger.Unregister<TMessage>(recipient);
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
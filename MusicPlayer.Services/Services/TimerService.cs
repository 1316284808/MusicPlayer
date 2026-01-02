using System;
using System.Windows.Threading;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Services
{
    /// <summary>
    /// WPF定时器服务实现 - 封装DispatcherTimer功能
    /// 提供定时器操作的抽象层，使ViewModel不直接依赖WPF框架
    /// </summary>
    public class TimerService : ITimerService, IDisposable
    {
        private readonly DispatcherTimer _dispatcherTimer;
        private bool _disposed = false;

        /// <summary>
        /// 初始化定时器服务
        /// </summary>
        public TimerService()
        {
            // 创建DispatcherTimer实例
            _dispatcherTimer = new DispatcherTimer();
            
            // 设置默认间隔
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            
            // 订阅内部定时器事件，转发到外部
            _dispatcherTimer.Tick += OnDispatcherTimerTick;
        }

        /// <summary>
        /// 定时器间隔时间
        /// </summary>
        public TimeSpan Interval
        {
            get => _dispatcherTimer.Interval;
            set => _dispatcherTimer.Interval = value;
        }

        /// <summary>
        /// 定时器是否正在运行
        /// </summary>
        public bool IsEnabled => _dispatcherTimer.IsEnabled;

        /// <summary>
        /// 定时器触发事件
        /// </summary>
        public event EventHandler<EventArgs>? Tick;

        /// <summary>
        /// 启动定时器
        /// </summary>
        public void Start()
        {
            _dispatcherTimer.Start();
        }

        /// <summary>
        /// 停止定时器
        /// </summary>
        public void Stop()
        {
            _dispatcherTimer.Stop();
        }

        /// <summary>
        /// 重启定时器（先停止再启动）
        /// </summary>
        public void Restart()
        {
            _dispatcherTimer.Stop();
            _dispatcherTimer.Start();
        }

        /// <summary>
        /// 内部定时器事件处理器
        /// </summary>
        private void OnDispatcherTimerTick(object? sender, EventArgs e)
        {
            // 转发事件到外部订阅者
            Tick?.Invoke(this, e);
        }

        /// <summary>
        /// 释放定时器资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 停止定时器
                _dispatcherTimer.Stop();
                
                // 取消事件订阅
                _dispatcherTimer.Tick -= OnDispatcherTimerTick;
                
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~TimerService()
        {
            Dispose(false);
        }
    }
}
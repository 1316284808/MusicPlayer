using System;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 定时器服务接口 - 抽象定时器功能
    /// 解决ViewModel中直接依赖具体UI框架定时器的问题，提高代码可移植性和可测试性
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// 定时器间隔时间
        /// </summary>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// 定时器是否正在运行
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 定时器触发事件
        /// </summary>
        event EventHandler<EventArgs> Tick;

        /// <summary>
        /// 启动定时器
        /// </summary>
        void Start();

        /// <summary>
        /// 停止定时器
        /// </summary>
        void Stop();

        /// <summary>
        /// 重启定时器（先停止再启动）
        /// </summary>
        void Restart();

        /// <summary>
        /// 释放定时器资源
        /// </summary>
        void Dispose();
    }
}
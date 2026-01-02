using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Services
{
    /// <summary>
    /// WPF调度器服务实现 - 封装WPF Dispatcher功能
    /// 提供UI线程操作的抽象层，使ViewModel不直接依赖WPF框架
    /// </summary>
    public class DispatcherService : IDispatcherService
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// 初始化调度器服务
        /// </summary>
        public DispatcherService()
        {
            try
            {
                // 获取当前应用程序的UI调度器
                _dispatcher = System.Windows.Application.Current?.Dispatcher 
                    ?? throw new InvalidOperationException("无法获取应用程序调度器，确保在WPF应用程序上下文中调用此服务。");
                
                // 验证调度器是否可用
                if (_dispatcher == null)
                {
                    throw new InvalidOperationException("调度器为null，无法初始化DispatcherService。");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化DispatcherService失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 在UI线程上同步执行指定操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public void Invoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                if (CheckAccess())
                {
                    // 如果已在UI线程，直接执行
                    action();
                }
                else
                {
                    // 切换到UI线程同步执行
                    if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
                    {
                        System.Diagnostics.Debug.WriteLine("调度器已关闭，无法执行操作");
                        return;
                    }
                    
                    _dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调度器调用失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 在UI线程上异步执行指定操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <returns>表示异步操作的任务</returns>
        public Task InvokeAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                if (CheckAccess())
                {
                    // 如果已在UI线程，直接执行并返回已完成的任务
                    action();
                    return Task.CompletedTask;
                }
                else
                {
                    // 检查调度器状态
                    if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
                    {
                        System.Diagnostics.Debug.WriteLine("调度器已关闭，无法执行异步操作");
                        return Task.CompletedTask;
                    }
                    
                    // 切换到UI线程异步执行
                    return _dispatcher.InvokeAsync(action).Task;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"异步调度器调用失败: {ex.Message}");
                return Task.FromException(ex);
            }
        }

        /// <summary>
        /// 在UI线程上异步执行指定操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <returns>表示异步操作的任务，包含执行结果</returns>
        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            if (CheckAccess())
            {
                // 如果已在UI线程，直接执行并返回结果
                var result = func();
                return Task.FromResult(result);
            }
            else
            {
                // 切换到UI线程异步执行
                return _dispatcher.InvokeAsync(func).Task;
            }
        }

        /// <summary>
        /// 检查当前是否在UI线程上
        /// </summary>
        /// <returns>如果在UI线程上返回true，否则返回false</returns>
        public bool CheckAccess()
        {
            return _dispatcher.CheckAccess();
        }

        /// <summary>
        /// 确保操作在UI线程上执行，如果不在UI线程则切换到UI线程
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public void EnsureUIThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Invoke(action);
        }
    }
}
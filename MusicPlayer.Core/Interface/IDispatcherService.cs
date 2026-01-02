using System;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// UI线程调度服务接口 - 抽象UI线程操作
    /// 解决ViewModel中直接依赖具体UI框架的问题，提高代码可测试性和可移植性
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// 在UI线程上同步执行指定操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        void Invoke(Action action);

        /// <summary>
        /// 在UI线程上异步执行指定操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <returns>表示异步操作的任务</returns>
        Task InvokeAsync(Action action);

        /// <summary>
        /// 在UI线程上异步执行指定操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <returns>表示异步操作的任务，包含执行结果</returns>
        Task<T> InvokeAsync<T>(Func<T> func);

        /// <summary>
        /// 检查当前是否在UI线程上
        /// </summary>
        bool CheckAccess();

        /// <summary>
        /// 确保操作在UI线程上执行，如果不在UI线程则切换到UI线程
        /// </summary>
        /// <param name="action">要执行的操作</param>
        void EnsureUIThread(Action action);
    }
}
using System;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 服务协调器接口 - 统一管理核心服务间的通信和依赖关系
    /// 通过协调器模式解决服务间的循环依赖问题
    /// </summary>
    public interface IServiceCoordinator : IDisposable
    {
        /// <summary>
        /// 检查服务协调器是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        /// <summary>
        /// 播放服务
        /// </summary>
        IPlayerService PlayerService { get; }

        /// <summary>
        /// 播放列表数据服务 - 管理唯一数据源
        /// </summary>
        IPlaylistDataService PlaylistDataService { get; }

        /// <summary>
        /// 播放状态服务
        /// </summary>
        IPlayerStateService PlayerStateService { get; }

        /// <summary>
        /// 消息服务 - 核心通信服务
        /// </summary>
        IMessagingService MessagingService { get; }

        /// <summary>
        /// 配置服务
        /// </summary>
        IConfigurationService ConfigurationService { get; }

        /// <summary>
        /// 通知服务
        /// </summary>
        INotificationService NotificationService { get; }

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        /// <returns>异步初始化任务</returns>
        Task InitializeAsync();

        /// <summary>
        /// 安全地执行需要跨服务协作的操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <returns>操作结果</returns>
        Task<T> ExecuteOperationAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// 同步执行服务间协调操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <returns>操作结果</returns>
        T ExecuteOperation<T>(Func<T> operation);

        /// <summary>
        /// 获取服务的运行时状态信息
        /// </summary>
        /// <returns>服务状态信息</returns>
        ServiceCoordinatorStatus GetStatus();

        /// <summary>
        /// 强制初始化PlaylistDataService，用于诊断和修复问题
        /// </summary>
        /// <returns>初始化是否成功</returns>
        Task<bool> ForceInitializePlaylistDataServiceAsync();
    }

    /// <summary>
    /// 服务协调器状态
    /// </summary>
    public class ServiceCoordinatorStatus
    {
        public bool IsInitialized { get; set; }
        public DateTime InitializationTime { get; set; }
        public int ActiveOperationsCount { get; set; }
        public string[] ServiceStatuses { get; set; } = Array.Empty<string>();
    }
}
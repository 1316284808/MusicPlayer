using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 统一生命周期管理服务
    /// 负责协调所有服务的生命周期，防止内存泄漏和服务异常
    /// </summary>
    public class LifecycleManagementService : IHostedService, IDisposable
    {
        private readonly ILogger<LifecycleManagementService>? _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IDisposable> _managedServices = new();
        private readonly object _lockObject = new();
        private bool _disposed = false;
        private Timer? _healthCheckTimer;

        public LifecycleManagementService(IServiceProvider serviceProvider, ILogger<LifecycleManagementService>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("生命周期管理服务启动");

            // 启动健康检查定时器（每30秒检查一次）
            _healthCheckTimer = new Timer(PerformHealthCheck, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("生命周期管理服务停止");

            // 停止健康检查定时器
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;

            // 优雅地释放所有托管服务
            await DisposeManagedServicesAsync(cancellationToken);
        }

        /// <summary>
        /// 注册需要管理的服务
        /// </summary>
        public void RegisterService<T>(T service) where T : IDisposable
        {
            lock (_lockObject)
            {
                if (_disposed) return;

                if (service != null && !_managedServices.Contains(service))
                {
                    _managedServices.Add(service);
                    _logger?.LogDebug($"注册生命周期管理服务: {typeof(T).Name}");
                }
            }
        }

        /// <summary>
        /// 取消注册服务
        /// </summary>
        public void UnregisterService<T>(T service) where T : IDisposable
        {
            lock (_lockObject)
            {
                if (_managedServices.Contains(service))
                {
                    _managedServices.Remove(service);
                    _logger?.LogDebug($"取消注册生命周期管理服务: {typeof(T).Name}");
                }
            }
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        private void PerformHealthCheck(object? state)
        {
            if (_disposed) return;

            try
            {
                // 检查内存使用情况
                var memoryUsage = GC.GetTotalMemory(false);
                var memoryUsageMB = memoryUsage / (1024.0 * 1024.0);

                if (memoryUsageMB > 200) // 如果内存使用超过200MB，触发垃圾回收
                {
                    _logger?.LogWarning($"内存使用过高: {memoryUsageMB:F2} MB，触发垃圾回收");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                // 记录当前托管的服务数量
                _logger?.LogDebug($"当前托管服务数量: {_managedServices.Count}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "健康检查时发生错误");
            }
        }

        /// <summary>
        /// 异步释放所有托管服务
        /// </summary>
        private async Task DisposeManagedServicesAsync(CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                if (_disposed) return;
            }

            var servicesToDispose = new List<IDisposable>();

            // 复制列表以避免并发修改
            lock (_lockObject)
            {
                servicesToDispose.AddRange(_managedServices);
                _managedServices.Clear();
            }

            // 按照依赖关系顺序释放服务（先释放业务服务，再释放基础设施服务）
            var disposalOrder = servicesToDispose.OrderByDescending(s => GetServicePriority(s.GetType()));

            foreach (var service in disposalOrder)
            {
                try
                {
                    // 检查是否是可异步释放的服务
                    var serviceType = service.GetType();
                    var disposeAsyncMethod = serviceType.GetMethod("DisposeAsync", new Type[] { typeof(CancellationToken) });
                    
                    if (disposeAsyncMethod != null && disposeAsyncMethod.ReturnType == typeof(Task))
                    {
                        // 调用异步释放方法
                        var task = (Task)disposeAsyncMethod.Invoke(service, new object[] { cancellationToken });
                        await task;
                    }
                    else
                    {
                        service.Dispose();
                    }

                    _logger?.LogDebug($"成功释放服务: {service.GetType().Name}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"释放服务 {service.GetType().Name} 时发生错误");
                }
            }
        }

        /// <summary>
        /// 获取服务释放优先级（数值越大越早释放）
        /// </summary>
        private int GetServicePriority(Type serviceType)
        {
            // 基础设施服务优先级较高
            if (serviceType.Name.Contains("Coordinator") || 
                serviceType.Name.Contains("Messaging") ||
                serviceType.Name.Contains("Configuration"))
            {
                return 100;
            }

            // 业务服务优先级中等
            if (serviceType.Name.Contains("Player") || 
                serviceType.Name.Contains("Playlist"))
            {
                return 50;
            }

            // UI相关服务优先级最低
            if (serviceType.Name.Contains("ViewModel") || 
                serviceType.Name.Contains("View"))
            {
                return 10;
            }

            return 30; // 默认优先级
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 同步释放所有资源
                StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                _disposed = true;

                _logger?.LogInformation("生命周期管理服务已释放");
            }
        }
    }
}
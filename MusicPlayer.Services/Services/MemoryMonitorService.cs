using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 内存监控服务 - 实时监控应用程序内存使用情况
    /// 提供内存使用统计、预警和自动清理功能
    /// </summary>
    public partial class MemoryMonitorService : ObservableObject, IDisposable
    {
        private readonly System.Timers.Timer _monitorTimer;
        private readonly long _memoryThreshold;
        private bool _isDisposed = false;
        
        /// <summary>
        /// 当前内存使用量（MB）
        /// </summary>
        [ObservableProperty]
        private double _currentMemoryUsage;
        
        /// <summary>
        /// 内存使用百分比（相对于阈值）
        /// </summary>
        [ObservableProperty]
        private double _memoryUsagePercentage;
        
        /// <summary>
        /// 是否超过内存阈值
        /// </summary>
        [ObservableProperty]
        private bool _isMemoryCritical;
        
        /// <summary>
        /// 内存监控状态
        /// </summary>
        [ObservableProperty]
        private string _memoryStatus = "正常";
        
        /// <summary>
        /// 内存清理事件 - 当需要清理内存时触发
        /// </summary>
        public event EventHandler<MemoryCleanupEventArgs>? MemoryCleanupRequested;
        
        /// <summary>
        /// 内存状态变化事件
        /// </summary>
        public event EventHandler<MemoryStatusChangedEventArgs>? MemoryStatusChanged;
        
        public MemoryMonitorService(long memoryThresholdMB = 300)
        {
            _memoryThreshold = memoryThresholdMB * 1024 * 1024; // 转换为字节
            
            // 创建监控定时器，每5秒检查一次内存
            _monitorTimer = new System.Timers.Timer(5000); // 5秒间隔
            _monitorTimer.Elapsed += OnMonitorTimerElapsed;
            _monitorTimer.AutoReset = true;
            _monitorTimer.Start();
            
            // 立即执行一次内存检查
            CheckMemoryUsage();
        }
        
        /// <summary>
        /// 定时器触发时检查内存使用情况
        /// </summary>
        private void OnMonitorTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            CheckMemoryUsage();
        }
        
        /// <summary>
        /// 检查内存使用情况
        /// </summary>
        private void CheckMemoryUsage()
        {
            try
            {
                // 获取当前进程的内存使用情况
                var currentProcess = Process.GetCurrentProcess();
                var memoryBytes = currentProcess.WorkingSet64;
                
                // 转换为MB
                CurrentMemoryUsage = Math.Round(memoryBytes / (1024.0 * 1024.0), 2);
                
                // 计算使用百分比
                MemoryUsagePercentage = Math.Min(100, (CurrentMemoryUsage * 1024 * 1024) / _memoryThreshold * 100);
                
                // 检查是否超过阈值
                var wasCritical = IsMemoryCritical;
                IsMemoryCritical = memoryBytes > _memoryThreshold;
                
                // 更新内存状态
                UpdateMemoryStatus();
                
                // 如果内存状态从正常变为临界，触发清理事件
                if (IsMemoryCritical && !wasCritical)
                {
                    RequestMemoryCleanup();
                }
                
                // 触发状态变化事件
                MemoryStatusChanged?.Invoke(this, new MemoryStatusChangedEventArgs
                {
                    CurrentMemoryMB = CurrentMemoryUsage,
                    UsagePercentage = MemoryUsagePercentage,
                    IsCritical = IsMemoryCritical
                });
            }
            catch (Exception ex)
            {
                // 记录错误但不要抛出异常，避免影响应用程序
                Debug.WriteLine($"内存监控错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新内存状态描述
        /// </summary>
        private void UpdateMemoryStatus()
        {
            if (IsMemoryCritical)
            {
                MemoryStatus = "临界";
            }
            else if (MemoryUsagePercentage > 80)
            {
                MemoryStatus = "警告";
            }
            else if (MemoryUsagePercentage > 60)
            {
                MemoryStatus = "注意";
            }
            else
            {
                MemoryStatus = "正常";
            }
        }
        
        /// <summary>
        /// 请求内存清理
        /// </summary>
        private void RequestMemoryCleanup()
        {
            var args = new MemoryCleanupEventArgs
            {
                CurrentMemoryMB = CurrentMemoryUsage,
                ThresholdMB = _memoryThreshold / (1024 * 1024),
                RequestedAt = DateTime.Now
            };
            
            MemoryCleanupRequested?.Invoke(this, args);
            
            // 执行自动清理
            PerformMemoryCleanup();
        }
        
        /// <summary>
        /// 执行内存清理操作
        /// </summary>
        private void PerformMemoryCleanup()
        {
            try
            {
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // 等待一段时间让GC完成工作
                System.Threading.Thread.Sleep(100);
                
                // 重新检查内存使用情况
                CheckMemoryUsage();
                
                Debug.WriteLine($"内存清理完成，当前内存使用: {CurrentMemoryUsage}MB");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"内存清理错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 手动触发内存清理
        /// </summary>
        public void ManualCleanup()
        {
            PerformMemoryCleanup();
        }
        
        /// <summary>
        /// 获取内存使用统计信息
        /// </summary>
        public MemoryStats GetMemoryStats()
        {
            return new MemoryStats
            {
                CurrentMemoryMB = CurrentMemoryUsage,
                ThresholdMB = _memoryThreshold / (1024 * 1024),
                UsagePercentage = MemoryUsagePercentage,
                IsCritical = IsMemoryCritical,
                Status = MemoryStatus,
                LastChecked = DateTime.Now
            };
        }
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _monitorTimer?.Stop();
                _monitorTimer?.Dispose();
                _isDisposed = true;
            }
        }
    }
    
    /// <summary>
    /// 内存统计信息
    /// </summary>
    public class MemoryStats
    {
        public double CurrentMemoryMB { get; set; }
        public double ThresholdMB { get; set; }
        public double UsagePercentage { get; set; }
        public bool IsCritical { get; set; }
        public string Status { get; set; } = "正常";
        public DateTime LastChecked { get; set; }
    }
    
    /// <summary>
    /// 内存清理事件参数
    /// </summary>
    public class MemoryCleanupEventArgs : EventArgs
    {
        public double CurrentMemoryMB { get; set; }
        public double ThresholdMB { get; set; }
        public DateTime RequestedAt { get; set; }
    }
    
    /// <summary>
    /// 内存状态变化事件参数
    /// </summary>
    public class MemoryStatusChangedEventArgs : EventArgs
    {
        public double CurrentMemoryMB { get; set; }
        public double UsagePercentage { get; set; }
        public bool IsCritical { get; set; }
    }
}
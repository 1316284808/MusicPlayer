using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 频谱分析器视图模型 - 专门负责处理音频频谱数据的显示和更新
    /// 实现为全局单例模式，确保频谱数据在应用程序范围内保持一致
    /// 单例模式下始终处于激活状态，不需要Activate/Deactivate控制
    /// </summary>
    public class SpectrumAnalyzerViewModel : ObservableObject, ISpectrumAnalyzerViewModel, IViewModelLifecycle
    {
        private readonly IMessagingService _messagingService;
        private bool _disposed = false;
        private bool _isWindowMaximized = false;
        private static readonly object _lockObject = new object(); // 用于线程同步

        /// <summary>
        /// 音频频谱数据 - 使用ObservableCollection确保实时更新
        /// </summary>
        public ObservableCollection<double> SpectrumData { get; } = new ObservableCollection<double>();

      

        /// <summary>
        /// 窗口是否最大化
        /// </summary>
        public bool IsWindowMaximized
        {
            get => _isWindowMaximized;
            set
            {
                if (_isWindowMaximized != value)
                {
                    _isWindowMaximized = value;
                    OnPropertyChanged(nameof(IsWindowMaximized));
                    
                    // 根据窗口状态更新频谱尺寸参数
                    UpdateSpectrumDimensions();
                }
            }
        }

        /// <summary>
        /// 圆形频谱中心X坐标
        /// </summary>
        private double _centerX = 150;
        public double CenterX 
        { 
            get => _centerX; 
            set 
            { 
                if (_centerX != value) 
                { 
                    _centerX = value; 
                    OnPropertyChanged(nameof(CenterX)); 
                } 
            } 
        }

        /// <summary>
        /// 圆形频谱中心Y坐标
        /// </summary>
        private double _centerY = 150;
        public double CenterY 
        { 
            get => _centerY; 
            set 
            { 
                if (_centerY != value) 
                { 
                    _centerY = value; 
                    OnPropertyChanged(nameof(CenterY)); 
                } 
            } 
        }

        /// <summary>
        /// 圆形频谱内圆半径
        /// </summary>
        private double _innerRadius = 75;
        public double InnerRadius 
        { 
            get => _innerRadius; 
            set 
            { 
                if (_innerRadius != value) 
                { 
                    _innerRadius = value; 
                    OnPropertyChanged(nameof(InnerRadius)); 
                } 
            } 
        }

        /// <summary>
        /// 圆形频谱最大条高度
        /// </summary>
        private double _maxBarHeight = 75;
        public double MaxBarHeight 
        { 
            get => _maxBarHeight; 
            set 
            { 
                if (_maxBarHeight != value) 
                { 
                    _maxBarHeight = value; 
                    OnPropertyChanged(nameof(MaxBarHeight)); 
                } 
            } 
        }

        /// <summary>
        /// 根据窗口状态更新频谱尺寸参数
        /// </summary>
        private void UpdateSpectrumDimensions()
        {
            if (IsWindowMaximized)
            {
                 
                CenterX = 225;
                CenterY = 225;
                InnerRadius = 225;  // 内径等于封面半径，使频谱从封面边缘开始
                MaxBarHeight = 125; // 最大条高度为封面半径的一半
            }
            else
            {
                // 正常时：频谱直径300，中心点(150,150)，内径150(封面半径)，最大高度75
                CenterX = 150;
                CenterY = 150;
                InnerRadius = 150;  // 内径等于封面半径，使频谱从封面边缘开始
                MaxBarHeight = 75; // 最大条高度为封面半径的一半
            }
            
            System.Diagnostics.Debug.WriteLine($"SpectrumAnalyzerViewModel: 窗口状态变化，更新频谱尺寸为 {(IsWindowMaximized ? "最大化" : "正常")}，" +
                $"中心点({CenterX},{CenterY})，内径{InnerRadius}，最大高度{MaxBarHeight}");
                
            // 强制刷新频谱数据，触发UI更新
            for (int i = 0; i < SpectrumData.Count; i++)
            {
                OnPropertyChanged($"SpectrumData[{i}]");
            }
        }

        public SpectrumAnalyzerViewModel(IMessagingService messagingService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            
            System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: 构造函数开始执行");
            
            // 初始化频谱数据（32个频段）
            InitializeSpectrumData();
            
            // 注册消息处理器
            RegisterMessageHandlers();
            
           
         }

        /// <summary>
        /// 初始化频谱数据
        /// </summary>
        private void InitializeSpectrumData()
        {
            // 初始化32个频段的数据
            for (int i = 0; i < 32; i++)
            {
                SpectrumData.Add(0.02); // 设置为最小可见值
            }
            
            System.Diagnostics.Debug.WriteLine($"SpectrumAnalyzerViewModel: 已初始化{SpectrumData.Count}个频段数据");
        }

        /// <summary>
        /// 更新频谱数据到ObservableCollection，使用平滑过渡确保实时更新
        /// 单例模式下始终处理数据，不需要检查激活状态
        /// </summary>
        private void UpdateSpectrumData(float[] newSpectrumData)
        {
            lock (_lockObject)
            {
                // 只有在ViewModel未被释放时才更新数据
                if (_disposed)
                    return;
                    
                try
                {
                    // 调试信息：记录接收到的数据
                    if (newSpectrumData == null)
                    {
                        System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: 接收到null频谱数据");
                        return;
                    }
                    
                    if (newSpectrumData.Length == 0)
                    {

                        System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: 接收到空频谱数组，可能是频谱被禁用");
                       // return;
                    }
                    
                    // 确保频谱数据长度匹配
                    int bandsCount = Math.Min(SpectrumData.Count, newSpectrumData.Length);
                    
                    
                    
                    for (int i = 0; i < bandsCount; i++)
                    {
                        float bandValue = newSpectrumData[i];
                        
                        // 数据有效性检查
                        if (float.IsNaN(bandValue) || float.IsInfinity(bandValue))
                        {
                            bandValue = 0.02f; // 设置为最小可见值
                        }
                        
                        // 应用对数缩放
                        bandValue = (float)(Math.Log10(1 + bandValue * 9999) / 4.0); // Log scale 0-1
                        
                        // 更快的衰减率，减少内存中的数据停留时间
                        var currentValue = SpectrumData[i];
                        var smoothedValue = currentValue * 0.6 + bandValue * 0.4; // 更快的衰减
                        
                        // 应用最小阈值，确保视觉效果
                        smoothedValue = Math.Max(0.02, smoothedValue);
                        
                        // 限制在0-1范围内
                        SpectrumData[i] = Math.Max(0, Math.Min(1, smoothedValue));
                    }
                }
                catch
                {
                    // 出错时快速清空频谱数据
                    ClearSpectrumData();
                }
            }
        }

        /// <summary>
        /// 清空频谱数据
        /// </summary>
        private void ClearSpectrumData()
        {
            for (int i = 0; i < SpectrumData.Count; i++)
            {
                SpectrumData[i] = 0.02; // 设置为最小可见值
            }
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 播放状态变化消息
            _messagingService.Register<PlaybackStateChangedMessage>(this, (r, m) =>
            {
                lock (_lockObject)
                {
                    // 如果播放停止，清空频谱数据
                    if (!m.Value)
                    {
                        ClearSpectrumData();
                    }
                }
            });

            // 频谱数据更新消息 - 单例模式下始终处理数据
            _messagingService.Register<SpectrumDataUpdatedMessage>(this, (r, m) =>
            {
                // 只有在ViewModel未被释放时才处理数据
                if (!_disposed)
                {
                    UpdateSpectrumData(m.Value);
                }
            });

            // 窗口状态变化消息
            _messagingService.Register<WindowStateChangedMessage>(this, (r, m) =>
            {
                IsWindowMaximized = m.Value == WindowState.Maximized;
            });
        }

       

        #region IViewModelLifecycle Implementation

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public new void Initialize()
        {
            // 频谱分析器在初始化时不做特殊处理，采用懒加载
            System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: Initialized with lazy loading");
        }

        /// <summary>
        /// 激活ViewModel - 单例模式下不需要，保留以兼容接口
        /// </summary>
        public void Activate()
        {
            // 单例模式下始终处于激活状态，此方法为空实现
            System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: Activate调用 - 单例模式下无操作");
        }

        /// <summary>
        /// 停用ViewModel - 单例模式下不需要，保留以兼容接口
        /// </summary>
        public void Deactivate()
        {
            // 单例模式下始终处于激活状态，此方法为空实现
            System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: Deactivate调用 - 单例模式下无操作");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public new void Cleanup()
        {
            lock (_lockObject)
            {
                if (!_disposed)
                { 
                    // 取消注册所有消息处理器
                    _messagingService?.Unregister(this);
                    
                    // 清空频谱数据
                    ClearSpectrumData();
                    SpectrumData.Clear();
                    
                    _disposed = true;
                    System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerViewModel: Cleaned up - 资源已释放");
                }
            }
        }

        #endregion
    }
}
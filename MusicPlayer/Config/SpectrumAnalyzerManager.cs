using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Core.Interface;
using MusicPlayer.ViewModels;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Config
{
    /// <summary>
    /// 全局频谱分析管理器
    /// 负责统一管理频谱控件，提供刷新和统计功能
    /// 由于SpectrumAnalyzerViewModel是单例，不再需要激活/停用逻辑
    /// </summary>
    public interface ISpectrumAnalyzerManager
    {
        /// <summary>
        /// 注册频谱控件
        /// </summary>
        /// <param name="controlId">控件唯一标识</param>
        void RegisterControl(string controlId);
        
        /// <summary>
        /// 取消注册频谱控件
        /// </summary>
        /// <param name="controlId">控件唯一标识</param>
        void UnregisterControl(string controlId);
        
        /// <summary>
        /// 获取当前注册的控件数量
        /// </summary>
        int RegisteredControlCount { get; }
        
        /// <summary>
        /// 强制刷新所有频谱控件
        /// </summary>
        void RefreshAllControls();
        
        /// <summary>
        /// 初始化频谱分析器
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 清理资源
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// 全局频谱分析管理器实现
    /// </summary>
    public class SpectrumAnalyzerManager : ISpectrumAnalyzerManager
    {
        private readonly ISpectrumAnalyzerViewModel _spectrumViewModel;
        private readonly IMessagingService _messagingService;
        private readonly HashSet<string> _registeredControls; // 控件ID集合
        private readonly object _lockObject = new object();
        private bool _isInitialized = false;
        private bool _isDisposed = false;

        public int RegisteredControlCount 
        { 
            get 
            { 
                lock (_lockObject)
                {
                    return _registeredControls.Count;
                }
            } 
        }

        public SpectrumAnalyzerManager(
            ISpectrumAnalyzerViewModel spectrumViewModel,
            IMessagingService messagingService)
        {
            _spectrumViewModel = spectrumViewModel ?? throw new ArgumentNullException(nameof(spectrumViewModel));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _registeredControls = new HashSet<string>();
            
            System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerManager: 已创建");
        }

        /// <summary>
        /// 注册频谱控件
        /// </summary>
        /// <param name="controlId">控件唯一标识</param>
        public void RegisterControl(string controlId)
        {
            if (string.IsNullOrEmpty(controlId))
                throw new ArgumentException("控件ID不能为空", nameof(controlId));
                
            lock (_lockObject)
            {
                if (_isDisposed)
                    return;
                    
                if (_registeredControls.Add(controlId))
                {
                    System.Diagnostics.Debug.WriteLine($"SpectrumAnalyzerManager: 注册控件 {controlId}，当前控件数量: {_registeredControls.Count}");
                    
                    // 如果这是第一个注册的控件，确保频谱分析器已初始化
                    if (!_isInitialized)
                    {
                        Initialize();
                    }
                }
            }
        }

        /// <summary>
        /// 取消注册频谱控件
        /// </summary>
        /// <param name="controlId">控件唯一标识</param>
        public void UnregisterControl(string controlId)
        {
            if (string.IsNullOrEmpty(controlId))
                return;
                
            lock (_lockObject)
            {
                if (_isDisposed || !_registeredControls.Contains(controlId))
                    return;
                    
                _registeredControls.Remove(controlId);
                System.Diagnostics.Debug.WriteLine($"SpectrumAnalyzerManager: 取消注册控件 {controlId}，剩余控件数量: {_registeredControls.Count}");
                
                // 单例模式下不需要停用频谱分析器，因为它是全局共享的
            }
        }



        /// <summary>
        /// 强制刷新所有频谱控件
        /// </summary>
        public void RefreshAllControls()
        {
            lock (_lockObject)
            {
                if (_isDisposed)
                    return;
                    
                // 发送窗口状态变化消息，触发所有控件刷新
                _messagingService.Send(new WindowStateChangedMessage(System.Windows.WindowState.Normal));
                System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerManager: 已发送刷新信号到所有控件");
            }
        }

        /// <summary>
        /// 初始化频谱分析器
        /// </summary>
        public void Initialize()
        {
            lock (_lockObject)
            {
                if (_isDisposed || _isInitialized)
                    return;
                    
                if (_spectrumViewModel != null)
                {
                    _spectrumViewModel.Initialize();
                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerManager: 频谱分析器已初始化");
                }
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            lock (_lockObject)
            {
                if (_isDisposed)
                    return;
                    
                _registeredControls.Clear();
                
                if (_spectrumViewModel != null)
                {
                    _spectrumViewModel.Deactivate();
                    _spectrumViewModel.Cleanup();
                }
                
                _isDisposed = true;
                System.Diagnostics.Debug.WriteLine("SpectrumAnalyzerManager: 资源已清理");
            }
        }
    }
}
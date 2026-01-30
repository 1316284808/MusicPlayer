using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Config;
using MusicPlayer.Core.Interface;
using MusicPlayer.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 圆形频谱分析器控件
     /// </summary>
    public partial class CircularSpectrumControl : UserControl, IDisposable
    {
        private ISpectrumAnalyzerViewModel? _spectrumViewModel;
        private ISpectrumAnalyzerManager? _spectrumManager;
      

        public CircularSpectrumControl()
        {
            InitializeComponent();
            
            // 立即获取并设置频谱ViewModel作为DataContext，确保初始化时绑定正确
            InitializeSpectrumViewModel();
            
            // 添加页面导航事件处理
            this.IsVisibleChanged += CircularSpectrumControl_IsVisibleChanged;
           
            // 添加尺寸变化事件处理
            this.SizeChanged += CircularSpectrumControl_SizeChanged;
            
        }
        
        /// <summary>
        /// 初始化频谱ViewModel
        /// </summary>
        private void InitializeSpectrumViewModel()
        {
            try
            {
                if (App.Current is App app && app.ServiceProvider != null)
                {
                    _spectrumViewModel = app.ServiceProvider.GetService<ISpectrumAnalyzerViewModel>();
                    if (_spectrumViewModel != null)
                    {
                        this.DataContext = _spectrumViewModel;
                        System.Diagnostics.Debug.WriteLine("CircularSpectrumControl: 在构造函数中成功设置DataContext为ISpectrumAnalyzerViewModel");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("CircularSpectrumControl: 无法获取ISpectrumAnalyzerViewModel服务");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CircularSpectrumControl: 无法获取App或ServiceProvider");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: 初始化频谱ViewModel失败 - {ex.Message}");
            }
        }

        private void CircularSpectrumControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                // 当控件可见性变化时，确保ViewModel正确设置并更新尺寸参数
                if (e.NewValue is bool isVisible && isVisible)
                {
                    // 更新频谱尺寸参数
                    UpdateSpectrumDimensionsForCurrentWindowState();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: IsVisibleChanged事件处理失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新当前窗口状态的频谱尺寸参数
        /// </summary>
        private void UpdateSpectrumDimensionsForCurrentWindowState()
        {
            if (_spectrumViewModel == null) return;
            
            try
            {
                // 获取当前窗口的实际状态
                bool actualIsMaximized = false;
                try
                {
                    // 尝试获取主窗口
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        actualIsMaximized = mainWindow.WindowState == WindowState.Maximized;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: 获取窗口状态失败 - {ex.Message}");
                }
                
                // 更新ViewModel的窗口状态
                if (_spectrumViewModel.IsWindowMaximized != actualIsMaximized)
                {
                    _spectrumViewModel.IsWindowMaximized = actualIsMaximized;
                }
                
                // 更新尺寸参数
                bool isMaximized = _spectrumViewModel.IsWindowMaximized;
                if (isMaximized)
                {
                    _spectrumViewModel.CenterX = 225;
                    _spectrumViewModel.CenterY = 225;
                    _spectrumViewModel.InnerRadius = 225;  // 内径等于封面半径，使频谱从封面边缘开始
                    _spectrumViewModel.MaxBarHeight = 125; // 最大条高度为封面半径的一半
                }
                else
                {
                    // 正常时：频谱直径300，中心点(150,150)，内径150(封面半径)，最大高度75
                    _spectrumViewModel.CenterX = 150;
                    _spectrumViewModel.CenterY = 150;
                    _spectrumViewModel.InnerRadius = 150;  // 内径等于封面半径，使频谱从封面边缘开始
                    _spectrumViewModel.MaxBarHeight = 75; // 最大条高度为封面半径的一半
                }
                
                // 添加调试信息
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: 窗口状态为{(isMaximized ? "最大化" : "正常")}，" +
                    $"更新频谱参数 - 中心点({_spectrumViewModel.CenterX},{_spectrumViewModel.CenterY})，内径{_spectrumViewModel.InnerRadius}，最大高度{_spectrumViewModel.MaxBarHeight}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: 更新频谱尺寸参数失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理控件尺寸变化事件
        /// </summary>
        private void CircularSpectrumControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (_spectrumViewModel != null)
                {
                    // 调用统一的尺寸更新方法
                    UpdateSpectrumDimensionsForCurrentWindowState();
                    System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: 尺寸变化，已更新频谱参数");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumControl: SizeChanged事件处理失败 - {ex.Message}");
            }
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消事件订阅
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    this.IsVisibleChanged -= CircularSpectrumControl_IsVisibleChanged;
                    this.SizeChanged -= CircularSpectrumControl_SizeChanged;

                    // 释放资源
                    _spectrumViewModel = null;
                    _spectrumManager = null;
                    
                    // 清空DataContext，解除对ViewModel的强引用
                    this.DataContext = null;
                    
                    // 清空UI内容，释放相关资源
                    this.Content = null;
                    
                    // 调用GC.Collect，确保资源被及时回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Diagnostics.Debug.WriteLine("CircularSpectrumControl: 已释放所有资源");
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
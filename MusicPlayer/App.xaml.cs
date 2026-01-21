using System.Windows;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;
using MusicPlayer.Config;
using MusicPlayer.ViewModels;
using MusicPlayer.Core.Interface;
using Hardcodet.Wpf.TaskbarNotification;
using MusicPlayer.Navigation;
using Wpf.Ui;
using Wpf.Ui.Controls;
using System.Windows.Controls;

namespace MusicPlayer;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal IServiceProvider? ServiceProvider => _serviceProvider;
        private IServiceProvider? _serviceProvider;
        internal AppStartup Startup { get; private set; } = new();
        private IViewModelLifecycleManager? _lifecycleManager;
        private TaskbarIcon? _notifyIcon;
        // 用于检测单实例的互斥体
        private Mutex? _mutex;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 创建全局唯一的互斥体，用于检测是否已有实例运行
                _mutex = new Mutex(true, "Global\\MusicPlayerMutex", out bool isNewInstance);
                
                if (!isNewInstance)
                {
                    // 已有实例运行，直接退出
                    _mutex.Dispose();
                    Shutdown();
                    return;
                }

                base.OnStartup(e);
                
                // 配置和启动依赖注入服务
                _serviceProvider = await this.ConfigureServicesAsync(Startup);
                // 获取ViewModel生命周期管理器
                _lifecycleManager = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IViewModelLifecycleManager>(_serviceProvider);
                
                // 通过依赖注入创建和显示主窗口，直接注入所有必需的ViewModel和服务
                var mainWindow = new MainWindow(
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMainViewModel>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Navigation.NavigationService>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IServiceCoordinator>(_serviceProvider!),
                    //Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IBackgroundViewModel>(_serviceProvider!),
                     Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService <ISettingsPageViewModel>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IPlaylistViewModel>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IControlBarViewModel>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ITitleBarViewModel>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsBarViewModel>(_serviceProvider!),
                    Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<WindowManagerService>(_serviceProvider!)
                );
                // 获取并初始化WPF-UI服务
                var snackbarService = _serviceProvider?.GetRequiredService<ISnackbarService>();
                var contentDialogService = _serviceProvider?.GetRequiredService<IContentDialogService>();
                
                // 设置SnackbarPresenter和ContentPresenter
                if (snackbarService is SnackbarService snackbarServiceImpl) {
                    snackbarServiceImpl.SetSnackbarPresenter(mainWindow.FindName("MainSnackbarPresenter") as SnackbarPresenter);
                }
                if (contentDialogService is ContentDialogService contentDialogServiceImpl) {
                    contentDialogServiceImpl.SetDialogHost(mainWindow.FindName("RootContentDialog") as ContentPresenter);
                }
                
                mainWindow.Show();
                
                // 初始化频谱分析管理器
                InitializeSpectrumAnalyzerManager();
                
                // 初始化托盘图标
                InitializeNotifyIcon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Application startup failed: {ex.Message}");
              
                Shutdown();
            }
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeNotifyIcon()
        {
            try
            {
                // 获取资源中定义的TaskbarIcon
                _notifyIcon = this.FindResource("NotifyIcon") as TaskbarIcon;
                if (_notifyIcon != null)
                {
                    // 设置DataContext
                    _notifyIcon.DataContext = _serviceProvider?.GetService<IMainViewModel>();
                    
                    System.Diagnostics.Debug.WriteLine("系统托盘图标初始化成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("无法获取系统托盘图标资源");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化系统托盘图标失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理系统托盘图标左键点击事件
        /// </summary>
        private void TaskbarIcon_LeftClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = this.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mainWindow != null)
                {
                    if (mainWindow.Visibility == Visibility.Visible)
                    {
                        // 如果窗口可见，则最小化到托盘
                        mainWindow.Hide();
                    }
                    else
                    {
                        // 如果窗口隐藏，则显示并激活
                        mainWindow.Show();
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理系统托盘左键点击失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化频谱分析管理器
        /// </summary>
        private void InitializeSpectrumAnalyzerManager()
        {
            try
            {
                if (_serviceProvider != null)
                {
                    var spectrumManager = _serviceProvider.GetService<MusicPlayer.Config.ISpectrumAnalyzerManager>();
                    if (spectrumManager != null)
                    {
                        spectrumManager.Initialize();
                        System.Diagnostics.Debug.WriteLine("App: 频谱分析管理器已初始化");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("App: 无法获取频谱分析管理器");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App: 初始化频谱分析管理器失败: {ex.Message}");
            }
        }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        // 记录异常详细信息
        System.Diagnostics.Debug.WriteLine($"未处理的调度器异常: {e.Exception.Message}");
        System.Diagnostics.Debug.WriteLine($"异常类型: {e.Exception.GetType().Name}");
        System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {e.Exception.StackTrace}");
        System.Diagnostics.Debug.WriteLine($"内部异常: {e.Exception.InnerException?.Message}");
        
        // 检查是否是UI线程相关的异常或窗口状态变化相关的异常
        if (e.Exception.StackTrace?.Contains("OnWindowStateChanged") == true || 
            e.Exception.StackTrace?.Contains("WindowStateChangedMessage") == true ||
            e.Exception is System.InvalidOperationException ||
            e.Exception.Message.Contains("调用线程") ||
            e.Exception.Message.Contains("不同线程"))
        {
            System.Diagnostics.Debug.WriteLine("UI线程或窗口状态变化异常，忽略此异常");
            e.Handled = true; // 标记为已处理，阻止应用程序崩溃
        }
        else
        {
            // 对于其他类型的异常，记录但仍然尝试继续运行
            System.Diagnostics.Debug.WriteLine("其他类型异常，尝试继续运行应用程序");
            e.Handled = true;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                // 保存配置到SQLite数据库（先保存，再停止服务）
                SaveConfigurationOnExit();

                // 清理托盘图标
                _notifyIcon?.Dispose();

                // 清理频谱分析管理器
                if (_serviceProvider != null)
                {
                    var spectrumManager = _serviceProvider.GetService<MusicPlayer.Config.ISpectrumAnalyzerManager>();
                    spectrumManager?.Cleanup();
                    System.Diagnostics.Debug.WriteLine("App: 频谱分析管理器已清理");

                    // 手动释放IDisposable服务，特别是数据库连接
                    var configurationService = _serviceProvider.GetService<IConfigurationService>();
                    if (configurationService is IDisposable disposableConfig)
                    {
                        disposableConfig.Dispose();
                        System.Diagnostics.Debug.WriteLine("App: ConfigurationService已释放");
                    }

                    var equalizerPresetRepository = _serviceProvider.GetService<IEqualizerPresetRepository>();
                    if (equalizerPresetRepository is IDisposable disposableEqualizer)
                    {
                        disposableEqualizer.Dispose();
                        System.Diagnostics.Debug.WriteLine("App: EqualizerPresetRepository已释放");
                    }
                }

                // 清理所有ViewModel
                _lifecycleManager?.CleanupAllViewModels();

                // 停止服务
                await Startup.StopAsync();

                // 释放互斥体资源
                _mutex?.Dispose();

                System.Diagnostics.Debug.WriteLine("Application shutdown completed successfully");
            }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Application shutdown failed: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }
    
     
    
    /// <summary>
    /// 在程序退出时保存配置到SQLite数据库
    /// 不能是异步，避免写入失败
    /// </summary>
    private void SaveConfigurationOnExit()
    {
        try
        {
            // 获取配置服务和播放状态服务
            var configurationService = _serviceProvider?.GetService<IConfigurationService>(); 
            if (configurationService == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveConfigurationOnExit: 无法获取ConfigurationService服务");
                return;
            } 
            // 确保PlayerState的状态同步到内存配置，然后保存
            configurationService.SaveCurrentConfiguration();
            
            System.Diagnostics.Debug.WriteLine("SaveConfigurationOnExit: 成功同步并保存配置到SQLite数据库");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveConfigurationOnExit: 保存配置失败: {ex.Message}");
        }
    }
}
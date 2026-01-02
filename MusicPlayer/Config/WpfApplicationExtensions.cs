using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicPlayer.Config
{ /// <summary>
  /// WPF应用扩展方法
  /// </summary>
    public static class WpfApplicationExtensions
    {
        /// <summary>
        /// 配置WPF应用的依赖注入
        /// </summary>
        /// <param name="app">WPF应用实例</param>
        /// <param name="startup">应用启动配置</param>
        /// <returns>服务提供者</returns>
        public static async Task<IServiceProvider> ConfigureServicesAsync(
            this Application app,
            AppStartup startup)
        {
            // 处理未捕获的异常
            app.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 启动服务
            var serviceProvider = await startup.StartAsync();

            // 设置应用退出处理
            app.Exit += OnApplicationExit;

            return serviceProvider;
        }

        /// <summary>
        /// 处理WPF调度器未处理的异常
        /// </summary>
        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // 获取错误处理服务
                var serviceProvider = ((App)Application.Current).ServiceProvider;

                var errorHandling = serviceProvider.GetService<IErrorHandlingService>();

                // 记录异常
                errorHandling?.HandleException(e.Exception, "WPF Dispatcher", "未处理的调度器异常");
                Debug.WriteLine(e.Exception);
                // 标记异常已处理
                e.Handled = true;

                System.Diagnostics.Debug.WriteLine($"Dispatcher exception handled: {e.Exception.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle dispatcher exception: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理应用退出事件
        /// </summary>
        private static async void OnApplicationExit(object sender, ExitEventArgs e)
        {
            try
            {
                var app = (App)Application.Current;
                await app.Startup.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle application exit: {ex.Message}");
            }
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services;
using MusicPlayer.Services.Handlers;
using MusicPlayer.Services.Messages;
using MusicPlayer.Logging;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace MusicPlayer.Config
{
    /// <summary>
    /// 应用启动配置
    /// 负责初始化依赖注入容器和服务生命周期管理
    /// </summary>
    public class AppStartup
    {
        private IHost? _host;
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// 配置和启动应用
        /// </summary>
        /// <param name="configureServices">服务配置委托</param>
        /// <returns>服务提供者</returns>
        public async Task<IServiceProvider> StartAsync(Action<IServiceCollection>? configureServices = null)
        {
            try
            {
                // 创建服务集合
                var services = new ServiceCollection();

                // 配置应用服务
                ConfigureServices(services);

                // 应用额外的服务配置
                configureServices?.Invoke(services);

                // 创建并启动主机（这里会自动创建服务提供者）
                _host = CreateHost(services);
                
                // 从主机获取服务提供者，确保只有一个服务提供者实例
                _serviceProvider = _host.Services;
                
                // 启动主机
                await _host.StartAsync();
              
                System.Diagnostics.Debug.WriteLine("Application services started successfully");

                return _serviceProvider;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start application: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止应用
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                if (_host != null)
                {
                    await _host.StopAsync();
                    await _host.WaitForShutdownAsync();
                }

                System.Diagnostics.Debug.WriteLine("Application services stopped successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to stop application: {ex.Message}");
            }
            finally
            {
                _host?.Dispose();
                _serviceProvider = null;
                _host = null;
            }
        }

        /// <summary>
        /// 配置核心服务
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // 添加MusicPlayer服务
            services.AddMusicPlayerServices();

            // 配置应用选项
            services.AddConfigurationServices();

            // 添加主机服务
            services.AddSingleton<IHostedService, ApplicationInitializationService>();
        }

        /// <summary>
        /// 创建应用主机
        /// </summary>
        private IHost CreateHost(IServiceCollection services)
        {
            
            return new HostBuilder()
                .ConfigureServices((context, hostingServices) => {
                    foreach (var service in services)
                    {
                        hostingServices.Add(service);
                    }
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFileWithTimestamp(Paths.LogsDirectory, LogLevel.Error);
                    logging.SetMinimumLevel(LogLevel.Error); // 只记录Error级别及以上
                })
                .Build();
        }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public IServiceProvider? ServiceProvider => _serviceProvider;
    }
     
}
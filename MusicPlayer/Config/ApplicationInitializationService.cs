using Microsoft.Extensions.Hosting;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Config
{
    /// <summary>
    /// 应用初始化服务
    /// 负责在应用启动时执行初始化逻辑
    /// </summary>
    public class ApplicationInitializationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorHandlingService _errorHandling;
        private readonly IMessagingService _messagingService;

        public ApplicationInitializationService(
            IServiceProvider serviceProvider,
            IErrorHandlingService errorHandling,
            IMessagingService messagingService)
        {
            _serviceProvider = serviceProvider;
            _errorHandling = errorHandling;
            _messagingService = messagingService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting application initialization...");

                // 发送应用启动消息
                _messagingService.Send(new ApplicationStartedMessage());

                // 记录启动信息
                _errorHandling.LogInfo("MusicPlayer application started", "ApplicationInitialization");

                // 初始化其他服务（如果需要）
                await InitializeServicesAsync();

                System.Diagnostics.Debug.WriteLine("Application initialization completed successfully");
            }
            catch (Exception ex)
            {
                _errorHandling.HandleException(ex, "ApplicationInitialization", "Failed to initialize application");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Stopping application initialization service...");

                // 发送应用关闭消息
                _messagingService.Send(new ApplicationClosingMessage
                {
                    CanCancel = false,
                    Reason = "Application shutdown"
                });

                // 记录关闭信息
                _errorHandling.LogInfo("MusicPlayer application shutting down", "ApplicationInitialization");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _errorHandling.HandleException(ex, "ApplicationInitialization", "Failed to shutdown application");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 初始化其他服务
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            // 准备播放列表文件 - 在所有服务初始化完成后执行
            try
            {
                // 发送应用启动完成消息，通知UI准备就绪
                _messagingService.Send(new ApplicationInitializationCompletedMessage());
            }
            catch (Exception ex)
            {
                _errorHandling.LogWarning("Failed to prepare playlist on startup: " + ex.Message, "ApplicationInitialization");
            }
        }


    }
}

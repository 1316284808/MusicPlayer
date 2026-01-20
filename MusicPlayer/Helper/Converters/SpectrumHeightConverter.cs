using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Services.Messages;
using MusicPlayer.Services;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Converters
{
    public class SpectrumHeightConverter : IValueConverter, IDisposable
    {
        private static bool _isSpectrumEnabled = true;
        private static Core.Interface.IMessagingService _messagingService;
        private static bool _disposed = false;
        
        /// <summary>
        /// 初始化转换器，订阅频谱显示状态变化
        /// </summary>
        public static void Initialize(Core.Interface.IMessagingService messagingService)
        {
            if (_disposed || messagingService == null) return;
            
            _messagingService = messagingService;
            
            // 注册频谱显示变化消息处理器
            _messagingService?.Register<SpectrumDisplayChangedMessage>(typeof(SpectrumHeightConverter), (r, m) => {
                _isSpectrumEnabled = m.IsEnabled;
            });
            
            // 注册配置变化消息处理器
            _messagingService?.Register<ConfigurationChangedMessage>(typeof(SpectrumHeightConverter), (r, m) => {
                _isSpectrumEnabled = m.Value.IsSpectrumEnabled;
            });
            
            // 发送配置查询消息，获取当前频谱设置
            _messagingService?.Send(new ConfigurationQueryMessage(config => {
                _isSpectrumEnabled = config.IsSpectrumEnabled;
                return true;
            }));
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Cleanup()
        {
            if (!_disposed)
            {
                _messagingService?.Unregister(typeof(SpectrumHeightConverter));
                _messagingService = null;
                _disposed = true;
            }
        }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double spectrumValue)
            {
                var maxHeight = parameter != null ? double.Parse(parameter.ToString()!) : 100.0;
                
                // 如果频谱被禁用，返回固定的最小高度
                if (!_isSpectrumEnabled)
                {
                    return Math.Max(2, 0.02 * maxHeight); // 使用固定的最小值
                }
                
                return Math.Max(2, spectrumValue * maxHeight);
            }
            
            // 如果频谱被禁用，返回固定的最小高度
            if (!_isSpectrumEnabled)
            {
                var maxHeight = parameter != null ? double.Parse(parameter.ToString()!) : 100.0;
                return Math.Max(2, 0.02 * maxHeight);
            }
            
            return 2.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        public void Dispose()
        {
            // 不需要实例级别的清理，使用静态Cleanup方法
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using MusicPlayer.Services;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 频谱设置视图模型
    /// </summary>
    public class WindowSettingsViewModel : ObservableObject, IWindowSettingsViewModel, IDisposable
    {
        private bool _isSpectrumEnabled = true;
        private bool _isTrayEnabled = true;
        private Theme _currentTheme = Theme.Mica;
        private readonly IConfigurationService _configurationService;
        private bool _disposed = false;

        public string IsSpectrumEnabledText => IsSpectrumEnabled ? "开启" : "禁用";

        /// <summary>
        /// 是否启用频谱显示
        /// </summary>

        public bool IsSpectrumEnabled
        {
            get => _isSpectrumEnabled;
            set
            {
                if (_isSpectrumEnabled != value)
                {
                    _isSpectrumEnabled = value;
                    OnPropertyChanged(nameof(IsSpectrumEnabled));
                    OnPropertyChanged(nameof(IsSpectrumEnabledText)); 
                    // 保存设置到配置
                    SaveSpectrumSetting(value);
                    
                    // 发送消息通知其他控件更新显示状态
                    SendSpectrumSettingMessage(value);
                }
            }
        }

        public string IsTrayEnabledText => IsTrayEnabled ? "开启" : "禁用";

        /// <summary>
        /// 是否启用托盘功能
        /// </summary>
        public bool IsTrayEnabled
        {
            get => _isTrayEnabled;
            set
            {
                if (_isTrayEnabled != value)
                {
                    _isTrayEnabled = value;
                    OnPropertyChanged(nameof(IsTrayEnabled));
                    OnPropertyChanged(nameof(IsTrayEnabledText)); 
                    // 保存设置到配置
                    SaveTraySetting(value);
                }
            }
        }

        /// <summary>
        /// 当前主题
        /// </summary>
        public Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnPropertyChanged();
                    
                    // 保存设置到配置
                    SaveThemeSetting(value);
                    
                    // 发送消息通知窗口更新主题
                    SendThemeChangedMessage(value);
                }
            }
        }

        /// <summary>
        /// 可用的主题选项
        /// </summary>
        public Theme[] ThemeOptions => Enum.GetValues(typeof(Theme)).Cast<Theme>().ToArray();

        private readonly IMessagingService _messagingService;

        public WindowSettingsViewModel(IConfigurationService configurationService, IMessagingService messagingService)
        {
            _configurationService = configurationService;
            _messagingService = messagingService;
            
            // 从配置中加载设置
            LoadSpectrumSetting();
            LoadTraySetting();
            LoadThemeSetting();
            
            // 通知属性更改，确保UI正确初始化
            OnPropertyChanged(nameof(IsSpectrumEnabled));
            OnPropertyChanged(nameof(IsTrayEnabled));
            OnPropertyChanged(nameof(CurrentTheme));
            OnPropertyChanged(nameof(ThemeOptions));
        }

        /// <summary>
        /// 加载频谱设置
        /// </summary>
        private void LoadSpectrumSetting()
        {
            try
            {
                _isSpectrumEnabled = _configurationService.CurrentConfiguration.IsSpectrumEnabled;
            }
            catch (Exception)
            {
                // 使用默认值
                _isSpectrumEnabled = true;
            }
        }

        /// <summary>
        /// 加载托盘设置
        /// </summary>
        private void LoadTraySetting()
        {
            try
            {
                // CloseBehavior=true表示关闭时最小化到托盘，相当于启用托盘功能
                _isTrayEnabled = _configurationService.CurrentConfiguration.CloseBehavior;
            }
            catch (Exception)
            {
                // 使用默认值
                _isTrayEnabled = true;
            }
        }

        /// <summary>
        /// 保存频谱设置
        /// </summary>
        private void SaveSpectrumSetting(bool isEnabled)
        {
            try
            {
                _configurationService.UpdateSpectrumEnabled(isEnabled);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 保存托盘设置
        /// </summary>
        private void SaveTraySetting(bool isEnabled)
        {
            try
            {
                _configurationService.UpdateCloseBehavior(isEnabled);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 加载主题设置
        /// </summary>
        private void LoadThemeSetting()
        {
            try
            {
                _currentTheme = _configurationService.CurrentConfiguration.Theme;
            }
            catch (Exception)
            {
                // 使用默认值
                _currentTheme = Theme.Mica;
            }
        }

        /// <summary>
        /// 保存主题设置
        /// </summary>
        private void SaveThemeSetting(Theme theme)
        {
            try
            {
                _configurationService.UpdateTheme(theme);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 发送频谱设置消息
        /// </summary>
        private void SendSpectrumSettingMessage(bool isEnabled)
        {
            try
            {
                _messagingService.Send(new SpectrumDisplayChangedMessage(isEnabled));
                
                // 同时发送ConfigurationChangedMessage，确保配置变化被广播
                _messagingService.Send(new ConfigurationChangedMessage(_configurationService.CurrentConfiguration));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 发送主题变化消息
        /// </summary>
        private void SendThemeChangedMessage(Theme theme)
        {
            try
            {
                _messagingService.Send(new ThemeChangedMessage(theme));
                
                // 同时发送ConfigurationChangedMessage，确保配置变化被广播
                _messagingService.Send(new ConfigurationChangedMessage(_configurationService.CurrentConfiguration));
            }
            catch (Exception)
            {
            }
        }

        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 清理资源
                _disposed = true;
            }
        }
    }
}
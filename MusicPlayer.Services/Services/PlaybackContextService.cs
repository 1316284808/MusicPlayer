using System;
using System.Collections.Generic;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services.Services
{
    /// <summary>
    /// 播放上下文服务实现
    /// 负责管理和切换当前的播放上下文
    /// </summary>
    public class PlaybackContextService : IPlaybackContextService
    {
        private readonly Dictionary<PlaybackContextType, IPlaybackContextProvider> _providers;
        private PlaybackContext _currentPlaybackContext;

        /// <summary>
        /// 当前播放上下文
        /// </summary>
        public PlaybackContext CurrentPlaybackContext 
        { 
            get => _currentPlaybackContext;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                
                _currentPlaybackContext = value;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PlaybackContextService()
        {
            _providers = new Dictionary<PlaybackContextType, IPlaybackContextProvider>();
            _currentPlaybackContext = PlaybackContext.CreateDefault();
        }

        /// <summary>
        /// 设置播放上下文
        /// </summary>
        /// <param name="type">播放上下文类型</param>
        /// <param name="identifier">标识符</param>
        /// <param name="displayName">显示名称</param>
        public void SetPlaybackContext(PlaybackContextType type, string identifier, string displayName)
        {
            CurrentPlaybackContext = new PlaybackContext 
            { 
                Type = type, 
                Identifier = identifier ?? string.Empty, 
                DisplayName = displayName ?? string.Empty 
            };
        }

        /// <summary>
        /// 根据类型获取播放上下文提供者
        /// </summary>
        /// <param name="type">播放上下文类型</param>
        /// <returns>播放上下文提供者</returns>
        public IPlaybackContextProvider GetProvider(PlaybackContextType type)
        {
            return _providers.TryGetValue(type, out var provider) 
                ? provider 
                : throw new NotSupportedException($"Provider for {type} not found");
        }

        /// <summary>
        /// 注册播放上下文提供者
        /// </summary>
        /// <param name="type">播放上下文类型</param>
        /// <param name="provider">播放上下文提供者</param>
        public void RegisterProvider(PlaybackContextType type, IPlaybackContextProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _providers[type] = provider;
        }
    }
}
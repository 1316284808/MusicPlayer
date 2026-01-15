using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;
using System.Windows;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 配置服务接口 - 专注于配置持久化和状态同步
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 当前配置
        /// </summary>
        PlayerConfiguration CurrentConfiguration { get; }

        /// <summary>
        /// 加载配置
        /// </summary>
        PlayerConfiguration LoadConfiguration();

        /// <summary>
        /// 保存配置
        /// </summary>
        void SaveConfiguration(PlayerConfiguration configuration);

        /// <summary>
        /// 保存当前配置 - 现在会先同步PlayerState状态到配置，然后保存
        /// </summary>
        void SaveCurrentConfiguration();
        
        /// <summary>
        /// 同步PlayerState状态到配置（内存同步，不持久化）
        /// </summary>
        void SyncStateToConfiguration(IPlayerStateService playerState);
        
        /// <summary>
        /// 从配置恢复到PlayerState（应用启动时调用）
        /// </summary>
        void RestorePlayerState(IPlayerStateService playerState);
        
        /// <summary>
        /// 自动持久化配置
        /// </summary>
        bool AutoSaveEnabled { get; set; }
        
        /// <summary>
        /// 自动保存间隔（毫秒）
        /// </summary>
        int AutoSaveIntervalMs { get; set; }
        
        // 配置更新方法 - 保持现有接口兼容性
        void UpdateVolume(float volume);
        void UpdatePlayMode(PlayMode playMode);
        void UpdatePosition(double position, string? currentSongPath);
        void UpdateSortRule(SortRule sortRule);
        void UpdatePlaylistCollapsed(bool isCollapsed);
        void UpdateSpectrumEnabled(bool isEnabled);
        void UpdateCloseBehavior(bool closeBehavior);
        void UpdateTheme(Theme theme);
        void UpdateLanguage(string language);
        void UpdateAudioEngine(AudioEngine audioEngine);
        
        // 均衡器相关方法
        void UpdateEqualizerEnabled(bool isEnabled);
        void UpdateEqualizerPresetName(string presetName);
        void UpdateEqualizerGains(float[] gains);
        
        /// <summary>
        /// 更新最后播放的歌曲ID
        /// </summary>
        void UpdateLastPlayedSongId(int songId);

        /// <summary>
        /// 更新歌词字体大小配置
        /// </summary>
        void UpdateLyricFontSize(double fontSize);

        /// <summary>
        /// 更新歌词文本对齐方式配置
        /// </summary>
        void UpdateLyricTextAlignment(System.Windows.HorizontalAlignment textAlignment);

        /// <summary>
        /// 更新歌词翻译启用状态配置
        /// </summary>
        void UpdateLyricTranslationEnabled(bool isEnabled);

        /// <summary>
        /// 保存所有配置
        /// </summary>
        void SaveAll();

        // 事件
        /// <summary>
        /// 配置加载完成事件
        /// </summary>
        event Action<PlayerConfiguration>? ConfigurationLoaded;
        
        /// <summary>
        /// 配置保存完成事件
        /// </summary>
        event Action<PlayerConfiguration>? ConfigurationSaved;
        
        /// <summary>
        /// 配置变更事件
        /// </summary>
        event Action<PlayerConfiguration> ConfigurationChanged;
    }
}
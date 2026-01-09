using MusicPlayer.Core.Enums;
using System;
using System.ComponentModel;
using System.Windows;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放器配置数据模型 - 专注于持久化配置
    /// 作为纯粹的数据传输对象(DTO)，不包含运行时状态管理逻辑
    /// 所有状态同步由ConfigurationService负责
    /// </summary>
    public class PlayerConfiguration : INotifyPropertyChanged
    {
        private float _volume = 0.5f;
        private PlayMode _playMode = PlayMode.RepeatAll;
        private double _currentPosition = 0;
        private string? _currentSongPath;
        private SortRule _sortRule = SortRule.ByAddedTime;
        private DateTime _lastSaved = DateTime.Now;
        private bool _isPlaylistCollapsed = false;
        private bool _isSpectrumEnabled = true;
        private bool _closeBehavior = false;
        private Theme _theme = Theme.None;
        private string _language = "zh-CN";
        private string _filterText = "";
        private AudioEngine _audioEngine = AudioEngine.Auto;
        private bool _isEqualizerEnabled = false;
        private string _equalizerPresetName = "平衡";
        private float[] _equalizerGains = new float[10] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
        private int _lastPlayedSongId = -1; // 最后播放的歌曲ID，-1表示无
        private double _lyricFontSize = 20.0; // 歌词字体大小
        private TextAlignment _lyricTextAlignment = TextAlignment.Center; // 歌词文本对齐方式
        private bool _isCoverCacheEnabled = true; // 是否启用封面缓存
        private string _lyricDirectory = string.Empty; // 歌词文件目录

        /// <summary>
        /// 音量大小 (0.0 - 1.0)
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                if (Math.Abs(_volume - value) > 0.001f)
                {
                    _volume = Math.Clamp(value, 0f, 1f);
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        /// <summary>
        /// 播放模式
        /// </summary>
        public PlayMode PlayMode
        {
            get => _playMode;
            set
            {
                if (_playMode != value)
                {
                    _playMode = value;
                    OnPropertyChanged(nameof(PlayMode));
                }
            }
        }

        /// <summary>
        /// 当前播放进度（秒）
        /// </summary>
        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (Math.Abs(_currentPosition - value) > 0.1)
                {
                    _currentPosition = Math.Max(0, value);
                    OnPropertyChanged(nameof(CurrentPosition));
                }
            }
        }

        /// <summary>
        /// 当前播放歌曲路径
        /// </summary>
        public string? CurrentSongPath
        {
            get => _currentSongPath;
            set
            {
                if (_currentSongPath != value)
                {
                    _currentSongPath = value;
                    OnPropertyChanged(nameof(CurrentSongPath));
                }
            }
        }

        /// <summary>
        /// 播放列表排序规则
        /// </summary>
        public SortRule SortRule
        {
            get => _sortRule;
            set
            {
                if (_sortRule != value)
                {
                    _sortRule = value;
                    OnPropertyChanged(nameof(SortRule));
                }
            }
        }

        /// <summary>
        /// 最后保存时间
        /// </summary>
        public DateTime LastSaved
        {
            get => _lastSaved;
            set
            {
                if (_lastSaved != value)
                {
                    _lastSaved = value;
                    OnPropertyChanged(nameof(LastSaved));
                }
            }
        }

        /// <summary>
        /// 播放列表是否折叠
        /// </summary>
        public bool IsPlaylistCollapsed
        {
            get => _isPlaylistCollapsed;
            set
            {
                if (_isPlaylistCollapsed != value)
                {
                    _isPlaylistCollapsed = value;
                    OnPropertyChanged(nameof(IsPlaylistCollapsed));
                }
            }
        }

        /// <summary>
        /// 频谱显示是否启用
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
                }
            }
        }

        /// <summary>
        /// 关闭主窗口行为：false=完全退出应用，true=最小化到托盘
        /// </summary>
        public bool CloseBehavior
        {
            get => _closeBehavior;
            set
            {
                if (_closeBehavior != value)
                {
                    _closeBehavior = value;
                    OnPropertyChanged(nameof(CloseBehavior));
                }
            }
        }

        /// <summary>
        /// 主题设置
        /// </summary>
        public Theme Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged(nameof(Theme));
                }
            }
        }

        /// <summary>
        /// 语言设置
        /// </summary>
        public string Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged(nameof(Language));
                }
            }
        }

        /// <summary>
        /// 过滤文本
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText != value)
                {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                }
            }
        }

        /// <summary>
        /// 音频引擎设置
        /// </summary>
        public AudioEngine AudioEngine
        {
            get => _audioEngine;
            set
            {
                if (_audioEngine != value)
                {
                    _audioEngine = value;
                    OnPropertyChanged(nameof(AudioEngine));
                }
            }
        }

        /// <summary>
        /// 是否启用均衡器
        /// </summary>
        public bool IsEqualizerEnabled
        {
            get => _isEqualizerEnabled;
            set
            {
                if (_isEqualizerEnabled != value)
                {
                    _isEqualizerEnabled = value;
                    OnPropertyChanged(nameof(IsEqualizerEnabled));
                }
            }
        }

        /// <summary>
        /// 均衡器预设名称
        /// </summary>
        public string EqualizerPresetName
        {
            get => _equalizerPresetName;
            set
            {
                if (_equalizerPresetName != value)
                {
                    _equalizerPresetName = value ?? "平衡";
                    OnPropertyChanged(nameof(EqualizerPresetName));
                }
            }
        }

        /// <summary>
        /// 均衡器增益值数组（10个频段）
        /// </summary>
        public float[] EqualizerGains
        {
            get => _equalizerGains;
            set
            {
                if (value != null && value.Length == 10)
                {
                    bool changed = false;
                    for (int i = 0; i < 10; i++)
                    {
                        if (Math.Abs(_equalizerGains[i] - value[i]) > 0.1f)
                        {
                            _equalizerGains[i] = value[i];
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        OnPropertyChanged(nameof(EqualizerGains));
                    }
                }
            }
        }

        /// <summary>
        /// 最后播放的歌曲ID，-1表示无
        /// </summary>
        public int LastPlayedSongId
        {
            get => _lastPlayedSongId;
            set
            {
                if (_lastPlayedSongId != value)
                {
                    _lastPlayedSongId = value;
                    OnPropertyChanged(nameof(LastPlayedSongId));
                }
            }
        }

        /// <summary>
        /// 歌词字体大小
        /// </summary>
        public double LyricFontSize
        {
            get => _lyricFontSize;
            set
            {
                if (Math.Abs(_lyricFontSize - value) > 0.1)
                {
                    _lyricFontSize = Math.Max(10, Math.Min(50, value)); // 限制字体大小在10-50之间
                    OnPropertyChanged(nameof(LyricFontSize));
                }
            }
        }

        /// <summary>
        /// 歌词文本对齐方式
        /// </summary>
        public TextAlignment LyricTextAlignment
        {
            get => _lyricTextAlignment;
            set
            {
                if (_lyricTextAlignment != value)
                {
                    _lyricTextAlignment = value;
                    OnPropertyChanged(nameof(LyricTextAlignment));
                }
            }
        }

        /// <summary>
        /// 是否启用封面缓存
        /// </summary>
        public bool IsCoverCacheEnabled
        {
            get => _isCoverCacheEnabled;
            set
            {
                if (_isCoverCacheEnabled != value)
                {
                    _isCoverCacheEnabled = value;
                    OnPropertyChanged(nameof(IsCoverCacheEnabled));
                }
            }
        }

        /// <summary>
        /// 歌词文件目录
        /// </summary>
        public string LyricDirectory
        {
            get => _lyricDirectory;
            set
            {
                if (_lyricDirectory != value)
                {
                    _lyricDirectory = value;
                    OnPropertyChanged(nameof(LyricDirectory));
                }
            }
        }

        /// <summary>
        /// 获取或设置指定频段的增益值
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <returns>增益值 (dB)</returns>
        public float GetEqualizerBandGain(int bandIndex)
        {
            if (bandIndex >= 0 && bandIndex < _equalizerGains.Length)
                return _equalizerGains[bandIndex];
            return 0f;
        }

        /// <summary>
        /// 设置指定频段的增益值
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <param name="gain">增益值 (dB)</param>
        public void SetEqualizerBandGain(int bandIndex, float gain)
        {
            if (bandIndex >= 0 && bandIndex < _equalizerGains.Length)
            {
                // 限制增益值在-12到+12dB之间
                gain = Math.Clamp(gain, -12f, 12f);

                if (Math.Abs(_equalizerGains[bandIndex] - gain) > 0.1f)
                {
                    _equalizerGains[bandIndex] = gain;
                    OnPropertyChanged(nameof(EqualizerGains));
                }
            }
        }

        /// <summary>
        /// 属性变更事件 - 仅用于通知配置变化，不用于UI绑定
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 检查配置是否有效
        /// </summary>
        public bool IsValid()
        {
            return Volume >= 0 && Volume <= 1.0 &&
                   CurrentPosition >= 0 &&
                   Enum.IsDefined(typeof(PlayMode), PlayMode) &&
                   Enum.IsDefined(typeof(SortRule), SortRule) &&
                   Enum.IsDefined(typeof(AudioEngine), AudioEngine) &&
                   !string.IsNullOrEmpty(EqualizerPresetName) &&
                   EqualizerGains != null && EqualizerGains.Length == 10;
        }

        /// <summary>
        /// 应用默认值（当配置无效时）
        /// </summary>
        public void ApplyDefaults()
        {
            Volume = 0.5f;
            PlayMode = PlayMode.RepeatAll;
            CurrentPosition = 0;
            CurrentSongPath = null;
            SortRule = SortRule.ByAddedTime;
            IsPlaylistCollapsed = false;
            IsSpectrumEnabled = true;
            CloseBehavior = false;  // 默认关闭时完全退出应用
            LastSaved = DateTime.Now;
            Theme = Theme.Mica;
            Language = "zh-CN";
            FilterText = "";
            AudioEngine = AudioEngine.Auto; // 默认自动选择音频引擎
            IsEqualizerEnabled = false; // 默认不启用均衡器
            EqualizerPresetName = "平衡"; // 默认平直响应
            EqualizerGains = new float[10] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f }; // 默认平直响应
            LastPlayedSongId = -1; // 默认无最后播放歌曲
            LyricFontSize = 20.0; // 默认歌词字体大小
            LyricTextAlignment = TextAlignment.Center; // 默认歌词居中对齐
            IsCoverCacheEnabled = true; // 默认启用封面缓存
            LyricDirectory = string.Empty; // 默认歌词目录为空，使用同目录下的歌词文件
        }

      
        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
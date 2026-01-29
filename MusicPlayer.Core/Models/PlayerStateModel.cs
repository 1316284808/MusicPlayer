using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放器状态模型 - 作为状态的唯一可信源
    /// 集中管理所有播放器状态数据，供多个服务共享
    /// </summary>
    public class PlayerStateModel : INotifyPropertyChanged
    {
        private float _volume = 0.5f;
        private bool _isPlaying;
        private bool _isMuted;
        private double _currentPosition;
        private double _maxPosition;
        private PlayMode _playMode = PlayMode.RepeatAll;
        private AudioEngine _audioEngine = AudioEngine.Auto;
        private Song? _currentSong;
        private float[] _spectrumData = new float[32];

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 音量 (0.0 - 1.0)
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                if (Math.Abs(_volume - value) > 0.001f)
                {
                    _volume = Math.Clamp(value, 0.0f, 1.0f);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否静音
        /// </summary>
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前播放位置（秒）
        /// </summary>
        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (Math.Abs(_currentPosition - value) > 0.001)
                {
                    _currentPosition = Math.Clamp(value, 0.0, _maxPosition);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 最大播放位置（秒）
        /// </summary>
        public double MaxPosition
        {
            get => _maxPosition;
            set
            {
                if (Math.Abs(_maxPosition - value) > 0.1)
                {
                    _maxPosition = Math.Max(0, value);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前播放模式
        /// </summary>
        public PlayMode PlayMode
        {
            get => _playMode;
            set
            {
                if (_playMode != value)
                {
                    _playMode = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前音频引擎
        /// </summary>
        public AudioEngine AudioEngine
        {
            get => _audioEngine;
            set
            {
                if (_audioEngine != value)
                {
                    _audioEngine = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前歌曲
        /// </summary>
        public Song? CurrentSong
        {
            get => _currentSong;
            set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 频谱数据
        /// </summary>
        public float[] SpectrumData
        {
            get => _spectrumData;
            set
            {
                if (value != null && !Equals(_spectrumData, value))
                {
                    if (value.Length != _spectrumData.Length)
                    {
                        _spectrumData = new float[value.Length];
                    }
                    Array.Copy(value, _spectrumData, Math.Min(value.Length, _spectrumData.Length));
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 从配置恢复状态
        /// </summary>
        public void RestoreFromConfiguration(PlayerConfiguration configuration)
        {
            if (configuration == null)
                return;

            Volume = configuration.Volume;
            PlayMode = configuration.PlayMode;
            AudioEngine = configuration.AudioEngine;
            CurrentPosition = configuration.CurrentPosition;
            IsMuted = false; // 不从配置恢复静音状态
        }

        /// <summary>
        /// 同步状态到配置
        /// </summary>
        public void SyncToConfiguration(PlayerConfiguration configuration)
        {
            if (configuration == null)
                return;

            configuration.Volume = Volume;
            configuration.PlayMode = PlayMode;
            configuration.AudioEngine = AudioEngine;
            configuration.CurrentPosition = CurrentPosition;
            configuration.CurrentSongPath = CurrentSong?.FilePath;
            configuration.LastSaved = DateTime.Now;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.ComponentModel;
using System.Linq;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 均衡器设置模型
    /// </summary>
    public class EqualizerSettings : INotifyPropertyChanged
    {
        private bool _isEnabled = false;
        private string _presetName = "平衡";
        private float[] _bandGains = new float[10]; // 10个频段的增益值，范围-12到+12dB
        private bool _isCustom = false;

        // 频段频率 (Hz)
        public static readonly double[] FrequencyBands = { 32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };

        /// <summary>
        /// 是否启用均衡器
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        /// <summary>
        /// 当前均衡器预设名称
        /// </summary>
        public string PresetName
        {
            get => _presetName;
            set
            {
                if (_presetName != value)
                {
                    _presetName = value ?? "平衡";
                    OnPropertyChanged(nameof(PresetName));
                    OnPropertyChanged(nameof(IsCustom));
                }
            }
        }

        /// <summary>
        /// 是否为自定义预设
        /// </summary>
        public bool IsCustom => _presetName == "自定义" || _isCustom;

        /// <summary>
        /// 获取或设置指定频段的增益值
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <returns>增益值 (dB)</returns>
        public float this[int bandIndex]
        {
            get
            {
                if (bandIndex >= 0 && bandIndex < _bandGains.Length)
                    return _bandGains[bandIndex];
                return 0f;
            }
            set
            {
                if (bandIndex >= 0 && bandIndex < _bandGains.Length)
                {
                    // 限制增益值在-12到+12dB之间
                    value = Math.Clamp(value, -12f, 12f);

                    // 降低阈值，使更小的变化也能被接受
                    if (Math.Abs(_bandGains[bandIndex] - value) > 0.01f)
                    {
                        _bandGains[bandIndex] = value;
                        OnPropertyChanged($"BandGain{bandIndex}");

                        // 如果不是自定义预设，则切换到自定义
                        if (_presetName != "自定义")
                        {
                            _isCustom = true;
                            OnPropertyChanged(nameof(IsCustom));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有频段的增益值
        /// </summary>
        public float[] BandGains => (float[])_bandGains.Clone();

        /// <summary>
        /// 初始化均衡器设置
        /// </summary>
        public EqualizerSettings()
        {
            // 初始化所有频段增益为0（平直响应）
            for (int i = 0; i < _bandGains.Length; i++)
            {
                _bandGains[i] = 0f;
            }

            _presetName = "平衡";
            _isCustom = false;
        }

        /// <summary>
        /// 应用预设的增益值
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        public void ApplyPreset(EqualizerPreset preset)
        {
            if (preset == null)
                return;

            _presetName = preset.PresetName;
            _isCustom = _presetName == "自定义";

            if (preset.BandGains != null && preset.BandGains.Length == _bandGains.Length)
            {
                for (int i = 0; i < _bandGains.Length; i++)
                {
                    // 降低阈值，确保预设增益值能正确应用
                    if (Math.Abs(_bandGains[i] - preset.BandGains[i]) > 0.01f)
                    {
                        _bandGains[i] = preset.BandGains[i];
                        OnPropertyChanged($"BandGain{i}");
                    }
                }
            }

            OnPropertyChanged(nameof(PresetName));
            OnPropertyChanged(nameof(IsCustom));
        }

        /// <summary>
        /// 重置为平直响应
        /// </summary>
        public void Reset()
        {
            _isCustom = false;
            _presetName = "平衡";

            for (int i = 0; i < _bandGains.Length; i++)
            {
                _bandGains[i] = 0f;
                OnPropertyChanged($"BandGain{i}");
            }

            OnPropertyChanged(nameof(PresetName));
            OnPropertyChanged(nameof(IsCustom));
        }

        /// <summary>
        /// 克隆均衡器设置
        /// </summary>
        /// <returns>克隆的均衡器设置</returns>
        public EqualizerSettings Clone()
        {
            EqualizerSettings clone = new EqualizerSettings
            {
                _isEnabled = this._isEnabled,
                _presetName = this._presetName,
                _isCustom = this._isCustom
            };

            Array.Copy(this._bandGains, clone._bandGains, this._bandGains.Length);

            return clone;
        }

        /// <summary>
        /// 从另一个均衡器设置复制所有属性值
        /// </summary>
        /// <param name="other">另一个均衡器设置</param>
        public void CopyFrom(EqualizerSettings other)
        {
            if (other == null) return;

            bool changed = false;

            if (_isEnabled != other.IsEnabled)
            {
                _isEnabled = other.IsEnabled;
                OnPropertyChanged(nameof(IsEnabled));
                changed = true;
            }

            if (_presetName != other.PresetName)
            {
                _presetName = other.PresetName;
                OnPropertyChanged(nameof(PresetName));
                changed = true;
            }

            if (_isCustom != other._isCustom)
            {
                _isCustom = other._isCustom;
                OnPropertyChanged(nameof(IsCustom));
                changed = true;
            }

            for (int i = 0; i < _bandGains.Length; i++)
            {
                if (Math.Abs(_bandGains[i] - other._bandGains[i]) > 0.1f)
                {
                    _bandGains[i] = other._bandGains[i];
                    OnPropertyChanged($"BandGain{i}");
                    changed = true;
                }
            }
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
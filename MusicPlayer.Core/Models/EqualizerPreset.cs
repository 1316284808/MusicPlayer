using System;
using System.ComponentModel;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 均衡器预设实体类
    /// </summary>
    public class EqualizerPreset : INotifyPropertyChanged
    {
        private int _id;
        private string _presetName = string.Empty;
        private float _bandGain0;
        private float _bandGain1;
        private float _bandGain2;
        private float _bandGain3;
        private float _bandGain4;
        private float _bandGain5;
        private float _bandGain6;
        private float _bandGain7;
        private float _bandGain8;
        private float _bandGain9;

        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        /// <summary>
        /// 预设名称
        /// </summary>
        public string PresetName
        {
            get => _presetName;
            set
            {
                if (_presetName != value)
                {
                    _presetName = value;
                    OnPropertyChanged(nameof(PresetName));
                }
            }
        }

        /// <summary>
        /// 频段0增益值 (32Hz)
        /// </summary>
        public float BandGain0
        {
            get => _bandGain0;
            set
            {
                if (Math.Abs(_bandGain0 - value) > 0.01f)
                {
                    _bandGain0 = value;
                    OnPropertyChanged(nameof(BandGain0));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段1增益值 (64Hz)
        /// </summary>
        public float BandGain1
        {
            get => _bandGain1;
            set
            {
                if (Math.Abs(_bandGain1 - value) > 0.01f)
                {
                    _bandGain1 = value;
                    OnPropertyChanged(nameof(BandGain1));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段2增益值 (125Hz)
        /// </summary>
        public float BandGain2
        {
            get => _bandGain2;
            set
            {
                if (Math.Abs(_bandGain2 - value) > 0.01f)
                {
                    _bandGain2 = value;
                    OnPropertyChanged(nameof(BandGain2));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段3增益值 (250Hz)
        /// </summary>
        public float BandGain3
        {
            get => _bandGain3;
            set
            {
                if (Math.Abs(_bandGain3 - value) > 0.01f)
                {
                    _bandGain3 = value;
                    OnPropertyChanged(nameof(BandGain3));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段4增益值 (500Hz)
        /// </summary>
        public float BandGain4
        {
            get => _bandGain4;
            set
            {
                if (Math.Abs(_bandGain4 - value) > 0.01f)
                {
                    _bandGain4 = value;
                    OnPropertyChanged(nameof(BandGain4));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段5增益值 (1000Hz)
        /// </summary>
        public float BandGain5
        {
            get => _bandGain5;
            set
            {
                if (Math.Abs(_bandGain5 - value) > 0.01f)
                {
                    _bandGain5 = value;
                    OnPropertyChanged(nameof(BandGain5));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段6增益值 (2000Hz)
        /// </summary>
        public float BandGain6
        {
            get => _bandGain6;
            set
            {
                if (Math.Abs(_bandGain6 - value) > 0.01f)
                {
                    _bandGain6 = value;
                    OnPropertyChanged(nameof(BandGain6));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段7增益值 (4000Hz)
        /// </summary>
        public float BandGain7
        {
            get => _bandGain7;
            set
            {
                if (Math.Abs(_bandGain7 - value) > 0.01f)
                {
                    _bandGain7 = value;
                    OnPropertyChanged(nameof(BandGain7));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段8增益值 (8000Hz)
        /// </summary>
        public float BandGain8
        {
            get => _bandGain8;
            set
            {
                if (Math.Abs(_bandGain8 - value) > 0.01f)
                {
                    _bandGain8 = value;
                    OnPropertyChanged(nameof(BandGain8));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 频段9增益值 (16000Hz)
        /// </summary>
        public float BandGain9
        {
            get => _bandGain9;
            set
            {
                if (Math.Abs(_bandGain9 - value) > 0.01f)
                {
                    _bandGain9 = value;
                    OnPropertyChanged(nameof(BandGain9));
                    OnPropertyChanged(nameof(BandGains));
                }
            }
        }

        /// <summary>
        /// 获取所有频段的增益值数组
        /// </summary>
        public float[] BandGains => new float[]
        {
            BandGain0, BandGain1, BandGain2, BandGain3, BandGain4,
            BandGain5, BandGain6, BandGain7, BandGain8, BandGain9
        };

        /// <summary>
        /// 设置所有频段的增益值
        /// </summary>
        /// <param name="gains">增益值数组</param>
        public void SetBandGains(float[] gains)
        {
            if (gains == null || gains.Length < 10)
                return;

            BandGain0 = gains[0];
            BandGain1 = gains[1];
            BandGain2 = gains[2];
            BandGain3 = gains[3];
            BandGain4 = gains[4];
            BandGain5 = gains[5];
            BandGain6 = gains[6];
            BandGain7 = gains[7];
            BandGain8 = gains[8];
            BandGain9 = gains[9];
        }

        /// <summary>
        /// 获取指定频段的增益值
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <returns>增益值 (dB)</returns>
        public float GetBandGain(int bandIndex)
        {
            return bandIndex switch
            {
                0 => BandGain0,
                1 => BandGain1,
                2 => BandGain2,
                3 => BandGain3,
                4 => BandGain4,
                5 => BandGain5,
                6 => BandGain6,
                7 => BandGain7,
                8 => BandGain8,
                9 => BandGain9,
                _ => 0f
            };
        }

        /// <summary>
        /// 设置指定频段的增益值
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <param name="gain">增益值 (dB)</param>
        public void SetBandGain(int bandIndex, float gain)
        {
            gain = Math.Clamp(gain, -12f, 12f);

            switch (bandIndex)
            {
                case 0: BandGain0 = gain; break;
                case 1: BandGain1 = gain; break;
                case 2: BandGain2 = gain; break;
                case 3: BandGain3 = gain; break;
                case 4: BandGain4 = gain; break;
                case 5: BandGain5 = gain; break;
                case 6: BandGain6 = gain; break;
                case 7: BandGain7 = gain; break;
                case 8: BandGain8 = gain; break;
                case 9: BandGain9 = gain; break;
            }
        }

        /// <summary>
        /// 频段频率 (Hz)
        /// </summary>
        public static readonly double[] FrequencyBands = { 32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };

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

        /// <summary>
        /// 创建副本
        /// </summary>
        public EqualizerPreset Clone()
        {
            return new EqualizerPreset
            {
                Id = this.Id,
                PresetName = this.PresetName,
                BandGain0 = this.BandGain0,
                BandGain1 = this.BandGain1,
                BandGain2 = this.BandGain2,
                BandGain3 = this.BandGain3,
                BandGain4 = this.BandGain4,
                BandGain5 = this.BandGain5,
                BandGain6 = this.BandGain6,
                BandGain7 = this.BandGain7,
                BandGain8 = this.BandGain8,
                BandGain9 = this.BandGain9
            };
        }
    }
}
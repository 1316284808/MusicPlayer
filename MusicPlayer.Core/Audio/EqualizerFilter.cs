using System;

namespace MusicPlayer.Core.Audio
{
    /// <summary>
    /// 均衡器滤波器 - 使用双二阶滤波器实现
    /// </summary>
    public class EqualizerFilter
    {
        private float _sampleRate;
        private float _centerFrequency;
        private float _qFactor;
        private float _gain;
        private float _a0, _a1, _a2, _b1, _b2;
        private float _x1, _x2, _y1, _y2;
        private FilterType _filterType;

        /// <summary>
        /// 滤波器类型
        /// </summary>
        private enum FilterType
        {
            Peak,   // 峰值滤波器（均衡器常用）
            LowPass, // 低通滤波器
            HighPass, // 高通滤波器
            BandPass  // 带通滤波器
        }

        /// <summary>
        /// 初始化均衡器滤波器
        /// </summary>
        /// <param name="sampleRate">采样率</param>
        /// <param name="centerFrequency">中心频率 (Hz)</param>
        /// <param name="qFactor">Q因子</param>
        /// <param name="gain">增益 (线性值，非dB)</param>
        public EqualizerFilter(float sampleRate, float centerFrequency, float qFactor, float gain)
        {
            _sampleRate = sampleRate;
            _centerFrequency = centerFrequency;
            _qFactor = qFactor;
            _gain = gain;
            _filterType = FilterType.Peak;
            
            // 初始化滤波器状态
            _x1 = _x2 = _y1 = _y2 = 0;
            
            CalculateCoefficients();
        }

        /// <summary>
        /// 设置增益
        /// </summary>
        /// <param name="gain">增益 (线性值，非dB)</param>
        public void SetGain(float gain)
        {
            if (Math.Abs(_gain - gain) > 0.001f)
            {
                _gain = gain;
                CalculateCoefficients();
            }
        }

        /// <summary>
        /// 获取增益
        /// </summary>
        /// <returns>增益 (线性值，非dB)</returns>
        public float GetGain()
        {
            return _gain;
        }

        /// <summary>
        /// 设置中心频率
        /// </summary>
        /// <param name="frequency">中心频率 (Hz)</param>
        public void SetFrequency(float frequency)
        {
            if (Math.Abs(_centerFrequency - frequency) > 0.1f)
            {
                _centerFrequency = frequency;
                CalculateCoefficients();
            }
        }

        /// <summary>
        /// 重置滤波器状态
        /// </summary>
        public void Reset()
        {
            _x1 = _x2 = _y1 = _y2 = 0;
        }

        /// <summary>
        /// 处理单个样本
        /// </summary>
        /// <param name="input">输入样本</param>
        /// <returns>处理后的样本</returns>
        public float Process(float input)
        {
            // 双二阶差分方程: y[n] = a0*x[n] + a1*x[n-1] + a2*x[n-2] - b1*y[n-1] - b2*y[n-2]
            float output = _a0 * input + _a1 * _x1 + _a2 * _x2 - _b1 * _y1 - _b2 * _y2;
            
            // 更新状态变量
            _x2 = _x1;
            _x1 = input;
            _y2 = _y1;
            _y1 = output;
            
            return output;
        }

        /// <summary>
        /// 计算滤波器系数
        /// </summary>
        private void CalculateCoefficients()
        {
            switch (_filterType)
            {
                case FilterType.Peak:
                    CalculatePeakCoefficients();
                    break;
                case FilterType.LowPass:
                    CalculateLowPassCoefficients();
                    break;
                case FilterType.HighPass:
                    CalculateHighPassCoefficients();
                    break;
                case FilterType.BandPass:
                    CalculateBandPassCoefficients();
                    break;
            }
        }

        /// <summary>
        /// 计算峰值滤波器系数
        /// </summary>
        private void CalculatePeakCoefficients()
        {
            float pi = (float)Math.PI;
            float omega = 2 * pi * _centerFrequency / _sampleRate;
            float sin = (float)Math.Sin(omega);
            float cos = (float)Math.Cos(omega);
            float alpha = sin / (2 * _qFactor);
            
            // 如果增益接近1，直接设置为1
            if (Math.Abs(_gain - 1.0f) < 0.001f)
            {
                _a0 = 1;
                _a1 = _a2 = 0;
                _b1 = _b2 = 0;
                return;
            }
            
            // 将线性增益转换为dB
            float gainDb = 20 * (float)Math.Log10(_gain);
            float a = (float)Math.Pow(10, gainDb / 40); // 注意：这里是/40而不是/20
            
            // 计算系数
            float b0 = 1 + alpha * a;
            _a0 = (1 + alpha / a) / b0;
            _a1 = -2 * cos / b0;
            _a2 = (1 - alpha / a) / b0;
            _b1 = -2 * cos;
            _b2 = (1 - alpha * a) / b0;
        }

        /// <summary>
        /// 计算低通滤波器系数
        /// </summary>
        private void CalculateLowPassCoefficients()
        {
            float pi = (float)Math.PI;
            float omega = 2 * pi * _centerFrequency / _sampleRate;
            float sin = (float)Math.Sin(omega);
            float cos = (float)Math.Cos(omega);
            float alpha = sin / (2 * _qFactor);
            
            float b0 = 1 + alpha;
            _a0 = (1 - cos) / (2 * b0);
            _a1 = (1 - cos) / b0;
            _a2 = (1 - cos) / (2 * b0);
            _b1 = 2 * cos / b0;
            _b2 = (alpha - 1) / b0;
        }

        /// <summary>
        /// 计算高通滤波器系数
        /// </summary>
        private void CalculateHighPassCoefficients()
        {
            float pi = (float)Math.PI;
            float omega = 2 * pi * _centerFrequency / _sampleRate;
            float sin = (float)Math.Sin(omega);
            float cos = (float)Math.Cos(omega);
            float alpha = sin / (2 * _qFactor);
            
            float b0 = 1 + alpha;
            _a0 = (1 + cos) / (2 * b0);
            _a1 = -(1 + cos) / b0;
            _a2 = (1 + cos) / (2 * b0);
            _b1 = 2 * cos / b0;
            _b2 = (alpha - 1) / b0;
        }

        /// <summary>
        /// 计算带通滤波器系数
        /// </summary>
        private void CalculateBandPassCoefficients()
        {
            float pi = (float)Math.PI;
            float omega = 2 * pi * _centerFrequency / _sampleRate;
            float sin = (float)Math.Sin(omega);
            float cos = (float)Math.Cos(omega);
            float alpha = sin / (2 * _qFactor);
            
            float b0 = 1 + alpha;
            _a0 = alpha / b0;
            _a1 = 0;
            _a2 = -alpha / b0;
            _b1 = 2 * cos / b0;
            _b2 = (alpha - 1) / b0;
        }
    }
}
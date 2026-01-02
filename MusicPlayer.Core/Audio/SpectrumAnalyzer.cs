using System;
using NAudio.Wave;
using NAudio.Dsp;

namespace MusicPlayer.Core.Audio
{
    /// <summary>
    /// 频谱分析器 - 负责音频信号的快速傅里叶变换(FFT)分析
    /// 用于将时域音频信号转换为频域频谱数据，用于音频可视化
    /// </summary>
    public class SpectrumAnalyzer : ISampleProvider, IDisposable
    {
        private readonly ISampleProvider _source;          // 音频数据源
        private readonly int _channels;                   // 音频通道数
        private readonly int _sampleRate;                 // 采样率
        private readonly int _fftLength;                  // FFT变换长度
        private readonly Complex[] _fftBuffer;            // FFT缓冲区（复用，避免频繁分配）
        private readonly float[] _lastSpectrum;           // 最新的频谱数据（正频率部分）
        private readonly float[] _window;                 // 汉宁窗函数，用于减少频谱泄漏
        private int _fftPos;                              // FFT缓冲区当前位置
        private readonly object _lock = new object();     // 线程安全锁
        private bool _disposed = false;                   // Dispose 状态标记

        /// <summary>音频格式信息</summary>
        public WaveFormat WaveFormat => _source?.WaveFormat;

        /// <summary>
        /// 获取当前频谱数据副本（仅用于调试或低频调用，高频调用请使用 CopySpectrumTo）
        /// </summary>
        public float[] GetSpectrum()
        {
            lock (_lock)
            {
                return (float[])_lastSpectrum.Clone();
            }
        }

        /// <summary>
        /// 将当前频谱数据复制到指定目标数组（推荐用于UI高频刷新，避免GC）
        /// 支持部分复制，只复制目标数组所需长度的数据
        /// </summary>
        /// <param name="destination">目标数组，长度可以小于等于 _lastSpectrum.Length</param>
        public void  CopySpectrumTo(float[] destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destination.Length <= 0)
                throw new ArgumentException("Destination array must have positive length.", nameof(destination));

            lock (_lock)
            {
                // 只复制目标数组长度的数据，支持部分复制以提高效率
                int copyLength = Math.Min(destination.Length, _lastSpectrum.Length);
                Array.Copy(_lastSpectrum, destination, copyLength);
            }
        }

        /// <summary>
        /// 频谱分析器构造函数
        /// </summary>
        /// <param name="source">音频数据源</param>
        /// <param name="fftLength">FFT变换长度，默认4096点（必须是2的幂）</param>
        public SpectrumAnalyzer(ISampleProvider source, int fftLength = 4096)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if ((fftLength & (fftLength - 1)) != 0)
                throw new ArgumentException("FFT length must be a power of two.", nameof(fftLength));

            _source = source;
            _channels = source.WaveFormat.Channels;
            _sampleRate = source.WaveFormat.SampleRate;
            _fftLength = fftLength;
            _fftBuffer = new Complex[fftLength];
            _lastSpectrum = new float[fftLength / 2];

            // 预计算汉宁窗
            _window = new float[fftLength];
            for (int i = 0; i < fftLength; i++)
            {
                _window[i] = 0.5f * (1 - (float)Math.Cos(2 * Math.PI * i / (fftLength - 1)));
            }
        }

        /// <summary>
        /// 读取音频数据并进行频谱分析
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            if (_disposed) return 0;
            

            int samplesRead = _source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i += _channels)
            {
                // 多通道混音为单声道
                float sample = 0;
                for (int channel = 0; channel < _channels && (i + channel) < samplesRead; channel++)
                {
                    sample += buffer[offset + i + channel];
                }
                sample /= _channels;

                // 应用窗函数并填入FFT缓冲区
                _fftBuffer[_fftPos] = new Complex
                {
                    X = sample * _window[_fftPos],
                    Y = 0
                };
                _fftPos++;

                // 缓冲区满，执行FFT
                if (_fftPos >= _fftLength)
                {
                    _fftPos = 0;
                    ProcessFFT();
                }
            }

            return samplesRead;
        }

        /// <summary>
        /// 执行FFT变换并更新频谱数据
        /// 使用 NAudio 内置的高效 FFT 实现
        /// </summary>
        private void ProcessFFT()
        {
            // 直接在 _fftBuffer 上操作（安全：音频线程独占写入，且 _fftPos 已重置）
            FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2), _fftBuffer);

            lock (_lock)
            {
                // 计算幅度谱（仅正频率部分）
                for (int i = 0; i < _lastSpectrum.Length; i++)
                {
                    var re = _fftBuffer[i].X;
                    var im = _fftBuffer[i].Y;
                    _lastSpectrum[i] = (float)Math.Sqrt(re * re + im * im);
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // 清理数组缓冲区，释放内存
                lock (_lock)
                {
                    Array.Clear(_fftBuffer, 0, _fftBuffer.Length);
                    Array.Clear(_lastSpectrum, 0, _lastSpectrum.Length);
                    Array.Clear(_window, 0, _window.Length);
                }
                
                (_source as IDisposable)?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
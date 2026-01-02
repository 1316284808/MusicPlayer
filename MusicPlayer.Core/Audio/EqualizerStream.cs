using System;
using NAudio.Wave;
using NAudio.Dsp;

namespace MusicPlayer.Core.Audio
{
    /// <summary>
    /// 均衡器音频处理流 - 对NAudio的音频流应用均衡器效果
    /// </summary>
    public class EqualizerStream : WaveStream
    {
        private readonly WaveStream _sourceStream;
        private readonly BiQuadFilter[] _filters;
        private readonly float[] _gains;
        private readonly bool[] _isEnabled;
        private readonly object _lockObject = new object();
        
        /// <summary>
        /// 初始化均衡器音频处理流
        /// </summary>
        /// <param name="sourceStream">源音频流</param>
        /// <param name="frequencyBands">频段频率数组 (Hz)</param>
        public EqualizerStream(WaveStream sourceStream, double[] frequencyBands)
        {
            _sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
            
            if (frequencyBands == null || frequencyBands.Length == 0)
            {
                throw new ArgumentNullException(nameof(frequencyBands));
            }
            
            int bandCount = frequencyBands.Length;
            _filters = new BiQuadFilter[bandCount];
            _gains = new float[bandCount];
            _isEnabled = new bool[bandCount];
            
            // 为每个频段创建滤波器
            for (int i = 0; i < bandCount; i++)
            {
                // 使用NAudio.Dsp的PeakingEQ滤波器
                _filters[i] = BiQuadFilter.PeakingEQ(
                    sourceStream.WaveFormat.SampleRate,
                    (float)frequencyBands[i],
                    1.4f, // 常用的Q因子
                    0.0f  // 初始增益为0dB
                );
                _gains[i] = 0; // 初始增益为0 dB
                _isEnabled[i] = false; // 初始不启用
            }
            
            System.Diagnostics.Debug.WriteLine($"EqualizerStream: 已创建{bandCount}个PeakingEQ滤波器");
        }

        /// <summary>
        /// 设置指定频段的增益
        /// </summary>
        /// <param name="bandIndex">频段索引</param>
        /// <param name="gainDb">增益 (dB)</param>
        public void SetBandGain(int bandIndex, float gainDb)
        {
            lock (_lockObject)
            {
                if (bandIndex >= 0 && bandIndex < _filters.Length)
                {
                    // 限制增益范围
                    gainDb = Math.Clamp(gainDb, -12f, 12f);
                    
                    // 检查值是否有变化
                    if (Math.Abs(_gains[bandIndex] - gainDb) < 0.01f)
                    {
                        System.Diagnostics.Debug.WriteLine($"EqualizerStream: 频段{bandIndex}增益无变化，仍为{_gains[bandIndex]:F1}dB");
                        return;
                    }
                    
                    _gains[bandIndex] = gainDb;
                    
                    // 重新创建PeakingEQ滤波器，因为NAudio.Dsp的BiQuadFilter不支持直接修改参数
                    _filters[bandIndex] = BiQuadFilter.PeakingEQ(
                        _sourceStream.WaveFormat.SampleRate,
                        GetFilterFrequency(bandIndex),
                        1.4f, // 常用的Q因子
                        gainDb  // 直接使用dB值
                    );
                    
                    // 如果增益为0dB，禁用该频段以节省CPU
                    _isEnabled[bandIndex] = Math.Abs(gainDb) > 0.01f; // 降低阈值，使0dB附近的增益也能应用
                    
                    // 添加调试信息
                    System.Diagnostics.Debug.WriteLine($"EqualizerStream: 频段{bandIndex}增益设置为{gainDb}dB，频率={GetFilterFrequency(bandIndex)}Hz，启用状态={_isEnabled[bandIndex]}");
                }
            }
        }
        
        /// <summary>
        /// 获取滤波器的频率
        /// </summary>
        /// <param name="bandIndex">频段索引</param>
        /// <returns>频率 (Hz)</returns>
        private float GetFilterFrequency(int bandIndex)
        {
            // 使用EqualizerSettings中定义的频率
            return bandIndex switch
            {
                0 => 32f,
                1 => 64f,
                2 => 125f,
                3 => 250f,
                4 => 500f,
                5 => 1000f,
                6 => 2000f,
                7 => 4000f,
                8 => 8000f,
                9 => 16000f,
                _ => 1000f // 默认值
            };
        }

        /// <summary>
        /// 获取指定频段的增益
        /// </summary>
        /// <param name="bandIndex">频段索引</param>
        /// <returns>增益 (dB)</returns>
        public float GetBandGain(int bandIndex)
        {
            lock (_lockObject)
            {
                if (bandIndex >= 0 && bandIndex < _gains.Length)
                    return _gains[bandIndex];
                return 0f;
            }
        }

        /// <summary>
        /// 设置所有频段的增益
        /// </summary>
        /// <param name="gainsDb">增益数组 (dB)</param>
        public void SetAllBandGains(float[] gainsDb)
        {
            if (gainsDb == null) 
            {
                System.Diagnostics.Debug.WriteLine("EqualizerStream: SetAllBandGains - 增益数组为null");
                return;
            }
            
            lock (_lockObject)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerStream: SetAllBandGains - 设置{gainsDb.Length}个频段增益");
                
                for (int i = 0; i < Math.Min(_filters.Length, gainsDb.Length); i++)
                {
                    // 限制增益范围
                    float gainDb = Math.Clamp(gainsDb[i], -12f, 12f);
                    
                    _gains[i] = gainDb;
                    
                    // 重新创建PeakingEQ滤波器
                    _filters[i] = BiQuadFilter.PeakingEQ(
                        _sourceStream.WaveFormat.SampleRate,
                        GetFilterFrequency(i),
                        1.4f, // 常用的Q因子
                        gainDb  // 直接使用dB值
                    );
                    
                    // 如果增益为0dB，禁用该频段以节省CPU
                    _isEnabled[i] = Math.Abs(gainDb) > 0.01f;
                    
                    System.Diagnostics.Debug.WriteLine($"EqualizerStream: 频段{i}增益设置为{gainDb:F1}dB，频率={GetFilterFrequency(i)}Hz");
                }
                
                System.Diagnostics.Debug.WriteLine($"EqualizerStream: 所有频段增益设置完成");
            }
        }

        /// <summary>
        /// 重置所有频段的增益
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                for (int i = 0; i < _filters.Length; i++)
                {
                    // 重新创建滤波器为0dB增益
                    _filters[i] = BiQuadFilter.PeakingEQ(
                        _sourceStream.WaveFormat.SampleRate,
                        GetFilterFrequency(i),
                        1.4f, // 常用的Q因子
                        0f  // 0dB增益
                    );
                    _gains[i] = 0f;
                    _isEnabled[i] = false;
                }
                
                System.Diagnostics.Debug.WriteLine("EqualizerStream: 所有频段已重置为0dB");
            }
        }

        /// <summary>
        /// 音频格式
        /// </summary>
        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

        /// <summary>
        /// 流长度（字节）
        /// </summary>
        public override long Length => _sourceStream.Length;

        /// <summary>
        /// 当前位置（字节）
        /// </summary>
        public override long Position
        {
            get => _sourceStream.Position;
            set
            {
                _sourceStream.Position = value;
                ResetFilters();
            }
        }

        /// <summary>
        /// 重置所有滤波器状态
        /// </summary>
        private void ResetFilters()
        {
            lock (_lockObject)
            {
                // 重新创建所有滤波器
                for (int i = 0; i < _filters.Length; i++)
                {
                    _filters[i] = BiQuadFilter.PeakingEQ(
                        _sourceStream.WaveFormat.SampleRate,
                        GetFilterFrequency(i),
                        1.4f, // 常用的Q因子
                        _gains[i] // 使用当前增益值
                    );
                }
                
                System.Diagnostics.Debug.WriteLine("EqualizerStream: 所有滤波器状态已重置");
            }
        }

        /// <summary>
        /// 读取音频数据
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">字节数</param>
        /// <returns>实际读取的字节数</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _sourceStream.Read(buffer, offset, count);
            
            if (bytesRead == 0)
                return 0;
            
            // 检查是否有任何频段启用
            bool anyBandEnabled = false;
            lock (_lockObject)
            {
                foreach (bool enabled in _isEnabled)
                {
                    if (enabled)
                    {
                        anyBandEnabled = true;
                        break;
                    }
                }
            }
            
            // 如果没有频段启用，直接返回原始数据
            if (!anyBandEnabled)
                return bytesRead;
            
            // 处理音频数据
            ProcessAudioData(buffer, offset, bytesRead);
            
            return bytesRead;
        }

        /// <summary>
        /// 处理音频数据
        /// </summary>
        /// <param name="buffer">音频缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">字节数</param>
        private void ProcessAudioData(byte[] buffer, int offset, int count)
        {
            // 根据位深度和声道数确定样本处理方式
            int bytesPerSample = _sourceStream.WaveFormat.BitsPerSample / 8;
            int channels = _sourceStream.WaveFormat.Channels;
            int samples = count / (bytesPerSample * channels);
            
            lock (_lockObject)
            {
                // 处理每个样本
                for (int i = 0; i < samples; i++)
                {
                    int sampleOffset = offset + i * bytesPerSample * channels;
                    
                    // 处理立体声
                    if (channels >= 2)
                    {
                        // 左声道
                        float leftSample = ConvertBytesToFloat(buffer, sampleOffset, bytesPerSample);
                        float processedLeft = leftSample;
                        
                        // 右声道
                        float rightSample = ConvertBytesToFloat(buffer, sampleOffset + bytesPerSample, bytesPerSample);
                        float processedRight = rightSample;
                        
                        // 应用所有启用的滤波器
                        for (int band = 0; band < _filters.Length; band++)
                        {
                            if (_isEnabled[band])
                            {
                                processedLeft = _filters[band].Transform(processedLeft);
                                processedRight = _filters[band].Transform(processedRight);
                            }
                        }
                        
                        // 写回处理后的样本
                        ConvertFloatToBytes(processedLeft, buffer, sampleOffset, bytesPerSample);
                        ConvertFloatToBytes(processedRight, buffer, sampleOffset + bytesPerSample, bytesPerSample);
                    }
                    else // 单声道
                    {
                        float sample = ConvertBytesToFloat(buffer, sampleOffset, bytesPerSample);
                        float processedSample = sample;
                        
                        // 应用所有启用的滤波器
                        for (int band = 0; band < _filters.Length; band++)
                        {
                            if (_isEnabled[band])
                            {
                                processedSample = _filters[band].Transform(processedSample);
                            }
                        }
                        
                        // 写回处理后的样本
                        ConvertFloatToBytes(processedSample, buffer, sampleOffset, bytesPerSample);
                    }
                }
            }
        }

        /// <summary>
        /// 将字节数组转换为浮点样本
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="bytesPerSample">每样本字节数</param>
        /// <returns>浮点样本值</returns>
        private float ConvertBytesToFloat(byte[] buffer, int offset, int bytesPerSample)
        {
            if (bytesPerSample == 2) // 16位
            {
                short sample = BitConverter.ToInt16(buffer, offset);
                return sample / 32768f;
            }
            else if (bytesPerSample == 4) // 32位
            {
                return BitConverter.ToSingle(buffer, offset);
            }
            else if (bytesPerSample == 3) // 24位
            {
                int sample = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
                // 如果最高位是1，则扩展符号位
                if ((sample & 0x800000) != 0)
                {
                    unchecked
                    {
                        sample |= (int)0xFF000000;
                    }
                }
                return sample / 8388608f;
            }
            else
            {
                throw new NotSupportedException($"不支持的位深度: {bytesPerSample * 8}位");
            }
        }

        /// <summary>
        /// 将浮点样本转换为字节数组
        /// </summary>
        /// <param name="sample">浮点样本值</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="bytesPerSample">每样本字节数</param>
        private void ConvertFloatToBytes(float sample, byte[] buffer, int offset, int bytesPerSample)
        {
            // 限制范围
            sample = Math.Clamp(sample, -1f, 1f);
            
            if (bytesPerSample == 2) // 16位
            {
                short value = (short)(sample * 32767f);
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, buffer, offset, 2);
            }
            else if (bytesPerSample == 4) // 32位
            {
                byte[] bytes = BitConverter.GetBytes(sample);
                Array.Copy(bytes, 0, buffer, offset, 4);
            }
            else if (bytesPerSample == 3) // 24位
            {
                int value = (int)(sample * 8388607f);
                buffer[offset] = (byte)(value & 0xFF);
                buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
                buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            }
            else
            {
                throw new NotSupportedException($"不支持的位深度: {bytesPerSample * 8}位");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
using MusicPlayer.Core.Enums;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

namespace MusicPlayer.Core.Audio
{
    /// <summary>
    /// 音频引擎管理器 - 负责管理不同音频引擎的初始化和配置
    /// </summary>
    public class AudioEngineManager : IDisposable
    {
        private IWavePlayer? _waveOut;
        private readonly object _audioLock = new();

        /// <summary>
        /// 当前音频引擎
        /// </summary>
        public AudioEngine CurrentAudioEngine { get; private set; }
        
        public  int DesiredLatency => 150;
        /// <summary>
        /// 创建音频输出设备
        /// </summary>
        /// <param name="audioEngine">音频引擎类型</param>
        /// <param name="audioStream">音频流</param>
        /// <param name="volume">初始音量 (0.0f - 1.0f)</param>
        /// <returns>创建的音频输出设备</returns>
        public IWavePlayer CreateAudioOutputDevice(AudioEngine audioEngine, object audioStream, float volume)
        {
            lock (_audioLock)
            {
                // 释放旧设备
                DisposeAudioDevice();

                CurrentAudioEngine = audioEngine;

                switch (audioEngine)
                {
                    case AudioEngine.DirectSound:
                        _waveOut = InitializeDirectSoundDevice(audioStream, volume);
                        break;
                    case AudioEngine.WASAPI:
                        _waveOut = InitializeWasapiDevice(audioStream, volume);
                        break;
                    case AudioEngine.Auto:
                    default:
                        _waveOut = InitializeWaveOutDevice(audioStream, volume);
                        break;
                }

                return _waveOut;
            }
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量值 (0.0f - 1.0f)</param>
        public void SetVolume(float volume)
        {
            lock (_audioLock)
            {
                if (_waveOut != null)
                {
                    _waveOut.Volume = volume;
                }
            }
        }

        /// <summary>
        /// 获取当前音频输出设备
        /// </summary>
        /// <returns>当前音频输出设备</returns>
        public IWavePlayer? GetCurrentAudioDevice()
        {
            return _waveOut;
        }

        /// <summary>
        /// 初始化WaveOutEvent音频设备
        /// </summary>
        private IWavePlayer InitializeWaveOutDevice(object audioStream, float volume)
        {
            var waveOut = new WaveOutEvent();
            waveOut.DesiredLatency = DesiredLatency;
            waveOut.Volume = volume;

            // 检查音频流类型并初始化
            if (audioStream is WaveStream waveStream)
            {
                waveOut.Init(waveStream);
            }
            else if (audioStream is ISampleProvider sampleProvider)
            {
                waveOut.Init(sampleProvider);
            }

            return waveOut;
        }

        /// <summary>
        /// 初始化DirectSound音频设备
        /// </summary>
        private IWavePlayer InitializeDirectSoundDevice(object audioStream, float volume)
        {
            var directSoundOut = new NAudio.Wave.DirectSoundOut(DesiredLatency);
            directSoundOut.Volume = volume;

            // 检查音频流类型并初始化
            if (audioStream is WaveStream waveStream)
            {
                directSoundOut.Init(waveStream);
            }
            else if (audioStream is ISampleProvider sampleProvider)
            {
                directSoundOut.Init(sampleProvider);
            }

            return directSoundOut;
        }

        /// <summary>
        /// 初始化WASAPI音频设备
        /// </summary>
        private IWavePlayer InitializeWasapiDevice(object audioStream, float volume)
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var mmDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                var wasapiOut = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, DesiredLatency);

                wasapiOut.Volume = volume;

                // 订阅播放停止事件，处理设备断开等异常
                wasapiOut.PlaybackStopped += (sender, e) =>
                {
                    // 检查是否由设备断开引起的异常
                    if (e.Exception != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"WasapiOut播放中断：{e.Exception.Message}，原因：设备断开/异常，正在自动恢复播放...");
                        // 重新初始化WASAPI，使用新的默认设备
                        try
                        {
                            lock (_audioLock)
                            {
                                if (_waveOut == sender)
                                {
                                    // 保存当前播放状态
                                    var wasPlaying = _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing;
                                    
                                    // 释放旧设备
                                    DisposeAudioDevice();
                                    
                                    // 重新初始化WASAPI
                                    var newWasapiOut = InitializeWasapiDevice(audioStream, volume);
                                    _waveOut = newWasapiOut;
                                    
                                    // 恢复播放
                                    if (wasPlaying)
                                    {
                                        _waveOut.Play();
                                        System.Diagnostics.Debug.WriteLine("WasapiOut设备断开后已自动恢复播放");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"WasapiOut恢复失败：{ex.Message}，正在切换到WaveOut音频引擎");
                            // 如果恢复失败，切换到WaveOut
                            lock (_audioLock)
                            {
                                if (_waveOut == sender)
                                {
                                    DisposeAudioDevice();
                                    _waveOut = InitializeWaveOutDevice(audioStream, volume);
                                    _waveOut.Play();
                                }
                            }
                        }
                    }
                };

                // 检查音频流类型并初始化
                if (audioStream is WaveStream waveStream)
                {
                    wasapiOut.Init(waveStream);
                }
                else if (audioStream is ISampleProvider sampleProvider)
                {
                    wasapiOut.Init(sampleProvider);
                }

                return wasapiOut;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WasapiOut初始化失败：{ex.Message}，正在切换到WaveOut音频引擎");
                // 如果WASAPI初始化失败，回退到WaveOut
                return InitializeWaveOutDevice(audioStream, volume);
            }
        }

        /// <summary>
        /// 释放音频设备
        /// </summary>
        private void DisposeAudioDevice()
        {
            if (_waveOut != null)
            {
                try
                {
                    _waveOut.Stop();
                    _waveOut.Dispose();
                }
                catch { }
                finally
                {
                    _waveOut = null;
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            DisposeAudioDevice();
        }
    }
}
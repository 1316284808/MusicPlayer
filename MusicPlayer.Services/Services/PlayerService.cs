using MusicPlayer.Core.Audio;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;
using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 音频播放服务 - 负责音频设备管理、频谱分析等核心技术功能
    /// </summary>
    public class PlayerService : IPlayerService
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaylistService _playlistService;
        private readonly IMessagingService _messagingService;
        private readonly INotificationService _notificationService;
        private readonly ISystemMediaTransportService _mediaTransportService;
        private readonly IEqualizerService _equalizerService;
        private readonly IConfigurationService _configurationService;
        
        // 音频播放相关字段 - 确保全局单例，防止多个音频引擎共存
        private IWavePlayer? _waveOut;
        private AudioFileReader? _audioFileReader;
        private SpectrumAnalyzer? _spectrumAnalyzer;
        private readonly object _audioLock = new();
        private readonly DispatcherTimer _timer;
        
        // 单例保护 - 防止多实例（使用线程安全的方式）
        private static readonly object _initLock = new object();
        private static bool _isAudioEngineInitialized = false;
        
        // 状态管理 - 统一使用 PlayerStateService 作为唯一可信源
        private readonly IPlayerStateService _playerStateService;
        private const int _fftLength = 1024;
        private bool _isUpdatingFromUI = false; // 防止循环更新标志位
        private bool _hasTriggeredNextSong = false; // 防止重复触发下一首
        
        // 频谱数据优化 - 使用预分配缓冲区和高效复制
        private float[] _spectrumDataBuffer = null; // 频谱数据缓冲区，避免重复分配
        private int _spectrumDataLength = 32; // 频谱数据长度，与UI显示一致播放
        private readonly object _spectrumLock = new object(); // 频谱数据线程锁
        
        // 跟踪当前加载的歌曲，避免重复加载
        private Song? _currentlyLoadedSong;
      

        // 事件
        public event EventHandler<bool>? PlaybackStateChanged;
        public event EventHandler<Song?>? CurrentSongChanged;
        public event EventHandler<double>? PlaybackProgressChanged;
        public event EventHandler<float[]>? SpectrumDataChanged;



        // IPlayerService接口实现 - 统一委托给 PlayerStateService
        public Song? CurrentSong => _playlistDataService.CurrentSong; // 使用PlaylistDataService作为歌曲可信源
        public bool IsPlaying => _playerStateService.IsPlaying;
        public bool IsMuted 
        { 
            get => _playerStateService.IsMuted;
            set => _messagingService.Send(new MuteToggleMessage());
        }
        
        public float Volume 
        { 
            get => _playerStateService.Volume;
            set => _messagingService.Send(new VolumeSetMessage(value));
        }
        
        public double CurrentPosition 
        { 
            get => _playerStateService.CurrentPosition;
            set => _messagingService.Send(new SeekMessage(value));
        }
        public double MaxPosition => _playerStateService.MaxPosition;
        public PlayMode CurrentPlayMode 
        { 
            get => _playerStateService.CurrentPlayMode;
            set => _messagingService.Send(new PlayModeChangedMessage(value));
        }
        public float[] SpectrumData => _playerStateService.SpectrumData;

        public PlayerService(
            IPlaylistDataService playlistDataService, 
            IPlaylistService playlistService, 
            IMessagingService messagingService, 
            INotificationService notificationService, 
            ISystemMediaTransportService mediaTransportService,
            IEqualizerService equalizerService,
            IPlayerStateService playerStateService,
            IConfigurationService configurationService,
            Microsoft.Extensions.Logging.ILogger<PlayerService>? logger = null)
        {
            // 添加实例ID日志，用于调试单例问题
            System.Diagnostics.Debug.WriteLine($"PlayerService: 创建新实例，ID: {GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"PlayerService: PlayerStateService实例ID: {playerStateService.GetHashCode()}");
            
            // 单例保护 - 确保只有一个音频引擎实例（线程安全）
            lock (_initLock)
            {
                if (_isAudioEngineInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("警告: PlayerService尝试多次初始化，忽略后续初始化");
                    return;
                }
                _isAudioEngineInitialized = true;
            }

            _playlistDataService = playlistDataService;
            _playlistService = playlistService;
            _messagingService = messagingService;
            _notificationService = notificationService;
            _mediaTransportService = mediaTransportService;
            _equalizerService = equalizerService;
            _playerStateService = playerStateService;
            _configurationService = configurationService;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(60); // 降低到60ms，减少CPU负载，提高兼容性
            _timer.Tick += Timer_Tick;

            // 订阅播放列表数据服务的事件
            _messagingService.Register<CurrentSongChangedMessage>(this, OnCurrentSongChanged);

            // 初始化 SMTC 服务
            _ = InitializeMediaTransportAsync();
          
          
            // PlaySelectedSongMessage  由 PlayerControlMessageHandler 统一处理，避免重复播放
            _messagingService.Register<VolumeChangedMessage>(this, OnVolumeChangedMessage);
            _messagingService.Register<MuteStateChangedMessage>(this, OnMuteStateChangedMessage);
            _messagingService.Register<AudioEngineChangedMessage>(this, OnAudioEngineChangedMessage);

            // 订阅 SMTC 控制事件
            _mediaTransportService.PlayOrPauseRequested += OnPlayOrPauseRequested;
            _mediaTransportService.NextRequested += OnNextRequested;
            _mediaTransportService.PreviousRequested += OnPreviousRequested;
            }

        private async Task InitializeMediaTransportAsync()
        {
            try
            {
                await _mediaTransportService.InitializeAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"系统媒体控制初始化失败: {ex.Message}");
            }
        }

        private void OnPlayOrPauseRequested(object? sender, EventArgs e)
        {
            // 简化Dispatcher调用，确保在正确的线程上执行
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 通过消息机制发送播放/暂停请求
                    _messagingService.Send(new PlayPauseMessage());
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"播放/暂停失败: {ex.Message}");
                }
            });
        }

        private void OnNextRequested(object? sender, EventArgs e)
        {
            // 简化Dispatcher调用，确保在正确的线程上执行
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 通过消息机制切换下一首歌曲
                    _messagingService.Send(new NextSongMessage());
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"下一首失败: {ex.Message}");
                }
            });
        }

        private void OnPreviousRequested(object? sender, EventArgs e)
        {
            // 简化Dispatcher调用，确保在正确的线程上执行
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 切换到上一首歌曲（通过消息机制）
                    _messagingService.Send(new PreviousSongMessage());
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"上一首失败: {ex.Message}");
                }
            });
        }



        private void OnCurrentSongChanged(object recipient, CurrentSongChangedMessage message)
        {
            // 不再发送重复的CurrentSongChangedMessage，因为PlaylistManagerService已经发送了
            // 只需要触发本地事件和更新系统媒体控制
            CurrentSongChanged?.Invoke(this, message.Value);
            
            // 同步媒体信息到系统控制
            _ = _mediaTransportService.UpdateMediaInfoAsync(message.Value);
            
            // 注意：不再直接调用LoadSong，而是由PlayerControlMessageHandler统一处理播放逻辑
            // 这样可以避免重复加载同一首歌曲，解决音频引擎被重复初始化的问题
        }

        public void LoadSong(Song song)
        {
            lock (_audioLock)
            {
                // 重置下一首触发标志
                _hasTriggeredNextSong = false;
                // 释放当前歌曲的高清封面资源，避免内存泄漏
                if (_playlistDataService.CurrentSong != null && _playlistDataService.CurrentSong != song)
                {
                    _playlistDataService.CurrentSong.OriginalAlbumArt = null;
                    System.Diagnostics.Debug.WriteLine($"释放旧歌曲 {_playlistDataService.CurrentSong.Title} 的高清封面资源");
                }

                // 完全停止并清理当前音频资源
                StopAudio();

                // 通过消息机制重置播放状态，而不是直接设置
                _messagingService.Send(new PlaybackStateChangedMessage(false));

                // 创建音频文件读取器
                _audioFileReader = CreateAudioFileReader(song.FilePath);
                if (_audioFileReader == null) return;

                // 创建频谱分析器
                _spectrumAnalyzer = new SpectrumAnalyzer(_audioFileReader, _fftLength);

                // 应用均衡器效果
                var audioStreamWithEqualizer = _equalizerService.ApplyEqualizer(_spectrumAnalyzer);

                   CreateAudioOutputDevice(audioStreamWithEqualizer);
                // 更新播放列表数据服务的当前歌曲状态（仅当不同时才更新，避免循环调用）
                if (_playlistDataService.CurrentSong != song)
                {
                    _playlistDataService.CurrentSong = song;
                } 
                // 设置音量（直接使用PlayerStateService的当前值）
                _audioFileReader.Volume = _playerStateService.IsMuted ? 0.0f : _playerStateService.Volume;
                bool isUserAction = _playlistDataService.DataSource.Any();
                if (isUserAction)
                {
                    _configurationService.UpdateLastPlayedSongId(song.Id);
                    System.Diagnostics.Debug.WriteLine($"PlayerService: 用户切换歌曲，更新最后播放歌曲ID为 {song.Id}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"PlayerService: 应用启动加载歌曲 {song.Title}，不更新最后播放歌曲ID");
                }
                
                // 加载歌词
                var lyrics = _playlistService.LoadLyrics(song.FilePath);
                _messagingService.Send(new LyricsUpdatedMessage(new ObservableCollection<LyricLine>(lyrics)));

                // 发送最大位置信息 - 从音频文件读取器获取实际时长
                if (_audioFileReader != null)
                {
                    double maxPos = _audioFileReader.TotalTime.TotalSeconds;
                    
                    // 通过消息机制设置最大位置和重置播放进度
                    _messagingService.Send(new MaxPositionChangedMessage(maxPos));
                    _messagingService.Send(new PlaybackProgressChangedMessage(0));
                    System.Diagnostics.Debug.WriteLine($"PlayerService: 加载歌曲 {song.Title}，重置播放进度到0");
                }

                System.Diagnostics.Debug.WriteLine($"歌曲加载完成: {song.Title}");
            }
        }

        public void StartPlayback()
        {
            lock (_audioLock)
            {
                if (_waveOut != null && !_playerStateService.IsPlaying)
                {
                    _waveOut.Play();
                    var isPlaying = true;
                    _timer.Start();
                    
                    // 确保频谱缓冲区已初始化
                    lock (_spectrumLock)
                    {
                        if (_spectrumDataBuffer == null)
                        {
                            _spectrumDataBuffer = new float[_spectrumDataLength];
                        }
                    }
                    
                    // 通过消息机制更新播放状态，而不是直接设置
                    _messagingService.Send(new PlaybackStateChangedMessage(isPlaying));
                    PlaybackStateChanged?.Invoke(this, isPlaying);
                    
                    // 同步到系统媒体控制
                    _ = _mediaTransportService.UpdatePlaybackStatusAsync(isPlaying);
                }
            }
        }

        public void PausePlayback()
        {
            lock (_audioLock)
            {
                if (_waveOut != null && _playerStateService.IsPlaying)
                {
                    _waveOut.Pause();
                    var isPlaying = false;
                    _timer.Stop();
                    
                    // 通过消息机制更新播放状态，而不是直接设置
                    _messagingService.Send(new PlaybackStateChangedMessage(isPlaying));
                    PlaybackStateChanged?.Invoke(this, isPlaying);
                    
                    // 同步到系统媒体控制
                    _ = _mediaTransportService.UpdatePlaybackStatusAsync(isPlaying);
                }
            }
        }

        public void StopPlayback()
        {
            lock (_audioLock)
            {
                StopAudio();
                
                // 重置下一首触发标志
                _hasTriggeredNextSong = false;
                
                // 清理频谱数据缓冲区，避免残留数据
                lock (_spectrumLock)
                {
                    if (_spectrumDataBuffer != null)
                    {
                        Array.Clear(_spectrumDataBuffer, 0, _spectrumDataBuffer.Length);
                    }
                }
                
                // 发送空的频谱数据，清空UI显示
                _messagingService.Send(new SpectrumDataUpdatedMessage(new float[0]));
                
                // 通过消息机制更新播放状态，而不是直接设置
                _messagingService.Send(new PlaybackStateChangedMessage(false));
                PlaybackStateChanged?.Invoke(this, false);
                
                // 停止时重置最大位置
                _messagingService.Send(new MaxPositionChangedMessage(0));
                
                // 同步到系统媒体控制
                _ = _mediaTransportService.UpdatePlaybackStatusAsync(false);
                
                // 停止时清空媒体信息
                _ = _mediaTransportService.UpdateMediaInfoAsync(null);
            }
        }


        public void SeekToPosition(double position)
        {
            lock (_audioLock)
            {
                if (_audioFileReader != null && _audioFileReader.TotalTime.TotalSeconds > 0)
                {
                    var newPosition = Math.Clamp(position, 0.0, _audioFileReader.TotalTime.TotalSeconds);
                    System.Diagnostics.Debug.WriteLine($"PlayerService: Seek 从 {position} 到 {newPosition}");
                    
                    // 设置防循环标志，防止定时器更新
                    _isUpdatingFromUI = true;
                    
                    _audioFileReader.CurrentTime = TimeSpan.FromSeconds(newPosition);
                    PlaybackProgressChanged?.Invoke(this, newPosition);
                    
                    // 通过PlayerStateService更新位置，而不是发送SeekMessage避免循环调用
                    // 使用类型转换调用SetPositionByUser方法，确保Seek操作的响应性
                    if (_playerStateService is PlayerStateService playerStateService)
                    {
                        playerStateService.SetPositionByUser(newPosition);
                    }
                    else
                    {
                        // 如果类型转换失败，回退到属性设置
                        _playerStateService.CurrentPosition = newPosition;
                    }
                    
                    // 重置防循环标志
                    _isUpdatingFromUI = false;
                }
            }
        }

        private void StopAudio()
        {
            // 安全地停止和释放音频设备
            try
            {
                if (_waveOut != null)
                {
                    _waveOut.Stop();
                    _waveOut.Dispose();
                    _waveOut = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止音频设备时出错: {ex.Message}");
                _waveOut = null; // 确保即使出错也清空引用
            }
            
            // 安全地释放音频文件读取器
            try
            {
                if (_audioFileReader != null)
                {
                    _audioFileReader.Dispose();
                    _audioFileReader = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放音频文件读取器时出错: {ex.Message}");
                _audioFileReader = null; // 确保即使出错也清空引用
            }
            
            // 安全地释放频谱分析器
            try
            {
                if (_spectrumAnalyzer != null)
                {
                    _spectrumAnalyzer.Dispose();
                    _spectrumAnalyzer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放频谱分析器时出错: {ex.Message}");
                _spectrumAnalyzer = null; // 确保即使出错也清空引用
            }
            
            _timer.Stop();
        }
        /// <summary>
        /// 仅初始化一次
        /// </summary>
        private void CreateAudioOutputDevice(object? audioStream = null)
        {
            try
            {
                // 安全地停止和释放旧音频设备
               
                    if (_waveOut != null)
                    {
                        _waveOut.Stop();
                        _waveOut.Dispose();
                        _waveOut = null;
                    }
                var audioEngine = _playerStateService.CurrentAudioEngine;
                System.Diagnostics.Debug.WriteLine($"PlayerService: 使用音频引擎 {audioEngine} 初始化音频输出设备");

                // 如果没有提供音频流，则使用频谱分析器
                audioStream ??= _spectrumAnalyzer;

                switch (audioEngine)
                {
                    case AudioEngine.DirectSound:
                        InitializeDirectSoundDevice(audioStream);
                        break;
                    case AudioEngine.WASAPI:
                        InitializeWasapiDevice(audioStream);
                        break;
                    case AudioEngine.Auto:
                    default:
                        InitializeWaveOutDevice(audioStream);
                        break;
                }
            }
            catch (Exception ex)
            {
                _waveOut?.Dispose();
                _waveOut = null;
                System.Diagnostics.Debug.WriteLine($"音频设备初始化失败: {ex.Message}");
                
                // 尝试使用默认音频引擎作为后备
                if (_playerStateService.CurrentAudioEngine != AudioEngine.Auto)
                {
                    System.Diagnostics.Debug.WriteLine("尝试使用默认音频引擎作为后备");
                    _playerStateService.CurrentAudioEngine = AudioEngine.Auto;
                    CreateAudioOutputDevice(audioStream);
                }
            }
        }

        /// <summary>
        /// 初始化WaveOutEvent音频设备
        /// </summary>
        private void InitializeWaveOutDevice(object audioStream)
        {
          
            _waveOut = new WaveOutEvent();
            _waveOut.Volume = _playerStateService.IsMuted ? 0.0f : _playerStateService.Volume;
            
            // 检查音频流类型并初始化
            if (audioStream is WaveStream waveStream)
            {
                _waveOut.Init(waveStream);
            }
            else if (audioStream is ISampleProvider sampleProvider)
            {
                _waveOut.Init(sampleProvider);
            }
            else
            {
                // 默认使用频谱分析器
                _waveOut.Init(_spectrumAnalyzer);
            }
            
            System.Diagnostics.Debug.WriteLine("WaveOutEvent音频设备初始化成功");
        }

        /// <summary>
        /// 初始化DirectSound音频设备
        /// </summary>
        private void InitializeDirectSoundDevice(object audioStream)
        {
          
            var directSoundOut = new NAudio.Wave.DirectSoundOut( );
            _waveOut = directSoundOut;
            _waveOut.Volume = _playerStateService.IsMuted ? 0.0f : _playerStateService.Volume;
            
            // 检查音频流类型并初始化
            if (audioStream is WaveStream waveStream)
            {
                _waveOut.Init(waveStream);
            }
            else if (audioStream is ISampleProvider sampleProvider)
            {
                _waveOut.Init(sampleProvider);
            }
            else
            {
                // 默认使用频谱分析器
                _waveOut.Init(_spectrumAnalyzer);
            }
            
            System.Diagnostics.Debug.WriteLine("DirectSound音频设备初始化成功");
        }





      

        /// <summary>
        /// 初始化WASAPI音频设备
        /// </summary>
        private void InitializeWasapiDevice(object audioStream)
        {
           

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var mmDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                var wasapiOut = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 200);

                _waveOut = wasapiOut;
                _waveOut.Volume = _playerStateService.IsMuted ? 0.0f : _playerStateService.Volume;

                // ===== 核心修复：绑定播放停止事件，监听设备断开/播放异常 =====
                _waveOut.PlaybackStopped += (sender, e) =>
                {
                    // 播放停止的原因是【设备异常/设备断开】，且当前有正在播放的音频流
                    if (e.Exception != null && audioStream != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"WASAPI播放中断：{e.Exception.Message}，原因：设备断开/异常，正在自动恢复播放...");
                        // 关键：重新初始化WASAPI，自动获取新的默认设备（扬声器）

                        InitializeWasapiDevice(audioStream);
                        // 恢复播放
                        _waveOut?.Play();
                    }
                };

                // 检查音频流类型并初始化
                if (audioStream is WaveStream waveStream)
                {
                    _waveOut.Init(waveStream);
                }
                else if (audioStream is ISampleProvider sampleProvider)
                {
                    _waveOut.Init(sampleProvider);
                }
                else
                {
                    // 默认使用频谱分析器
                    _waveOut.Init(_spectrumAnalyzer);
                }

                System.Diagnostics.Debug.WriteLine("WASAPI音频设备初始化成功，已绑定异常恢复播放");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WASAPI初始化失败：{ex.Message}");
            }
        }




        private AudioFileReader? CreateAudioFileReader(string filePath)
        {
            try
            {
                if (!Paths.FileExists(filePath))
                {
                    _notificationService.ShowError($"文件不存在: {filePath}");
                    return null;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                // 检查文件大小，防止处理过大的文件
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 500 * 1024 * 1024) // 500MB限制
                {
                    _notificationService.ShowWarning($"文件过大，无法处理: {Path.GetFileName(filePath)}");
                    return null;
                }
                
                if (extension == ".ogg")
                {
                    var vorbisReader = new VorbisWaveReader(filePath);
                    return new VorbisAudioFileReader(vorbisReader);
                }
                else if (extension == ".mp3" || extension == ".wav" || extension == ".flac" || 
                         extension == ".m4a" || extension == ".aac" || extension == ".wma")
                {
                    // 使用MediaFoundationReader时添加额外安全检查
                    try
                    {
                        return new AudioFileReader(filePath);
                    }
                    catch (System.NullReferenceException ex)
                    {
                        _notificationService.ShowError($"音频文件格式不支持或已损坏: {Path.GetFileName(filePath)} {ex.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError($"无法读取音频文件: {Path.GetFileName(filePath)} {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    _notificationService.ShowWarning($"不支持的音频格式: {extension}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"创建音频文件读取器失败: {ex.Message}");
                return null;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                lock (_audioLock)
                {
                    // 分离关键功能：播放进度更新优先级高于频谱分析
                    UpdatePlaybackProgress();
                    
                    // 如果音频已停止，重置标志位
                    if (_waveOut == null || !_waveOut.PlaybackState.ToString().Contains("Playing"))
                    {
                        _hasTriggeredNextSong = false;
                    }
                    
                    // 频谱分析独立处理，失败不影响播放
                    UpdateSpectrumData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Timer_Tick错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新播放进度 - 关键功能，高优先级
        /// </summary>
        private void UpdatePlaybackProgress()
        {
            if (_audioFileReader != null && _waveOut != null && !_isUpdatingFromUI)
            {
                var currentTime = _audioFileReader.CurrentTime;
                var totalTime = _audioFileReader.TotalTime;
                
                if (totalTime.TotalSeconds > 0)
                {
                    var position = currentTime.TotalSeconds;
                    
                    // 通过PlayerStateService管理播放进度
                    _messagingService.Send(new PlaybackProgressChangedMessage(position));
                    _messagingService.Send(new PlaybackProgressMessage(position));
                    
                    // 检查播放是否结束  添加0.5秒的缓冲 
                    if (!_hasTriggeredNextSong && position >= totalTime.TotalSeconds - 0.5)
                    {
                        System.Diagnostics.Debug.WriteLine($"播放即将结束: 当前时间={position}秒, 总时长={totalTime.TotalSeconds}秒");
                        _hasTriggeredNextSong = true;
                        _messagingService.Send(new NextSongMessage());
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新频谱数据 - 对象池减少GC压力
        /// </summary>
        private void UpdateSpectrumData()
        {
            try
            {
                if (_spectrumAnalyzer != null)
                {
                    lock (_spectrumLock)
                    {
                        // 初始化缓冲区（如果需要）
                        if (_spectrumDataBuffer == null)
                        {
                            _spectrumDataBuffer = new float[_spectrumDataLength];
                        }
                        _spectrumAnalyzer.CopySpectrumTo(_spectrumDataBuffer);
                        _playerStateService.UpdateSpectrumData(_spectrumDataBuffer);
                    }
                }
            }
            catch (Exception spectrumEx)
            {
                System.Diagnostics.Debug.WriteLine($"频谱分析器错误: {spectrumEx.Message}");
                
            }
        }


        /// <summary>
        /// 处理音量变化消息
        /// </summary>
        private void OnVolumeChangedMessage(object recipient, VolumeChangedMessage message)
        {
            lock (_audioLock)
            {
                // 获取当前状态值来计算实际音量
                var volume = _playerStateService.IsMuted ? 0.0f : message.Value;
                
                if (_waveOut != null)
                {
                    _waveOut.Volume = volume;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("音频输出设备为 null，无法应用音量");
                }
                
                if (_audioFileReader != null)
                {
                    _audioFileReader.Volume = volume;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AudioFileReader 为 null，无法应用音量");
                }
            }
        }

        /// <summary>
        /// 处理静音状态变化消息
        /// </summary>
        private void OnMuteStateChangedMessage(object recipient, MuteStateChangedMessage message)
        {
            System.Diagnostics.Debug.WriteLine($"收到静音状态变化消息，静音状态: {message.Value}");
            lock (_audioLock)
            {
                // 根据静音状态设置音量
                var volume = message.Value ? 0.0f : _playerStateService.Volume;
                
                if (_waveOut != null)
                {
                    _waveOut.Volume = volume;
                    System.Diagnostics.Debug.WriteLine($"静音状态下应用音量到音频输出设备: {volume}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("音频输出设备为 null，无法应用音量");
                }
                
                if (_audioFileReader != null)
                {
                    _audioFileReader.Volume = volume;
                    System.Diagnostics.Debug.WriteLine($"静音状态下应用音量到 AudioFileReader: {volume}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AudioFileReader 为 null，无法应用音量");
                }
            }
        }

        /// <summary>
        /// 处理音频引擎变更消息
        /// </summary>
        private void OnAudioEngineChangedMessage(object recipient, AudioEngineChangedMessage message)
        {
            System.Diagnostics.Debug.WriteLine($"PlayerService: 收到音频引擎变更消息，新引擎: {message.AudioEngine}");
            
            // 如果当前有正在播放的歌曲，需要重新加载以应用新的音频引擎
            if (CurrentSong != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        // 保存当前播放状态
                        var wasPlaying = _playerStateService.IsPlaying;
                        
                        System.Diagnostics.Debug.WriteLine($"PlayerService: 重新加载歌曲 {CurrentSong.Title} 以应用新的音频引擎，播放进度将重置为0");
                        
                        LoadSong(CurrentSong);
                        
                        if (wasPlaying)
                        {
                            StartPlayback();
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"PlayerService: 音频引擎切换完成，播放进度已重置为0");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PlayerService: 切换音频引擎时出错: {ex.Message}");
                        _notificationService.ShowError($"切换音频引擎失败: {ex.Message}");
                    }
                });
            }
        }

      
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_audioLock)
                {
                    try
                    {
                        // 停止定时器
                        _timer?.Stop();
                        _timer.Tick -= Timer_Tick;

                        // 停止音频播放
                        _waveOut?.Stop();
                        _waveOut?.Dispose();
                        // 释放音频文件读取器
                        _audioFileReader?.Dispose();
                        // 释放频谱分析器
                        _spectrumAnalyzer?.Dispose();
                        
                        // 清理频谱数据缓冲区
                        lock (_spectrumLock)
                        {
                            if (_spectrumDataBuffer != null)
                            {
                                Array.Clear(_spectrumDataBuffer, 0, _spectrumDataBuffer.Length);
                                _spectrumDataBuffer = null;
                            }
                        }
                        // 取消订阅 SMTC 事件
                        _mediaTransportService.PlayOrPauseRequested -= OnPlayOrPauseRequested;
                        _mediaTransportService.NextRequested -= OnNextRequested;
                        _mediaTransportService.PreviousRequested -= OnPreviousRequested;
                         // 释放 SMTC 服务
                        _mediaTransportService?.Dispose();
                        
                        // 取消订阅所有消息
                        _messagingService.Unregister(this);
                        
                        // 重置单例标志，允许重新创建实例（线程安全）
                        lock (_initLock)
                        {
                            _isAudioEngineInitialized = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不抛出异常
                        System.Diagnostics.Debug.WriteLine($"释放PlayerService资源时出错: {ex.Message}");
                    }
                }
            }
        }
    }
}
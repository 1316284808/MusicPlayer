using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌词视图模型
    /// </summary>
    public partial class LyricsViewModel : ObservableObject, ILyricsViewModel
    {
        private readonly IPlayerStateService _playerStateService;
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistService _playlistService;
        private string _currentLyrics = "暂无歌词";
        private List<LyricLine> _lyricsLines = new List<LyricLine>();
        private bool _isMouseOver = false;

        /// <summary>
        /// 是否鼠标悬浮
        /// </summary>
        public bool IsMouseOver
        {
            get => _isMouseOver;
            set
            {
                if (_isMouseOver != value)
                {
                    _isMouseOver = value;
                    OnPropertyChanged(nameof(IsMouseOver));
                }
            }
        }

        /// <summary>
        /// 当前显示的歌词
        /// </summary>
        public string CurrentLyrics
        {
            get => _currentLyrics;
            private set
            {
                if (_currentLyrics != value)
                {
                    _currentLyrics = value;
                    OnPropertyChanged(nameof(CurrentLyrics));
                }
            }
        }

        private LyricLine _currentLyricLine;

        /// <summary>
        /// 当前显示的歌词行对象
        /// </summary>
        public LyricLine CurrentLyricLine
        {
            get => _currentLyricLine;
            private set
            {
                if (_currentLyricLine != value)
                {
                    _currentLyricLine = value;
                    OnPropertyChanged(nameof(CurrentLyricLine));
                    
                    // 更新CurrentLyrics显示
                    if (value != null)
                    {
                        // 组合中英文歌词，用换行符分隔
                        string combinedText = string.Empty;
                        if (!string.IsNullOrEmpty(value.TextCN))
                        {
                            combinedText = value.TextCN;
                        }
                        if (!string.IsNullOrEmpty(value.TextEN))
                        {
                            if (!string.IsNullOrEmpty(combinedText))
                            {
                                combinedText += "\n";
                            }
                            combinedText += value.TextEN;
                        }
                        CurrentLyrics = combinedText;
                    }
                    else
                    {
                        CurrentLyrics = "暂无歌词";
                    }
                }
            }
        }

        /// <summary>
        /// 关闭歌词窗口命令
        /// </summary>
        [RelayCommand]
        private void CloseWindow()
        {
            // 发送关闭歌词窗口的消息
            _messagingService.Send(new CloseLyricsWindowMessage());
        }

        public LyricsViewModel(IPlayerStateService playerStateService, IMessagingService messagingService, IPlaylistService playlistService)
        {
            _playerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));

            // 注册消息处理器
            RegisterMessageHandlers();
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 歌词更新消息
            _messagingService.Register<LyricsUpdatedMessage>(this, (recipient, message) =>
            {
                OnLyricsUpdated(message.Value);
            });

            // 播放进度变化消息
            _messagingService.Register<PlaybackProgressChangedMessage>(this, (recipient, message) =>
            {
                UpdateLyricsByTime(message.Value);
            });

            // 当前歌曲变化消息 - 清理旧歌词数据
            _messagingService.Register<CurrentSongChangedMessage>(this, (recipient, message) =>
            {
                OnCurrentSongChanged(message.Value);
            });
        }

        /// <summary>
        /// 处理歌曲切换消息
        /// </summary>
        /// <param name="song">新歌曲</param>
        private void OnCurrentSongChanged(Song? song)
        {
            try
            {
                // 清理旧歌词数据，释放内存
                _lyricsLines.Clear();
                _lyricsLines.TrimExcess(); // 释放多余容量

                if (song == null)
                {
                    CurrentLyrics = "暂无歌曲";
                }
                else
                {
                    CurrentLyrics = song.Title ?? "正在加载...";
                }

                System.Diagnostics.Debug.WriteLine($"OnCurrentSongChanged: 已清理旧歌词数据，新歌曲: {song?.Title ?? "无"}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理歌曲切换失败: {ex.Message}");
                CurrentLyrics = "加载失败";
                _lyricsLines.Clear();
            }
        }

        /// <summary>
        /// 处理歌词更新消息
        /// </summary>
        /// <param name="lyrics">歌词集合</param>
        private void OnLyricsUpdated(ObservableCollection<LyricLine> lyrics)
        {
            try
            {
                _lyricsLines.Clear();

                if (lyrics != null && lyrics.Count > 0)
                {
                    foreach (var lyric in lyrics)
                    {
                        _lyricsLines.Add(lyric);
                    }

                    // 显示第一句歌词
                    CurrentLyricLine = _lyricsLines[0];
                    _lyricsLines[0].Progress = 0;
                    
                    // 构建调试信息
                    string firstLineDebug = string.Empty;
                    if (!string.IsNullOrEmpty(_lyricsLines[0].TextCN))
                    {
                        firstLineDebug += _lyricsLines[0].TextCN;
                    }
                    if (!string.IsNullOrEmpty(_lyricsLines[0].TextEN))
                    {
                        if (!string.IsNullOrEmpty(firstLineDebug))
                        {
                            firstLineDebug += "\n";
                        }
                        firstLineDebug += _lyricsLines[0].TextEN;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"OnLyricsUpdated: 收到 {_lyricsLines.Count} 行歌词，第一句: '{firstLineDebug}'");
                }
                else
                {
                    CurrentLyrics = "暂无歌词";
                    CurrentLyricLine = null;
                    System.Diagnostics.Debug.WriteLine("OnLyricsUpdated: 没有歌词数据");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理歌词更新失败: {ex.Message}");
                CurrentLyrics = "加载歌词失败";
                CurrentLyricLine = null;
                _lyricsLines.Clear();
            }
        }



        /// <summary>
        /// 根据播放时间更新歌词
        /// </summary>
        /// <param name="position">播放位置（秒）</param>
        private void UpdateLyricsByTime(double position)
        {
            try
            {
                if (_lyricsLines.Count == 0)
                {
                    //System.Diagnostics.Debug.WriteLine($"UpdateLyricsByTime: 没有歌词行，位置={position}");
                    return;
                }

                // 将秒转换为TimeSpan
                var timeSpan = TimeSpan.FromSeconds(position);

                // 找到当前时间应该显示的歌词
                var currentLine = _lyricsLines.LastOrDefault(line => line.Time <= timeSpan);

                if (currentLine != null)
                {
                    // 更新当前歌词
                    if (currentLine != CurrentLyricLine)
                    {
                        CurrentLyricLine = currentLine;
                        
                        // 构建调试信息
                        string debugText = string.Empty;
                        if (!string.IsNullOrEmpty(currentLine.TextCN))
                        {
                            debugText += currentLine.TextCN;
                        }
                        if (!string.IsNullOrEmpty(currentLine.TextEN))
                        {
                            if (!string.IsNullOrEmpty(debugText))
                            {
                                debugText += "\n";
                            }
                            debugText += currentLine.TextEN;
                        }
                         }

                    // 计算逐字进度
                    int currentIndex = _lyricsLines.IndexOf(currentLine);
                    TimeSpan lineDuration;
                    
                    // 如果是最后一句歌词，进度固定为1
                    if (currentIndex == _lyricsLines.Count - 1)
                    {
                        lineDuration = TimeSpan.FromSeconds(5); // 最后一句持续5秒
                    }
                    else
                    {
                        // 计算当前歌词行的播放时长（下一句时间 - 当前句时间）
                        var nextLine = _lyricsLines[currentIndex + 1];
                        lineDuration = nextLine.Time - currentLine.Time;
                    }

                    // 计算当前进度在当前歌词行内的比例
                    TimeSpan elapsedInLine = timeSpan - currentLine.Time;
                    double progress = elapsedInLine.TotalSeconds / lineDuration.TotalSeconds;
                    
                    // 确保进度在0-1之间
                    progress = Math.Clamp(progress, 0.0, 1.0);
                    
                    // 更新当前歌词行的进度
                    if (Math.Abs(currentLine.Progress - progress) > 0.01) // 避免频繁更新
                    {
                        currentLine.Progress = progress;
                        
                        // 构建调试信息
                        string debugText = string.Empty;
                        if (!string.IsNullOrEmpty(currentLine.TextCN))
                        {
                            debugText += currentLine.TextCN;
                        }
                        if (!string.IsNullOrEmpty(currentLine.TextEN))
                        {
                            if (!string.IsNullOrEmpty(debugText))
                            {
                                debugText += "\n";
                            }
                            debugText += currentLine.TextEN;
                        }
                       }
                }
                else
                {
                     // 如果没有找到匹配的歌词，显示第一句
                    if (_lyricsLines.Count > 0)
                    {
                        if (_lyricsLines[0] != CurrentLyricLine)
                        {
                            CurrentLyricLine = _lyricsLines[0];
                            
                            // 构建调试信息
                            string debugText = string.Empty;
                            if (!string.IsNullOrEmpty(_lyricsLines[0].TextCN))
                            {
                                debugText += _lyricsLines[0].TextCN;
                            }
                            if (!string.IsNullOrEmpty(_lyricsLines[0].TextEN))
                            {
                                if (!string.IsNullOrEmpty(debugText))
                                {
                                    debugText += "\n";
                                }
                                debugText += _lyricsLines[0].TextEN;
                            }
                            
                               }
                        // 第一句歌词开始前，进度为0
                        _lyricsLines[0].Progress = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新歌词失败: {ex.Message}");
            }
        }



        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public override void Initialize()
        {
            // 获取当前播放状态
            LoadCurrentState();
        }

        /// <summary>
        /// 加载当前播放状态
        /// </summary>
        private void LoadCurrentState()
        {
            try
            {
                // 获取当前歌曲
                var currentSong = _playerStateService.CurrentSong;
                if (currentSong != null)
                {
                    // 从PlaylistService获取当前歌词
                    var lyrics = _playlistService.LoadLyrics(currentSong.FilePath);
                    if (lyrics != null && lyrics.Count > 0)
                    {
                        OnLyricsUpdated(new ObservableCollection<LyricLine>(lyrics));

                        // 获取当前播放位置
                        var currentPosition = _playerStateService.CurrentPosition;
                        if (currentPosition > 0)
                        {
                            UpdateLyricsByTime(currentPosition);
                        }
                    }
                    else
                    {
                        CurrentLyrics = currentSong.Title ?? "暂无歌词";
                        _lyricsLines.Clear();
                    }
                }
                else
                {
                    CurrentLyrics = "暂无歌曲";
                    _lyricsLines.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentState: 加载当前状态失败 - {ex.Message}");
                CurrentLyrics = "加载失败";
                _lyricsLines.Clear();
            }
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 取消注册消息
            _messagingService?.Unregister(this);
        }

    }
}
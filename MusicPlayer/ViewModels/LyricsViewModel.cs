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
        private Core.Models.LyricLine? _currentLyricLine;
        
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
        
        /// <summary>
        /// 当前歌词行
        /// </summary>
        public Core.Models.LyricLine? CurrentLyricLine
        {
            get => _currentLyricLine;
            private set
            {
                if (_currentLyricLine != value)
                {
                    _currentLyricLine = value;
                    OnPropertyChanged(nameof(CurrentLyricLine));
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
                    // 添加歌词并设置结束时间
                    for (int i = 0; i < lyrics.Count; i++)
                    {
                        var lyric = lyrics[i];
                        // 设置结束时间为下一行的开始时间，如果是最后一行则设置为10秒后
                        TimeSpan endTime = i < lyrics.Count - 1 ? lyrics[i + 1].Time : lyric.Time.Add(TimeSpan.FromSeconds(10));
                        lyric.EndTime = endTime;
                        lyric.CurrentHighlightedIndex = 0;
                        lyric.HighlightedText = lyric.Text;
                        _lyricsLines.Add(lyric);
                    }
                    
                    // 显示第一句歌词
                    CurrentLyrics = _lyricsLines[0].Text;
                    System.Diagnostics.Debug.WriteLine($"OnLyricsUpdated: 收到 {_lyricsLines.Count} 行歌词，第一句: '{_lyricsLines[0].Text}'");
                }
                else
                {
                    CurrentLyrics = "暂无歌词";
                    System.Diagnostics.Debug.WriteLine("OnLyricsUpdated: 没有歌词数据");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理歌词更新失败: {ex.Message}");
                CurrentLyrics = "加载歌词失败";
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
                    return;
                }

                // 将秒转换为TimeSpan
                var timeSpan = TimeSpan.FromSeconds(position);
                
                // 找到当前时间应该显示的歌词
                var currentLine = _lyricsLines.LastOrDefault(line => line.Time <= timeSpan);
                
                if (currentLine != null)
                {
                    // 更新当前歌词
                    if (currentLine.Text != CurrentLyrics)
                    {
                        // 切换到新的歌词行时，重置高亮索引
                        foreach (var line in _lyricsLines)
                        {
                            if (line != currentLine)
                            {
                                line.CurrentHighlightedIndex = 0;
                                line.TargetHighlightedIndex = 0;
                            }
                        }
                        CurrentLyrics = currentLine.Text;
                        System.Diagnostics.Debug.WriteLine($"UpdateLyricsByTime: 更新歌词 '{currentLine.Text}' 位置={position} 歌词时间={currentLine.Time.TotalSeconds}");
                    }
                    
                    // 设置当前歌词行，用于逐字高亮
                    CurrentLyricLine = currentLine;
                    
                    // 计算当前歌词行的总时长
                    TimeSpan lineDuration = currentLine.EndTime - currentLine.Time;
                    double lineDurationSeconds = lineDuration.TotalSeconds;
                    
                    // 如果时长小于0.5秒，不进行逐字高亮
                    if (lineDurationSeconds < 0.5)
                    {
                        currentLine.UpdateHighlightSmoothly(currentLine.Text.Length);
                        currentLine.HighlightedText = currentLine.Text;
                        return;
                    }
                    
                    // 计算当前已经播放的时间在该行中的比例
                    TimeSpan elapsedInLine = timeSpan - currentLine.Time;
                    double elapsedSecondsInLine = elapsedInLine.TotalSeconds;
                    double progress = Math.Min(1.0, elapsedSecondsInLine / lineDurationSeconds);
                    
                    // 计算应该高亮的字数
                    int totalChars = currentLine.Text.Length;
                    int highlightedChars = (int)Math.Round(totalChars * progress);
                    highlightedChars = Math.Max(0, Math.Min(totalChars, highlightedChars));
                    
                    // 使用平滑过渡方法更新高亮索引
                    currentLine.UpdateHighlightSmoothly(highlightedChars);
                    currentLine.HighlightedText = currentLine.Text;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateLyricsByTime: 没有找到匹配的歌词行，位置={position}");
                    // 如果没有找到匹配的歌词，显示第一句
                    if (_lyricsLines.Count > 0 && CurrentLyrics != _lyricsLines[0].Text)
                    {
                        CurrentLyrics = _lyricsLines[0].Text;
                        System.Diagnostics.Debug.WriteLine($"UpdateLyricsByTime: 显示第一句歌词 '{_lyricsLines[0].Text}'");
                    }
                    CurrentLyricLine = null;
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
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage.Streams;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 系统媒体传输控制服务实现
    /// 基于 MediaPlayer 桥接 WinRT API，提供 Windows 11 快捷控制面板集成
    /// </summary>
    public class SystemMediaTransportService : ISystemMediaTransportService
    {
        private readonly Windows.Media.Playback.MediaPlayer _mediaPlayer;
        private readonly SystemMediaTransportControls _smtc;
        private readonly SystemMediaTransportControlsDisplayUpdater _displayUpdater;
        private bool _isInitialized = false;

        // 事件
        public event EventHandler? PlayOrPauseRequested;
        public event EventHandler? NextRequested;
        public event EventHandler? PreviousRequested;

        public SystemMediaTransportService()
        {
            // 创建 MediaPlayer 作为 SMTC 桥接
            _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            
            // 禁用 MediaPlayer 的内置命令管理器，避免冲突
            _mediaPlayer.CommandManager.IsEnabled = false;
            
            // 获取 SMTC 实例
            _smtc = _mediaPlayer.SystemMediaTransportControls;
            _displayUpdater = _smtc.DisplayUpdater;
            
            // 设置媒体类型为音乐
            _displayUpdater.Type = MediaPlaybackType.Music;
            _displayUpdater.AppMediaId = "MusicPlayer";
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 启用 SMTC
                _smtc.IsEnabled = true;
                
                // 启用控制按钮
                _smtc.IsPlayEnabled = true;
                _smtc.IsPauseEnabled = true;
                _smtc.IsNextEnabled = true;
                _smtc.IsPreviousEnabled = true;
                
                // 注册按钮事件
                _smtc.ButtonPressed += OnButtonPressed;
                
                // 多次尝试设置应用程序信息
                try
                {
                    // 方法1：设置完整的媒体信息
                    _displayUpdater.Type = MediaPlaybackType.Music;
                    _displayUpdater.AppMediaId = "MusicPlayer";
                    _displayUpdater.MusicProperties.Title = "MusicPlayer";
                    _displayUpdater.MusicProperties.Artist = "";
                    _displayUpdater.MusicProperties.AlbumTitle = "";
                    _displayUpdater.Update();
                    
                    System.Diagnostics.Debug.WriteLine("SMTC 初始化方法1完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SMTC 初始化方法1失败: {ex.Message}");
                }
            });

            _isInitialized = true;
        }

        public async Task UpdateMediaInfoAsync(Song? song)
        {
            if (!_isInitialized) await InitializeAsync();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (song == null)
                {
                    // 清空媒体信息
                    _displayUpdater.MusicProperties.Title = "";
                    _displayUpdater.MusicProperties.Artist = "";
                    _displayUpdater.MusicProperties.AlbumTitle = "";
                    _displayUpdater.Thumbnail = null;
                }
                else
                {
                    // 验证歌曲信息的有效性
                    if (string.IsNullOrWhiteSpace(song.Title) && string.IsNullOrWhiteSpace(song.Artist))
                    {
                        return;
                    }
                    
                    // 设置歌曲信息
                    _displayUpdater.MusicProperties.Title = song.Title ?? "未知歌曲";
                    _displayUpdater.MusicProperties.Artist = song.Artist ?? "未知艺术家";
                    _displayUpdater.MusicProperties.AlbumTitle = song.Album ?? "";

                    // 设置专辑封面
                    try
                    {
                        if (song.AlbumArt != null)
                        {
                            var thumbnail = await CreateThumbnailFromBitmapImageAsync(song.AlbumArt);
                            _displayUpdater.Thumbnail = thumbnail;
                        }
                        else
                        {
                            _displayUpdater.Thumbnail = null;
                        }
                    }
                    catch
                    {
                        // 如果设置封面失败，使用默认封面
                        _displayUpdater.Thumbnail = null;
                    }
                }

                // 更新显示
                _displayUpdater.Update();
            });
        }

        public async Task UpdatePlaybackStatusAsync(bool isPlaying)
        {
            if (!_isInitialized) await InitializeAsync();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _smtc.PlaybackStatus = isPlaying ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
            });
        }

        public void EnableControls(bool isPlayEnabled, bool isPauseEnabled, bool isNextEnabled, bool isPreviousEnabled)
        {
            if (!_isInitialized) return;

            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _smtc.IsPlayEnabled = isPlayEnabled;
                _smtc.IsPauseEnabled = isPauseEnabled;
                _smtc.IsNextEnabled = isNextEnabled;
                _smtc.IsPreviousEnabled = isPreviousEnabled;
            });
        }

        private async Task<RandomAccessStreamReference?> CreateThumbnailFromBitmapImageAsync(System.Windows.Media.Imaging.BitmapImage bitmapImage)
        {
            // 有效性检查
            if (bitmapImage == null)
            {
                return null;
            }
            
            if (bitmapImage.PixelWidth <= 0 || bitmapImage.PixelHeight <= 0)
            {
                return null;
            }
            
            try
            {
                // 如果 BitmapImage 有 UriSource，直接从文件创建
                if (bitmapImage.UriSource != null)
                {
                    var filePath = bitmapImage.UriSource.LocalPath;
                    
                    if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                            return RandomAccessStreamReference.CreateFromFile(file);
                        }
                        catch
                        {
                            // 文件访问失败，继续尝试其他方式
                        }
                    }
                }
                
                // 检查 BitmapImage 是否可以访问
                if (!bitmapImage.IsFrozen && bitmapImage.CanFreeze && !bitmapImage.IsFrozen)
                {
                    try
                    {
                        bitmapImage.Freeze();
                    }
                    catch
                    {
                        // 冻结失败，继续处理
                    }
                }
                
                // 否则将 BitmapImage 转换为字节数组
                byte[]? imageData = await ConvertBitmapImageToBytesAsync(bitmapImage);
                if (imageData != null && imageData.Length > 0)
                {
                    // 创建 InMemoryRandomAccessStream
                    var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    using (var writer = new Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(imageData);
                        await writer.StoreAsync();
                    }
                    stream.Seek(0);
                    
                    return RandomAccessStreamReference.CreateFromStream(stream);
                }
            }
            catch
            {
                // 所有尝试都失败，返回 null
            }
            
            return null;
        }

        private async Task<byte[]?> ConvertBitmapImageToBytesAsync(System.Windows.Media.Imaging.BitmapImage bitmapImage)
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 再次检查有效性
                    if (bitmapImage == null)
                    {
                        return null;
                    }
                    
                    if (bitmapImage.PixelWidth <= 0 || bitmapImage.PixelHeight <= 0)
                    {
                        return null;
                    }
                    
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    
                    // 使用更安全的方式创建 BitmapFrame
                    System.Windows.Media.Imaging.BitmapFrame frame;
                    if (bitmapImage.IsFrozen)
                    {
                        frame = System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage);
                    }
                    else
                    {
                        // 如果未冻结，创建副本
                        try
                        {
                            frame = System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    
                    encoder.Frames.Add(frame);
                    
                    using (var stream = new System.IO.MemoryStream())
                    {
                        encoder.Save(stream);
                        var result = stream.ToArray();
                        
                        if (result.Length > 0)
                        {
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch
                {
                    return null;
                }
            });
        }

        private void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            // 使用异步调用避免死锁
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                    case SystemMediaTransportControlsButton.Pause:
                        PlayOrPauseRequested?.Invoke(this, EventArgs.Empty);
                        break;
                        
                    case SystemMediaTransportControlsButton.Next:
                        NextRequested?.Invoke(this, EventArgs.Empty);
                        break;
                        
                    case SystemMediaTransportControlsButton.Previous:
                        PreviousRequested?.Invoke(this, EventArgs.Empty);
                        break;
                }
            });
        }

        public void Dispose()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_smtc != null)
                {
                    _smtc.IsEnabled = false;
                    _smtc.ButtonPressed -= OnButtonPressed;
                }
                
                _mediaPlayer?.Dispose();
            });
        }
    }
}
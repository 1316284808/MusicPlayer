using MusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 播放列表专辑封面加载行为
    /// </summary>
    public static class PlaylistAlbumArtBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistAlbumArtBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty PlaylistViewModelProperty =
            DependencyProperty.RegisterAttached(
                "PlaylistViewModel",
                typeof(IPlaylistViewModel),
                typeof(PlaylistAlbumArtBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static IPlaylistViewModel GetPlaylistViewModel(DependencyObject obj)
        {
            return (IPlaylistViewModel)obj.GetValue(PlaylistViewModelProperty);
        }

        public static void SetPlaylistViewModel(DependencyObject obj, IPlaylistViewModel value)
        {
            obj.SetValue(PlaylistViewModelProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl userControl)
            {
                if ((bool)e.NewValue)
                {
                    Enable(userControl);
                }
                else
                {
                    Disable(userControl);
                }
            }
        }

        private static void Enable(UserControl userControl)
        {
            try
            {
                var viewModel = GetPlaylistViewModel(userControl);
                if (viewModel != null)
                {
                    // 订阅专辑封面加载请求事件
                    viewModel.AlbumLoadRequested += OnAlbumLoadRequested;
                    System.Diagnostics.Debug.WriteLine("PlaylistAlbumArtBehavior: 专辑封面加载行为已启用");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistAlbumArtBehavior: PlaylistViewModel 未设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 启用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void Disable(UserControl userControl)
        {
            try
            {
                var viewModel = GetPlaylistViewModel(userControl);
                if (viewModel != null)
                {
                    // 取消订阅事件
                    viewModel.AlbumLoadRequested -= OnAlbumLoadRequested;
                    System.Diagnostics.Debug.WriteLine("PlaylistAlbumArtBehavior: 专辑封面加载行为已禁用");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 禁用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void OnAlbumLoadRequested(object? sender, AlbumLoadRequestEventArgs e)
        {
            try
            {
                var song = e.Song;
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 收到专辑封面加载请求，歌曲: {song.Title}");

                // 这里应该实际处理专辑封面加载逻辑
                // 例如：调用专辑封面加载服务或更新UI
                // 而不是重新触发事件

                // 检查专辑封面是否已加载
                if (song.AlbumArt != null)
                {
                    // 专辑封面已存在，不需要处理
                    System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 专辑封面已加载");
                }
                else if (song.AlbumArtData == null || song.AlbumArtData.Length == 0)
                {
                    // 有专辑封面数据但未加载，强制加载
                    System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 有专辑封面数据，开始加载");
                    song.EnsureAlbumArtLoaded();
                }
                else
                {
                    // 需要加载专辑封面的逻辑
                    System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 需要加载歌曲 {song.Title} 的专辑封面");
                    // 这里可以调用专辑封面加载服务
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 处理专辑封面加载请求失败: {ex.Message}");
            }
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 播放列表滚动到当前歌曲行为类
    /// 处理滚动到当前播放歌曲的功能
    /// </summary>
    public static class PlaylistScrollToCurrentSongBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistScrollToCurrentSongBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    // 注册消息处理器
                    WeakReferenceMessenger.Default.Register<ScrollToCurrentSongMessage>(listBox, (recipient, message) =>
                    {
                        ScrollToCurrentSong(listBox, message.CurrentSong);
                    });
                }
                else
                {
                    // 取消注册消息处理器
                    WeakReferenceMessenger.Default.Unregister<ScrollToCurrentSongMessage>(listBox);
                }
            }
        }

        /// <summary>
        /// 滚动到指定歌曲
        /// </summary>
        /// <param name="listBox">播放列表ListBox</param>
        /// <param name="song">要滚动到的歌曲</param>
        private static void ScrollToCurrentSong(ListBox listBox, Core.Models.Song song)
        {
            if (song == null)
            {
                System.Diagnostics.Debug.WriteLine("PlaylistScrollToCurrentSongBehavior: ScrollToCurrentSong - 歌曲为null");
                return;
            }

            // 获取歌曲在播放列表中的索引
            int index = listBox.Items.IndexOf(song);

            if (index >= 0)
            {
                // 滚动到歌曲并选中
                listBox.ScrollIntoView(song);
                listBox.SelectedItem = song;

                // 获取ListBoxItem容器并确保完全可见
                var listBoxItem = listBox.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                
                if (listBoxItem != null)
                {
                    // 确保ListBoxItem完全可见
                    listBoxItem.BringIntoView();
                }

                System.Diagnostics.Debug.WriteLine($"PlaylistScrollToCurrentSongBehavior: 已滚动到歌曲: {song.Title}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistScrollToCurrentSongBehavior: 歌曲不在播放列表中: {song.Title}");
            }
        }
    }
}
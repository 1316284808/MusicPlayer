using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MusicPlayer.Services.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// PlaylistDetailControl.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistDetailControl : UserControl, IDisposable
    {
        private bool _disposed;

        public PlaylistDetailControl()
        {
            InitializeComponent();

            // 注册消息处理器，处理搜索框焦点请求
            WeakReferenceMessenger.Default.Register<SearchBoxFocusRequestMessage>(this, (recipient, message) =>
            {
                // 处理搜索框焦点请求
                SearchTextBox.Focus();
            });
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消消息订阅
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    
                    // 清理附加行为
                    CleanupAttachedBehaviors();
                    
                    // 清空DataContext，解除Page对ViewModel的强引用
                    this.DataContext = null;
                }
                _disposed = true;
            }
        }

        private void CleanupAttachedBehaviors()
        {
            // 清理PlaylistListBox的附加行为
            if (PlaylistListBox != null)
            {
                Helper.PlaylistScrollBehavior.SetIsEnabled(PlaylistListBox, false);
                Helper.NewPlaylistAlbumArtBehavior.SetIsEnabled(PlaylistListBox, false);
                Helper.NewPlaylistAlbumArtBehavior.SetViewModel(PlaylistListBox, null);
                Helper.PlaylistInteractionBehavior.SetIsEnabled(PlaylistListBox, false);
                Helper.PlaylistScrollToCurrentSongBehavior.SetIsEnabled(PlaylistListBox, false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

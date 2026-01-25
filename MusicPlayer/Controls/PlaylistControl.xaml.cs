using System.Windows.Controls;
using MusicPlayer.ViewModels;
using MusicPlayer.Services.Messages;
using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Helper;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 播放列表控件
    /// 负责显示播放列表和相关的用户界面元素
    /// 遵循MVVM架构原则，不包含业务逻辑
    /// 现在使用PlaylistInteractionBehavior处理交互逻辑
    /// </summary>
    public partial class PlaylistControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public PlaylistControl()
        {
            InitializeComponent();

            // 注册消息处理器
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
                    
                    // 清空DataContext，解除对ViewModel的强引用
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
                PlaylistScrollBehavior.SetIsEnabled(PlaylistListBox, false);
                NewPlaylistAlbumArtBehavior.SetIsEnabled(PlaylistListBox, false);
                NewPlaylistAlbumArtBehavior.SetViewModel(PlaylistListBox, null);
                PlaylistInteractionBehavior.SetIsEnabled(PlaylistListBox, false);
                PlaylistScrollToCurrentSongBehavior.SetIsEnabled(PlaylistListBox, false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
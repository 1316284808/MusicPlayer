using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 中心内容控件
    /// 负责显示专辑封面、圆形频谱和歌词
    /// </summary>
    public partial class CenterContentControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public CenterContentControl()
        {
            InitializeComponent();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    CircularSpectrum.Dispose(); 
                    this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
                    this.Content = null;     // 清空页面内容，释放UI资源
                    _disposed = true;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
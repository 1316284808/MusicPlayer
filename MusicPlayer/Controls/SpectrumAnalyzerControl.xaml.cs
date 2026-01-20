using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 频谱分析器控件 - 负责显示音频频谱可视化
    /// 暂时弃用，没有做圆形频谱和线性频谱的打算
    /// </summary>
    public partial class SpectrumAnalyzerControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public SpectrumAnalyzerControl()
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
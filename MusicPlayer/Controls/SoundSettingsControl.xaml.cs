using System.Windows.Controls;
using MusicPlayer.Services.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// SoundSettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class SoundSettingsControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public SoundSettingsControl()
        {
            InitializeComponent();
            
            // 注册消息处理器
            WeakReferenceMessenger.Default.Register<SavePresetFocusRequestMessage>(this, (recipient, message) =>
            {
                // 处理保存预设文本框焦点请求
                SavePresetTextBox.Focus();
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
                    this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
                    this.Content = null;     // 清空页面内容，释放UI资源
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
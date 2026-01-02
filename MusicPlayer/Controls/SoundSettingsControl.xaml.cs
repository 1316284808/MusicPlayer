using System.Windows.Controls;
using MusicPlayer.Services.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// SoundSettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class SoundSettingsControl : UserControl
    {
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
    }
}
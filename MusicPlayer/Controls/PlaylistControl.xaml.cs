using System.Windows.Controls;
using MusicPlayer.ViewModels;
using MusicPlayer.Services.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 播放列表控件
    /// 负责显示播放列表和相关的用户界面元素
    /// 遵循MVVM架构原则，不包含业务逻辑
    /// 现在使用PlaylistInteractionBehavior处理交互逻辑
    /// </summary>
    public partial class PlaylistControl : UserControl
    {
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
    }
}
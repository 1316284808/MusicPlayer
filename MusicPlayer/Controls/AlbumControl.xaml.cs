using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Helper;
using MusicPlayer.Services.Messages;
using MusicPlayer.ViewModels;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// AlbumControl.xaml 的交互逻辑
    /// </summary>
    public partial class AlbumControl : UserControl
    {
        public AlbumControl()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.Register<SearchBoxFocusRequestMessage>(this, (recipient, message) =>
            {
                // 处理搜索框焦点请求
                SearchTextBox.Focus();
            });
        }
    }
}
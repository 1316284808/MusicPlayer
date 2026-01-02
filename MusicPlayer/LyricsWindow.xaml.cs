using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MusicPlayer
{
    /// <summary>
    /// 桌面歌词窗口
    /// </summary>
    public partial class LyricsWindow : System.Windows.Window
    {
        private readonly ILyricsViewModel _lyricsViewModel;
        private readonly IMessagingService _messagingService;

        public LyricsWindow(ILyricsViewModel lyricsViewModel, IMessagingService messagingService)
        {
            InitializeComponent();
            this.Topmost = true;
            this.Activate(); // 尝试激活并聚焦窗口
            _lyricsViewModel = lyricsViewModel ?? throw new ArgumentNullException(nameof(lyricsViewModel));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            
            DataContext = _lyricsViewModel;
            
            // 初始化ViewModel，注册消息订阅
            _lyricsViewModel.Initialize();
            
            // 注册关闭窗口消息处理
            _messagingService.Register<Services.Messages.CloseLyricsWindowMessage>(this, (recipient, message) =>
            {
                this.Close();
            });
        }





        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 清理ViewModel资源，取消消息订阅
            _lyricsViewModel.Cleanup();
        }
    }
}
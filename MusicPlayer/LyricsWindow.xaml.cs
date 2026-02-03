using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services;
using MusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

// 解决TextBlock命名冲突
using TextBlock = System.Windows.Controls.TextBlock;

namespace MusicPlayer
{
    /// <summary>
    /// 桌面歌词窗口
    /// </summary>
    public partial class LyricsWindow : System.Windows.Window
    {
        private readonly ILyricsViewModel _lyricsViewModel;
        private readonly IMessagingService _messagingService;

        // 配置项：一键修改，无需改逻辑
        private readonly int _fontSize = 28;                                 // 字体大小
        private readonly SolidColorBrush _blackBrush = Brushes.Black;          // 底层黑色
        private readonly SolidColorBrush _blueBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));    // 顶层蓝色
        private readonly int _charSpacing = 0;                                 // 字符间距，防止粘连

        // 歌词显示控件
        //private Helper.HighlightTextBlock tbLyricEN = null!;
        //private Helper.HighlightTextBlock tbLyricCN = null!;

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
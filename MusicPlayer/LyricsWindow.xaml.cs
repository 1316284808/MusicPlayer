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

        // 当前显示的歌词文本，用于比较是否需要更新
        private string _currentDisplayTextCN = string.Empty;
        private string _currentDisplayTextNonCN = string.Empty;

        // 当前歌词行对象，用于取消之前的事件订阅
        private LyricLine _previousLyricLine;

        public LyricsWindow(ILyricsViewModel lyricsViewModel, IMessagingService messagingService)
        {
            InitializeComponent();
            this.Topmost = true;
            this.Activate(); // 尝试激活并聚焦窗口
            _lyricsViewModel = lyricsViewModel ?? throw new ArgumentNullException(nameof(lyricsViewModel));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));

            DataContext = _lyricsViewModel;

            // 获取歌词控件引用（在InitializeComponent之后）
            //tbLyricEN = (Helper.HighlightTextBlock)FindName("tbLyricEN");
            //tbLyricCN = (Helper.HighlightTextBlock)FindName("tbLyricCN");

            // 初始化ViewModel，注册消息订阅
            _lyricsViewModel.Initialize();

            // 注册关闭窗口消息处理
            _messagingService.Register<Services.Messages.CloseLyricsWindowMessage>(this, (recipient, message) =>
            {
                this.Close();
            });

            // 监听ViewModel的PropertyChanged事件，更新歌词显示
            ((INotifyPropertyChanged)_lyricsViewModel).PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 监听ViewModel属性变化
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILyricsViewModel.CurrentLyricLine))
            {
                // 取消之前歌词行的事件订阅
                if (_previousLyricLine != null)
                {
                    ((INotifyPropertyChanged)_previousLyricLine).PropertyChanged -= OnLyricLinePropertyChanged;
                }

                // 当前歌词行变化，更新字符面板
                UpdateLyricChars();

                // 订阅新歌词行的PropertyChanged事件
                _previousLyricLine = _lyricsViewModel.CurrentLyricLine;
                if (_previousLyricLine != null)
                {
                    ((INotifyPropertyChanged)_previousLyricLine).PropertyChanged += OnLyricLinePropertyChanged;
                }
            }
        }

        /// <summary>
        /// 监听LyricLine属性变化
        /// </summary>
        private void OnLyricLinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LyricLine.Progress))
            {
                // 当前歌词行的进度变化，更新字符填充效果
                UpdateLyricProgress();
            }
        }





        /// <summary>
        /// 更新歌词显示
        /// </summary>
        private void UpdateLyricChars()
        {
            string newTextOriginal = " ";
            string newTextTranslated = " ";

            if (_lyricsViewModel.CurrentLyricLine != null)
            {
                // 获取原文歌词
                if (!string.IsNullOrEmpty(_lyricsViewModel.CurrentLyricLine.OriginalText))
                {
                    newTextOriginal = _lyricsViewModel.CurrentLyricLine.OriginalText;
                }

                // 获取翻译歌词
                if (!string.IsNullOrEmpty(_lyricsViewModel.CurrentLyricLine.TranslatedText))
                {
                    newTextTranslated = _lyricsViewModel.CurrentLyricLine.TranslatedText;
                }
            }

            // 更新原文歌词
            if (newTextOriginal != _currentDisplayTextCN)
            {
                tbLyricCN.Text = newTextOriginal;
                _currentDisplayTextCN = newTextOriginal;
            }

            // 更新翻译歌词
            if (newTextTranslated != _currentDisplayTextNonCN)
            {
                tbLyricEN.Text = newTextTranslated;
                _currentDisplayTextNonCN = newTextTranslated;
            }

            // 立即应用当前进度
            UpdateLyricProgress();
        }

        /// <summary>
        /// 更新歌词进度，驱动逐字填充效果
        /// </summary>
        private void UpdateLyricProgress()
        {
            if (_lyricsViewModel.CurrentLyricLine == null)
            {
                return;
            }

            // 当前全局进度 0~1
            double totalProgress = Math.Clamp(_lyricsViewModel.CurrentLyricLine.Progress, 0d, 1d);

            // 设置英文歌词高光位置（0=无光，1=完全高亮）
            tbLyricEN.HighlightPos = totalProgress;

            // 设置中文歌词高光位置
            tbLyricCN.HighlightPos = totalProgress;
        }

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 取消当前歌词行的事件订阅
            if (_previousLyricLine != null)
            {
                ((INotifyPropertyChanged)_previousLyricLine).PropertyChanged -= OnLyricLinePropertyChanged;
            }

            // 清理ViewModel的事件订阅
            ((INotifyPropertyChanged)_lyricsViewModel).PropertyChanged -= OnViewModelPropertyChanged;

            // 清理ViewModel资源，取消消息订阅
            _lyricsViewModel.Cleanup();
        }
    }
}
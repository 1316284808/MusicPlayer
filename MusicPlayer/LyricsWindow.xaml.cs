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

        // 存储顶层所有字符控件，顺序=填充顺序（从上到下、从左到右）
        private List<TextBlock> _charListEN = new List<TextBlock>(); // 英文歌词字符列表
        private List<TextBlock> _charListCN = new List<TextBlock>(); // 中文歌词字符列表

        // 当前显示的歌词文本，用于比较是否需要更新字符面板
        private string _currentDisplayTextCN = string.Empty;
        private string _currentDisplayTextEN = string.Empty;

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
            
            // 初始化ViewModel，注册消息订阅
            _lyricsViewModel.Initialize();
            
            // 注册关闭窗口消息处理
            _messagingService.Register<Services.Messages.CloseLyricsWindowMessage>(this, (recipient, message) =>
            {
                this.Close();
            });

            // 监听ViewModel的PropertyChanged事件，更新歌词显示
            ((INotifyPropertyChanged)_lyricsViewModel).PropertyChanged += OnViewModelPropertyChanged;

            // 初始化双层字符面板
            InitLyricCharsCN("");
            InitLyricCharsEN("");
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
        /// 初始化中文双层字符面板
        /// </summary>
        private void InitLyricCharsCN(string lyricText)
        {
           
            // 清空现有字符
            wpBottoms.Children.Clear();
            wpTops.Children.Clear();
            _charListCN.Clear();
            if (lyricText.Length <= 0) return;
            // 将文本按换行符分割为行
            string[] lines = lyricText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 遍历每行文本
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                // 遍历行中的每个字符
                foreach (char c in line)
                {
                    // 1. 创建底层黑色字符
                    var blackTb = CreateCharTextBlock(c.ToString(), _blackBrush);
                    RenderOptions.SetClearTypeHint(blackTb, ClearTypeHint.Enabled);
                    wpBottoms.Children.Add(blackTb);

                    // 2. 创建顶层蓝色字符
                    var blueTb = CreateCharTextBlock(c.ToString(), _blueBrush);
                    RenderOptions.SetClearTypeHint(blueTb, ClearTypeHint.Enabled);
                    wpTops.Children.Add(blueTb);
                    _charListCN.Add(blueTb);

                    // 3. 初始化顶层字符的裁剪区域：宽度0 → 完全隐藏蓝色，只显示底层黑色
                    blueTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
                }

                // 如果不是最后一行，添加换行符
                if (lineIndex < lines.Length - 1)
                {
                    // 1. 为底层添加换行符
                    var blackNewLine = new TextBlock
                    {
                        Text = "\n",
                        FontSize = _fontSize,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true,
                        TextAlignment = TextAlignment.Center
                    };
                    wpBottoms.Children.Add(blackNewLine);

                    // 2. 为顶层添加换行符
                    var blueNewLine = new TextBlock
                    {
                        Text = "\n",
                        FontSize = _fontSize,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true,
                        TextAlignment = TextAlignment.Center
                    };
                    wpTops.Children.Add(blueNewLine);
                    _charListCN.Add(blueNewLine);

                    // 3. 初始化换行符的裁剪区域（换行符不需要裁剪效果）
                    blueNewLine.Clip = new RectangleGeometry { Rect = new Rect(0, 0, double.MaxValue, double.MaxValue) };
                }
            }

            // 更新当前显示文本
            _currentDisplayTextCN = lyricText;
        }

        /// <summary>
        /// 初始化英文双层字符面板
        /// </summary>
        private void InitLyricCharsEN(string lyricText)
        { 
            // 清空现有字符
            wpBottom.Children.Clear();
            wpTop.Children.Clear();
            _charListEN.Clear();
            if (lyricText.Length <= 0) return;
            // 将文本按换行符分割为行
            string[] lines = lyricText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 遍历每行文本
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                // 遍历行中的每个字符
                foreach (char c in line)
                {
                    // 1. 创建底层黑色字符
                    var blackTb = CreateCharTextBlock(c.ToString(), _blackBrush);
                    RenderOptions.SetClearTypeHint(blackTb, ClearTypeHint.Enabled);
                    wpBottom.Children.Add(blackTb);

                    // 2. 创建顶层蓝色字符
                    var blueTb = CreateCharTextBlock(c.ToString(), _blueBrush);
                    RenderOptions.SetClearTypeHint(blueTb, ClearTypeHint.Enabled);
                    wpTop.HorizontalAlignment = HorizontalAlignment.Center;
                    wpTop.Children.Add(blueTb);
                    _charListEN.Add(blueTb);

                    // 3. 初始化顶层字符的裁剪区域：宽度0 → 完全隐藏蓝色，只显示底层黑色
                    blueTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
                }

                // 如果不是最后一行，添加换行符
                if (lineIndex < lines.Length - 1)
                {
                    // 1. 为底层添加换行符
                    var blackNewLine = new TextBlock
                    {
                        Text = "\n",
                        FontSize = _fontSize,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true,
                        TextAlignment = TextAlignment.Center
                    };
                    wpBottom.Children.Add(blackNewLine);

                    // 2. 为顶层添加换行符
                    var blueNewLine = new TextBlock
                    {
                        Text = "\n",
                        FontSize = _fontSize,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true,
                        TextAlignment = TextAlignment.Center
                    };
                    wpTop.Children.Add(blueNewLine);
                    _charListEN.Add(blueNewLine);

                    // 3. 初始化换行符的裁剪区域（换行符不需要裁剪效果）
                    blueNewLine.Clip = new RectangleGeometry { Rect = new Rect(0, 0, double.MaxValue, double.MaxValue) };
                }
            }

            // 更新当前显示文本
            _currentDisplayTextEN = lyricText;
        }

        /// <summary>
        /// 创建单个字符的TextBlock，统一样式
        /// </summary>
        private TextBlock CreateCharTextBlock(string text, Brush brush)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = _fontSize,
                FontWeight = FontWeights.SemiBold, 
                Foreground = brush,
                Margin = new Thickness(0, 0, _charSpacing, 0),
                SnapsToDevicePixels = true,  // 强制像素对齐，杜绝模糊
                UseLayoutRounding = true,    // 布局坐标取整，消除毛边
            };
        }

        /// <summary>
        /// 更新歌词字符面板
        /// </summary>
        private void UpdateLyricChars()
        {
            string newTextCN = " ";
            string newTextEN = " ";
            
            if (_lyricsViewModel.CurrentLyricLine != null)
            {
                // 获取中文歌词
                if (!string.IsNullOrEmpty(_lyricsViewModel.CurrentLyricLine.TextCN))
                {
                    newTextCN = _lyricsViewModel.CurrentLyricLine.TextCN;
                }
                
                // 获取英文歌词
                if (!string.IsNullOrEmpty(_lyricsViewModel.CurrentLyricLine.TextEN))
                {
                    newTextEN = _lyricsViewModel.CurrentLyricLine.TextEN;
                }
            }

            // 更新中文歌词面板
            if (newTextCN != _currentDisplayTextCN)
            {
                InitLyricCharsCN(newTextCN);
            }

            // 更新英文歌词面板
            if (newTextEN != _currentDisplayTextEN)
            {
                InitLyricCharsEN(newTextEN);
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
            
            // 更新中文歌词高亮
            if (_charListCN.Count > 0)
            {
                // 中文歌词总字符数
                int totalCharCountCN = _charListCN.Count;
                // 每个字符占用的进度占比
                double perCharProgressCN = 1.0 / totalCharCountCN;

                // 遍历所有中文字符，按顺序填充
                for (int i = 0; i < totalCharCountCN; i++)
                {
                    var charTb = _charListCN[i];
                    // 计算当前字符的【填充进度】：0=完全黑，1=完全蓝
                    double currentCharFillProgress = (totalProgress / perCharProgressCN) - i;
                    currentCharFillProgress = Math.Clamp(currentCharFillProgress, 0d, 1d);

                    // 获取字符真实宽高（解决布局加载时宽高为0的问题）
                    charTb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double charWidth = charTb.DesiredSize.Width;
                    double charHeight = charTb.DesiredSize.Height;

                    // 单个字符内部：从左到右纯蓝覆盖纯黑，无渐变
                    (charTb.Clip as RectangleGeometry).Rect = new Rect(0, 0, charWidth * currentCharFillProgress, charHeight);
                }
            }

            // 更新英文歌词高亮
            if (_charListEN.Count > 0)
            {
                // 英文歌词总字符数
                int totalCharCountEN = _charListEN.Count;
                // 每个字符占用的进度占比
                double perCharProgressEN = 1.0 / totalCharCountEN;

                // 遍历所有英文字符，按顺序填充
                for (int i = 0; i < totalCharCountEN; i++)
                {
                    var charTb = _charListEN[i];
                    // 计算当前字符的【填充进度】：0=完全黑，1=完全蓝
                    double currentCharFillProgress = (totalProgress / perCharProgressEN) - i;
                    currentCharFillProgress = Math.Clamp(currentCharFillProgress, 0d, 1d);

                    // 获取字符真实宽高（解决布局加载时宽高为0的问题）
                    charTb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double charWidth = charTb.DesiredSize.Width;
                    double charHeight = charTb.DesiredSize.Height;

                    // 单个字符内部：从左到右纯蓝覆盖纯黑，无渐变
                    (charTb.Clip as RectangleGeometry).Rect = new Rect(0, 0, charWidth * currentCharFillProgress, charHeight);
                }
            }
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
            
            // 清理字符列表资源
            _charListCN.Clear();
            _charListEN.Clear();
            
            // 清理ViewModel资源，取消消息订阅
            _lyricsViewModel.Cleanup();
        }
    }
}
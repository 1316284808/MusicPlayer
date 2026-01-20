using MusicPlayer.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 逐字歌词用户控件
    /// </summary>
    public partial class WordByWordLyricControl : UserControl, IDisposable
    {
        // 配置项
        private readonly int _fontSize = 28;
        private readonly SolidColorBrush _blackBrush = Brushes.Black;
        private readonly SolidColorBrush _blueBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));
        private readonly int _charSpacing = 0;

        // 存储顶层所有字符控件
        private List<TextBlock> _charListEN = new List<TextBlock>(); // 英文歌词字符列表
        private List<TextBlock> _charListCN = new List<TextBlock>(); // 中文歌词字符列表

        // 当前显示的歌词文本
        private string _currentDisplayTextCN = string.Empty;
        private string _currentDisplayTextEN = string.Empty;

        // 当前歌词行对象
        private LyricLine _previousLyricLine;

        /// <summary>
        /// 依赖属性：当前歌词行
        /// </summary>
        public static readonly DependencyProperty CurrentLyricLineProperty = DependencyProperty.Register(
            nameof(CurrentLyricLine), typeof(LyricLine), typeof(WordByWordLyricControl),
            new PropertyMetadata(null, OnCurrentLyricLineChanged));

        /// <summary>
        /// 当前歌词行
        /// </summary>
        public LyricLine CurrentLyricLine
        {
            get => (LyricLine)GetValue(CurrentLyricLineProperty);
            set => SetValue(CurrentLyricLineProperty, value);
        }

        public WordByWordLyricControl()
        {
            InitializeComponent();
            InitLyricCharsCN("");
            InitLyricCharsEN("");
        }

        /// <summary>
        /// 当前歌词行变化事件处理
        /// </summary>
        private static void OnCurrentLyricLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricControl)d;
            control.UpdateLyricLine((LyricLine)e.OldValue, (LyricLine)e.NewValue);
        }

        /// <summary>
        /// 更新歌词行
        /// </summary>
        private void UpdateLyricLine(LyricLine oldLine, LyricLine newLine)
        {
            // 取消之前歌词行的事件订阅
            if (oldLine != null)
            {
                ((INotifyPropertyChanged)oldLine).PropertyChanged -= OnLyricLinePropertyChanged;
            }

            // 更新字符面板
            UpdateLyricChars();

            // 订阅新歌词行的PropertyChanged事件
            _previousLyricLine = newLine;
            if (newLine != null)
            {
                ((INotifyPropertyChanged)newLine).PropertyChanged += OnLyricLinePropertyChanged;
            }
        }

        /// <summary>
        /// 监听LyricLine属性变化
        /// </summary>
        private void OnLyricLinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LyricLine.Progress))
            {
                // 更新字符填充效果
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
            
            // 处理空字符串，显示空格
            if (string.IsNullOrEmpty(lyricText))
            {
                lyricText = " ";
            }
            
            // 将文本按换行符分割为行
            string[] lines = lyricText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 遍历每行文本
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                // 遍历行中的每个字符
                foreach (char c in line)
                {
                    // 创建底层黑色字符
                    var blackTb = CreateCharTextBlock(c.ToString(), _blackBrush);
                    RenderOptions.SetClearTypeHint(blackTb, ClearTypeHint.Enabled);
                    wpBottoms.Children.Add(blackTb);

                    // 创建顶层蓝色字符
                    var blueTb = CreateCharTextBlock(c.ToString(), _blueBrush);
                    RenderOptions.SetClearTypeHint(blueTb, ClearTypeHint.Enabled);
                    wpTops.Children.Add(blueTb);
                    _charListCN.Add(blueTb);

                    // 初始化顶层字符的裁剪区域
                    blueTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
                }

                // 添加换行符
                if (lineIndex < lines.Length - 1)
                {
                    AddNewLine(wpBottoms, wpTops, _charListCN);
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
            
            // 处理空字符串，显示空格
            if (string.IsNullOrEmpty(lyricText))
            {
                lyricText = " ";
            }
            
            // 将文本按换行符分割为行
            string[] lines = lyricText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 遍历每行文本
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                // 遍历行中的每个字符
                foreach (char c in line)
                {
                    // 创建底层黑色字符
                    var blackTb = CreateCharTextBlock(c.ToString(), _blackBrush);
                    RenderOptions.SetClearTypeHint(blackTb, ClearTypeHint.Enabled);
                    wpBottom.Children.Add(blackTb);

                    // 创建顶层蓝色字符
                    var blueTb = CreateCharTextBlock(c.ToString(), _blueBrush);
                    RenderOptions.SetClearTypeHint(blueTb, ClearTypeHint.Enabled);
                    wpTop.Children.Add(blueTb);
                    _charListEN.Add(blueTb);

                    // 初始化顶层字符的裁剪区域
                    blueTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
                }

                // 添加换行符
                if (lineIndex < lines.Length - 1)
                {
                    AddNewLine(wpBottom, wpTop, _charListEN);
                }
            }

            // 更新当前显示文本
            _currentDisplayTextEN = lyricText;
        }

        /// <summary>
        /// 添加换行符
        /// </summary>
        private void AddNewLine(StackPanel bottomPanel, StackPanel topPanel, List<TextBlock> charList)
        {
            // 底层换行符
            var blackNewLine = new TextBlock
            {
                Text = "\n",
                FontSize = _fontSize,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                TextAlignment = TextAlignment.Center
            };
            bottomPanel.Children.Add(blackNewLine);

            // 顶层换行符
            var blueNewLine = new TextBlock
            {
                Text = "\n",
                FontSize = _fontSize,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                TextAlignment = TextAlignment.Center
            };
            topPanel.Children.Add(blueNewLine);
            charList.Add(blueNewLine);

            // 换行符不需要裁剪效果
            blueNewLine.Clip = new RectangleGeometry { Rect = new Rect(0, 0, double.MaxValue, double.MaxValue) };
        }

        /// <summary>
        /// 创建单个字符的TextBlock
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
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
        }

        /// <summary>
        /// 更新歌词字符面板
        /// </summary>
        private void UpdateLyricChars()
        {
            string newTextCN = string.Empty;
            string newTextEN = string.Empty;
            
            if (CurrentLyricLine != null)
            {
                // 获取中文歌词
                newTextCN = CurrentLyricLine.TextCN ?? string.Empty;
                
                // 获取英文歌词
                newTextEN = CurrentLyricLine.TextEN ?? string.Empty;
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
            if (CurrentLyricLine == null)
            {
                return;
            }

            // 当前全局进度 0~1
            double totalProgress = Math.Clamp(CurrentLyricLine.Progress, 0d, 1d);

            // 更新中文歌词高亮
            if (_charListCN.Count > 0)
            {
                int totalCharCountCN = _charListCN.Count;
                double perCharProgressCN = 1.0 / totalCharCountCN;

                for (int i = 0; i < totalCharCountCN; i++)
                {
                    var charTb = _charListCN[i];
                    double currentCharFillProgress = (totalProgress / perCharProgressCN) - i;
                    currentCharFillProgress = Math.Clamp(currentCharFillProgress, 0d, 1d);

                    // 获取字符真实宽高
                    charTb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double charWidth = charTb.DesiredSize.Width;
                    double charHeight = charTb.DesiredSize.Height;

                    // 更新裁剪区域
                    (charTb.Clip as RectangleGeometry).Rect = new Rect(0, 0, charWidth * currentCharFillProgress, charHeight);
                }
            }

            // 更新英文歌词高亮
            if (_charListEN.Count > 0)
            {
                int totalCharCountEN = _charListEN.Count;
                double perCharProgressEN = 1.0 / totalCharCountEN;

                for (int i = 0; i < totalCharCountEN; i++)
                {
                    var charTb = _charListEN[i];
                    double currentCharFillProgress = (totalProgress / perCharProgressEN) - i;
                    currentCharFillProgress = Math.Clamp(currentCharFillProgress, 0d, 1d);

                    // 获取字符真实宽高
                    charTb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double charWidth = charTb.DesiredSize.Width;
                    double charHeight = charTb.DesiredSize.Height;

                    // 更新裁剪区域
                    (charTb.Clip as RectangleGeometry).Rect = new Rect(0, 0, charWidth * currentCharFillProgress, charHeight);
                }
            }
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消事件订阅
                    if (_previousLyricLine != null)
                    {
                        ((INotifyPropertyChanged)_previousLyricLine).PropertyChanged -= OnLyricLinePropertyChanged;
                    }

                    // 清理字符列表
                    _charListCN.Clear();
                    _charListEN.Clear();
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 多行歌词控件 - 按实际渲染行分割，每行独立控制高亮进度
    /// 解决整句歌词换行后多行同步高亮的问题
    /// 优化：使用对象池减少内存分配
    /// </summary>
    public partial class MultiLineLyricControl : UserControl
    {
        // 对象池：存储可重用的 HighlightTextBlock
        private readonly Queue<Helper.HighlightTextBlock> _textBlockPool = new Queue<Helper.HighlightTextBlock>();
        
        // 当前使用的 HighlightTextBlock
        private readonly List<Helper.HighlightTextBlock> _activeLineBlocks = new List<Helper.HighlightTextBlock>();
        
        // 存储原始文本
        private string _originalText = string.Empty;
        
        // 缓存的文本宽度，避免重复计算
        private double _cachedTextWidth = 0;
        
        // 缓存的文本内容，避免重复分割
        private string _cachedText = string.Empty;
        
        // 缓存的分割结果，避免重复分割
        private string[] _cachedLines = Array.Empty<string>();
        
        // 缓存的字体大小，用于判断是否需要重新计算
        private double _cachedFontSize = 20.0;

        public MultiLineLyricControl()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// 从对象池获取或创建 HighlightTextBlock
        /// </summary>
        private Helper.HighlightTextBlock GetTextBlockFromPool()
        {
            if (_textBlockPool.Count > 0)
            {
                return _textBlockPool.Dequeue();
            }
            
            // 池为空时创建新实例
            var textBlock = new Helper.HighlightTextBlock
            {
                TextWrapping = TextWrapping.NoWrap
            };
            
            return textBlock;
        }
        
        /// <summary>
        /// 将 HighlightTextBlock 归还到对象池
        /// </summary>
        private void ReturnTextBlockToPool(Helper.HighlightTextBlock textBlock)
        {
            if (textBlock != null)
            {
                // 重置状态
                textBlock.Text = string.Empty;
                textBlock.Opacity = 1.0;
                
                // 归还到池中
                _textBlockPool.Enqueue(textBlock);
            }
        }

        #region Dependency Properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize),
            typeof(double),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(20.0, OnFontSizePropertyChanged));

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
            nameof(FontWeight),
            typeof(FontWeight),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(FontWeights.Normal, OnTextPropertyChanged));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(Brushes.Gray));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty HighlightColorProperty = DependencyProperty.Register(
            nameof(HighlightColor),
            typeof(Color),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(Color.FromArgb(255, 0, 120, 215)));

        public Color HighlightColor
        {
            get => (Color)GetValue(HighlightColorProperty);
            set => SetValue(HighlightColorProperty, value);
        }

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            nameof(TextAlignment),
            typeof(TextAlignment),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(TextAlignment.Right, OnTextAlignmentPropertyChanged));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(0.0, OnProgressPropertyChanged));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty HighlightWidthProperty = DependencyProperty.Register(
            nameof(HighlightWidth),
            typeof(double),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(0.2, OnProgressPropertyChanged));

        public double HighlightWidth
        {
            get => (double)GetValue(HighlightWidthProperty);
            set => SetValue(HighlightWidthProperty, value);
        }

        public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(
            nameof(LineHeight),
            typeof(double),
            typeof(MultiLineLyricControl),
            new PropertyMetadata(5.0));

        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        #endregion

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiLineLyricControl control)
            {
                control.UpdateLyricLines();
            }
        }

        private static void OnFontSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiLineLyricControl control)
            {
                control.UpdateFontSize();
            }
        }

        private static void OnProgressPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiLineLyricControl control)
            {
                control.UpdateHighlightProgress();
            }
        }

        private static void OnTextAlignmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiLineLyricControl control)
            {
                control.UpdateTextAlignment();
            }
        }

        private void MultiLineLyricControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLyricLines();
        }

        private void MultiLineLyricControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 当宽度变化时，重新计算换行
            if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 0.1)
            {
                UpdateLyricLines();
            }
        }

        /// <summary>
        /// 根据文本和控件宽度，将歌词分割为多行
        /// 使用FormattedText精确测量文本宽度
        /// </summary>
        private void UpdateLyricLines()
        {
            _originalText = Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_originalText))
            {
                ClearAllLines();
                return;
            }

            // 检查缓存：如果文本、字体大小和宽度都没变化，跳过更新
            if (_cachedLines.Length > 0 && _originalText == _cachedText && 
                Math.Abs(ActualWidth - _cachedTextWidth) < 0.1 && 
                Math.Abs(FontSize - _cachedFontSize) < 0.01)
            {
                UpdateHighlightProgress(); // 只更新进度
                return;
            }

            // 清空当前显示的行（归还到对象池）
            ClearAllLines();

            // 获取实际可用的宽度
            double availableWidth = ActualWidth > 0 ? ActualWidth : 600;
            if (availableWidth <= 0)
            {
                CreateSingleLine(_originalText);
                return;
            }

            try
            {
                // 方法1：如果文本中包含显式的换行符，直接按换行符分割
                if (_originalText.Contains('\n'))
                {
                    _cachedLines = _originalText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    // 方法2：使用FormattedText精确测量文本宽度进行换行
                    var lines = new List<string>();
                    var currentLine = new System.Text.StringBuilder();
                    var words = SplitIntoWords(_originalText);
                    
                    foreach (var word in words)
                    {
                        string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                        
                        // 使用FormattedText精确测量宽度
                        var formattedText = new FormattedText(
                            testLine,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                            FontSize,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        
                        if (formattedText.Width <= availableWidth || currentLine.Length == 0)
                        {
                            // 如果加上这个词后宽度仍在范围内，或者这是第一个词
                            if (currentLine.Length > 0)
                            {
                                currentLine.Append(' ');
                            }
                            currentLine.Append(word);
                        }
                        else
                        {
                            // 宽度超出，将当前行添加到结果中
                            if (currentLine.Length > 0)
                            {
                                lines.Add(currentLine.ToString().Trim());
                                currentLine.Clear();
                            }
                            
                            // 将当前词作为新行的开始
                            currentLine.Append(word);
                        }
                    }
                    
                    // 添加最后一行
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString().Trim());
                    }
                    
                    _cachedLines = lines.ToArray();
                }

                // 更新缓存
                _cachedText = _originalText;
                _cachedTextWidth = availableWidth;
                _cachedFontSize = FontSize;

                // 创建行
                for (int i = 0; i < _cachedLines.Length; i++)
                {
                    CreateLineBlock(_cachedLines[i], i > 0 ? LineHeight : 0);
                }
            }
            catch (Exception ex)
            {
                // 如果出错，回退到单行显示
                Debug.WriteLine($"MultiLineLyricControl: 更新歌词行时出错: {ex.Message}");
                _cachedLines = new[] { _originalText };
                CreateSingleLine(_originalText);
            }
        }

        /// <summary>
        /// 将文本分割为单词列表（支持中英文混合）
        /// </summary>
        private List<string> SplitIntoWords(string text)
        {
            var words = new List<string>();
            var currentWord = new System.Text.StringBuilder();
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                // 如果是空格，作为分隔符
                if (char.IsWhiteSpace(c))
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    continue;
                }
                
                // 如果是标点符号，作为独立的词
                if (char.IsPunctuation(c))
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    words.Add(c.ToString());
                    continue;
                }
                
                // 如果是中文字符，作为独立的词
                if (c >= 0x4E00 && c <= 0x9FFF) // 中文字符范围
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    words.Add(c.ToString());
                    continue;
                }
                
                // 其他字符（英文字母、数字等）累积到当前词
                currentWord.Append(c);
            }
            
            // 添加最后一个词
            if (currentWord.Length > 0)
            {
                words.Add(currentWord.ToString());
            }
            
            return words;
        }

        /// <summary>
        /// 创建单行歌词（从对象池获取）
        /// </summary>
        private void CreateSingleLine(string text)
        {
            var highlightTb = GetTextBlockFromPool();
            
            // 配置属性
            highlightTb.Text = text;
            highlightTb.FontSize = FontSize;
            highlightTb.FontWeight = FontWeight;
            highlightTb.Foreground = Foreground;
            highlightTb.HighlightColor = HighlightColor;
            highlightTb.TextAlignment = TextAlignment;
            
            // 设置对齐方式，让控件自动拉伸填充父容器
            highlightTb.HorizontalAlignment = HorizontalAlignment.Stretch;
            highlightTb.VerticalAlignment = VerticalAlignment.Top;
            
            highlightTb.Margin = new Thickness(0);

            LyricLinesPanel.Children.Add(highlightTb);
            _activeLineBlocks.Add(highlightTb);
            _cachedLines = new[] { text };
        }

        /// <summary>
        /// 创建一行歌词块（从对象池获取）
        /// </summary>
        private void CreateLineBlock(string lineText, double topMargin)
        {
            var highlightTb = GetTextBlockFromPool();
            
            // 配置属性
            highlightTb.Text = lineText;
            highlightTb.FontSize = FontSize;
            highlightTb.FontWeight = FontWeight;
            highlightTb.Foreground = Foreground;
            highlightTb.HighlightColor = HighlightColor;
            highlightTb.TextAlignment = TextAlignment;
            
            // 设置对齐方式，让控件自动拉伸填充父容器
            highlightTb.HorizontalAlignment = HorizontalAlignment.Stretch;
            highlightTb.VerticalAlignment = VerticalAlignment.Top;
            
            highlightTb.Margin = new Thickness(0, topMargin, 0, 0);

            LyricLinesPanel.Children.Add(highlightTb);
            _activeLineBlocks.Add(highlightTb);
        }

        /// <summary>
        /// 清空所有行（归还到对象池）
        /// </summary>
        private void ClearAllLines()
        {
            // 将当前激活的行归还到对象池
            foreach (var block in _activeLineBlocks)
            {
                LyricLinesPanel.Children.Remove(block);
                ReturnTextBlockToPool(block);
            }
            
            _activeLineBlocks.Clear();
        }

        /// <summary>
        /// 更新所有行的文本对齐方式
        /// </summary>
        private void UpdateTextAlignment()
        {
            foreach (var block in _activeLineBlocks)
            {
                block.TextAlignment = TextAlignment;
                block.HorizontalAlignment = HorizontalAlignment.Stretch;
                block.VerticalAlignment = VerticalAlignment.Top;
            }
        }

        /// <summary>
        /// 根据总进度，更新每行的高亮进度
        /// </summary>
        private void UpdateHighlightProgress()
        {
            if (_activeLineBlocks.Count == 0) return;

            double totalProgress = Math.Clamp(Progress, 0.0, 1.0);

            // 单行情况
            if (_activeLineBlocks.Count == 1)
            {
                _activeLineBlocks[0].HighlightPos = totalProgress;
                _activeLineBlocks[0].HighlightWidth = HighlightWidth;
                return;
            }

            // 多行情况：计算每行的进度
            double progressPerLine = 1.0 / _activeLineBlocks.Count;

            for (int lineIndex = 0; lineIndex < _activeLineBlocks.Count; lineIndex++)
            {
                var lineBlock = _activeLineBlocks[lineIndex];
                
                // 计算当前行的填充进度
                // 公式: (总进度 - 前面行的进度) / 当前行的进度权重
                double lineProgress = Math.Clamp(
                    (totalProgress - lineIndex * progressPerLine) / progressPerLine,
                    0.0, 1.0);

                // 设置当前行的高亮位置和宽度
                lineBlock.HighlightPos = lineProgress;
                lineBlock.HighlightWidth = HighlightWidth;
            }
        }

        /// <summary>
        /// 更新所有文本块的字体大小
        /// </summary>
        private void UpdateFontSize()
        {
            if (_activeLineBlocks.Count == 0) return;

            foreach (var block in _activeLineBlocks)
            {
                block.FontSize = FontSize;
            }

            // 清除字体大小缓存，强制重新计算布局
            _cachedFontSize = 0; // 设置为0，确保下次UpdateLyricLines会重新计算
            
            // 字体大小变化可能影响换行，需要重新计算
            UpdateLyricLines();
        }
    }
}
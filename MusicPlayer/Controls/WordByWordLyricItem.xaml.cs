using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Core.Models;
using MusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 逐字歌词项控件
    /// </summary>
    public partial class WordByWordLyricItem : UserControl, IDisposable
    {
        // 黑色和蓝色画笔
        private readonly SolidColorBrush _blackBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99000000"));// #333333 
        private readonly SolidColorBrush _blueBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));
        // 存储顶层所有字符控件
        private List<TextBlock> _charListCN = new List<TextBlock>();
        private List<TextBlock> _charListEN = new List<TextBlock>();
        // 当前显示的歌词文本
        private string _currentDisplayTextCN = string.Empty;
        private string _currentDisplayTextEN = string.Empty;
        // 当前歌词行对象
        private LyricLine _currentLyricLine = null;
        // 字体大小
        private double _customFontSize = 16;
        // 选中状态字体大小
        private double _selectedFontSize = 24;
        // 是否选中
        private bool _isSelected = false;
        // 是否启用歌词翻译
        private bool _isLyricTranslationEnabled = true;

        /// <summary>
        /// 依赖属性：当前歌词行
        /// </summary>
        public static readonly DependencyProperty CurrentLyricLineProperty = DependencyProperty.Register(
            nameof(CurrentLyricLine), typeof(LyricLine), typeof(WordByWordLyricItem),
            new PropertyMetadata(null, OnCurrentLyricLineChanged));

        /// <summary>
        /// 依赖属性：选中状态字体大小
        /// </summary>
        public static readonly DependencyProperty SelectedFontSizeProperty = DependencyProperty.Register(
            nameof(SelectedFontSize), typeof(double), typeof(WordByWordLyricItem),
            new PropertyMetadata(24.0, OnSelectedFontSizeChanged));

        /// <summary>
        /// 依赖属性：文本对齐方式
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            nameof(TextAlignment), typeof(HorizontalAlignment), typeof(WordByWordLyricItem),
            new PropertyMetadata(HorizontalAlignment.Left, OnTextAlignmentChanged));

        /// <summary>
        /// 依赖属性：是否选中
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(WordByWordLyricItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

        /// <summary>
        /// 依赖属性：是否启用歌词翻译
        /// </summary>
        public static readonly DependencyProperty IsLyricTranslationEnabledProperty = DependencyProperty.Register(
            nameof(IsLyricTranslationEnabled), typeof(bool), typeof(WordByWordLyricItem),
            new PropertyMetadata(true, OnIsLyricTranslationEnabledChanged));

        /// <summary>
        /// 是否启用歌词翻译
        /// </summary>
        public bool IsLyricTranslationEnabled
        {
            get => (bool)GetValue(IsLyricTranslationEnabledProperty);
            set => SetValue(IsLyricTranslationEnabledProperty, value);
        }

        /// <summary>
        /// 依赖属性：自定义字体大小
        /// </summary>
        public static readonly DependencyProperty CustomFontSizeProperty = DependencyProperty.Register(
            nameof(CustomFontSize), typeof(double), typeof(WordByWordLyricItem),
            new PropertyMetadata(16.0, OnCustomFontSizeChanged));

        /// <summary>
        /// 当前歌词行
        /// </summary>
        public LyricLine CurrentLyricLine
        {
            get => (LyricLine)GetValue(CurrentLyricLineProperty);
            set => SetValue(CurrentLyricLineProperty, value);
        }

        /// <summary>
        /// 自定义字体大小
        /// </summary>
        public double CustomFontSize
        {
            get => (double)GetValue(CustomFontSizeProperty);
            set => SetValue(CustomFontSizeProperty, value);
        }

        /// <summary>
        /// 选中状态字体大小
        /// </summary>
        public double SelectedFontSize
        {
            get => (double)GetValue(SelectedFontSizeProperty);
            set => SetValue(SelectedFontSizeProperty, value);
        }

        /// <summary>
        /// 文本对齐方式
        /// </summary>
        public HorizontalAlignment TextAlignment
        {
            get => (HorizontalAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public WordByWordLyricItem()
        {
            InitializeComponent();
        }
         /// <summary>
        /// 当前歌词行变化事件处理
        /// </summary>
        private static void OnCurrentLyricLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricItem)d;
            control. ResetAllCharsFill();
            control.UpdateLyricLine((LyricLine)e.OldValue, (LyricLine)e.NewValue);
        }

        /// <summary>
        /// 自定义字体大小变化事件处理
        /// </summary>
        private static void OnCustomFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricItem)d;
            control._customFontSize = (double)e.NewValue;
            control.UpdateFontSize();
        }

        /// <summary>
        /// 选中状态字体大小变化事件处理
        /// </summary>
        private static void OnSelectedFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricItem)d;
            control._selectedFontSize = (double)e.NewValue;
            control.UpdateFontSize();
        }

        /// <summary>
        /// 文本对齐方式变化事件处理
        /// </summary>
        private static void OnTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricItem)d;
            //control._textAlignment = (TextAlignment)e.NewValue;
            
            // 设置中文字符对齐方式
            control.BottomWrapPanel.HorizontalAlignment = (HorizontalAlignment)e.NewValue;
            control.TopWrapPanel.HorizontalAlignment = (HorizontalAlignment)e.NewValue;
            
            // 设置英文字符对齐方式
            control.BottomWrapPanel1.HorizontalAlignment = (HorizontalAlignment)e.NewValue;
            control.TopWrapPanel1.HorizontalAlignment = (HorizontalAlignment)e.NewValue;
        }

        /// <summary>
        /// 是否选中变化事件处理
        /// </summary>
        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricItem)d;
            control._isSelected = (bool)e.NewValue;
            control.UpdateFontSize();
            
            // 当IsSelected变为false时，重置所有字符的填色效果，使其变为黑色
            if (!(bool)e.NewValue)
            {
                control.ResetAllCharsFill();
            }
        }

        /// <summary>
        /// 是否启用歌词翻译变化事件处理
        /// </summary>
        private static void OnIsLyricTranslationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WordByWordLyricItem)d;
            control._isLyricTranslationEnabled = (bool)e.NewValue;
            control.UpdateLyricChars();
        }

        /// <summary>
        /// 重置所有字符的填色效果，使其变为黑色
        /// </summary>
        private void ResetAllCharsFill()
        {
            // 重置中文字符填充效果
            foreach (var charTb in _charListCN)
            {
                // 将所有字符的Clip区域重置为0宽度，使其变为黑色
                charTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
            }

            // 重置英文字符填充效果
            foreach (var charTb in _charListEN)
            {
                // 将所有字符的Clip区域重置为0宽度，使其变为黑色
                charTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
            }
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

            // 更新当前歌词行
            _currentLyricLine = newLine;

            // 更新字符面板
            UpdateLyricChars();

            // 订阅新歌词行的PropertyChanged事件
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
        /// 初始化中文字符面板
        /// </summary>
        private void InitLyricCharsCN(string lyricText)
        {
            // 清空现有字符
            BottomWrapPanel.Children.Clear();
            TopWrapPanel.Children.Clear();
            _charListCN.Clear();

            // 处理空字符串
            if (string.IsNullOrEmpty(lyricText))
            {
                _currentDisplayTextCN = string.Empty;
                return;
            }

            // 遍历每个字符
            foreach (char c in lyricText)
            {
                // 创建底层黑色字符
                var blackTb = CreateCharTextBlockCN(c.ToString(), _blackBrush);
                BottomWrapPanel.Children.Add(blackTb);

                // 创建顶层蓝色字符
                var blueTb = CreateCharTextBlockCN(c.ToString(), _blueBrush);
                TopWrapPanel.Children.Add(blueTb);
                _charListCN.Add(blueTb);

                // 初始状态：蓝色文本不可见
                blueTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
            }

            // 更新当前显示文本
            _currentDisplayTextCN = lyricText;
        }

        /// <summary>
        /// 初始化英文字符面板
        /// </summary>
        private void InitLyricCharsEN(string lyricText)
        {
            // 清空现有字符
            BottomWrapPanel1.Children.Clear();
            TopWrapPanel1.Children.Clear();
            _charListEN.Clear();

            // 处理空字符串
            if (string.IsNullOrEmpty(lyricText))
            {
                _currentDisplayTextEN = string.Empty;
                return;
            }

            // 遍历每个字符
            foreach (char c in lyricText)
            {
                // 创建底层黑色字符
                var blackTb = CreateCharTextBlockeN(c.ToString(), _blackBrush);
                BottomWrapPanel1.Children.Add(blackTb);

                // 创建顶层蓝色字符
                var blueTb = CreateCharTextBlockeN(c.ToString(), _blueBrush);
                TopWrapPanel1.Children.Add(blueTb);
                _charListEN.Add(blueTb);

                // 初始状态：蓝色文本不可见
                blueTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, double.MaxValue) };
            }

            // 更新当前显示文本
            _currentDisplayTextEN = lyricText;
        }

        /// <summary>
        /// 创建单个字符的TextBlock
        /// </summary>
        private TextBlock CreateCharTextBlockCN(string text, Brush brush)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Foreground = brush, 
              
                Margin = new Thickness(0, 0, 0, 0),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                FontSize = _isSelected ? _selectedFontSize : _customFontSize
            };

            return tb;
        }


        private TextBlock CreateCharTextBlockeN(string text, Brush brush)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Foreground = brush, 
                Margin = new Thickness(0, 0, 0, 0),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                FontSize =   _customFontSize
            };

            return tb;
        }

        /// <summary>
        /// 更新歌词字符面板
        /// </summary>
        private void UpdateLyricChars()
        {
            string newTextCN = string.Empty;
            string newTextEN = string.Empty;

            if (_currentLyricLine != null)
            {
                newTextCN = _currentLyricLine.TextCN;
                newTextEN = _currentLyricLine.TextEN;
            }

            // 更新中文歌词字符面板
            if (newTextCN != _currentDisplayTextCN)
            {
                InitLyricCharsCN(newTextCN);
            }

            // 更新英文歌词字符面板
            if (newTextEN != _currentDisplayTextEN)
            {
                InitLyricCharsEN(newTextEN);
            }

            // 控制Grid的可见性
            CNGrid.Visibility = string.IsNullOrEmpty(newTextCN) ? Visibility.Collapsed : Visibility.Visible;
            //禁用歌词翻译的共能必须是在双语歌词的前提下。
            if (newTextEN.Length>0) { 
            CNGrid.Visibility = (_isLyricTranslationEnabled && !string.IsNullOrEmpty(newTextEN)) ? Visibility.Visible : Visibility.Collapsed;
            }
            // 立即应用当前进度
            UpdateLyricProgress();
        }

        /// <summary>
        /// 更新字体大小
        /// </summary>
        private void UpdateFontSize()
        {
            double currentFontSize = _isSelected ? _selectedFontSize : _customFontSize;

            // 更新中文底层字符字体大小
            foreach (TextBlock tb in BottomWrapPanel.Children)
            {
                tb.FontSize = currentFontSize;
              
            }

            // 更新中文顶层字符字体大小
            foreach (TextBlock tb in TopWrapPanel.Children)
            {
                tb.FontSize = currentFontSize;
                
            }

            // 更新英文底层字符字体大小
            foreach (TextBlock tb in BottomWrapPanel1.Children)
            {
                tb.FontSize = currentFontSize;
                
            }

            // 更新英文顶层字符字体大小
            foreach (TextBlock tb in TopWrapPanel1.Children)
            {
                tb.FontSize = currentFontSize;
               
            }
        }

        /// <summary>
        /// 更新歌词进度，驱动逐字填充效果
        /// </summary>
        private void UpdateLyricProgress()
        {
            if (_currentLyricLine == null)
            {
                return;
            }

            // 只有当歌词行处于选中状态时，才更新其进度效果
            if (!_isSelected)
            {
                ResetAllCharsFill();
                return;
            }

            // 当前行的全局进度 0~1
            double totalProgress = Math.Clamp(_currentLyricLine.Progress, 0d, 1d);

            // 更新中文字符高亮
            if (_charListCN.Count > 0)
            {
                int totalCharCount = _charListCN.Count;
                double perCharProgress = 1.0 / totalCharCount;

                for (int i = 0; i < totalCharCount; i++)
                {
                    var charTb = _charListCN[i];
                    double currentCharFillProgress = (totalProgress / perCharProgress) - i;
                    currentCharFillProgress = Math.Clamp(currentCharFillProgress, 0d, 1d);

                    // 设置字符填充进度
                    SetCharFillProgress(charTb, currentCharFillProgress);
                }
            }

            // 更新英文字符高亮
            if (_charListEN.Count > 0)
            {
                int totalCharCount = _charListEN.Count;
                double perCharProgress = 1.0 / totalCharCount;

                for (int i = 0; i < totalCharCount; i++)
                {
                    var charTb = _charListEN[i];
                    double currentCharFillProgress = (totalProgress / perCharProgress) - i;
                    currentCharFillProgress = Math.Clamp(currentCharFillProgress, 0d, 1d);

                    // 设置字符填充进度
                    SetCharFillProgress(charTb, currentCharFillProgress);
                }
            }
        }

        /// <summary>
        /// 设置单个字符的填充进度
        /// </summary>
        private void SetCharFillProgress(TextBlock charTb, double progress)
        {
            progress = Math.Clamp(progress, 0d, 1d);
            // 标点符号瞬间变蓝，无过渡
            char currentChar = charTb.Text[0];
            bool isSymbol = new[] { '，', '。', '？', '！', '、', ' ', '.', ',', ';', '：', '：' }.Contains(currentChar);
            double fillPro = isSymbol ? (progress > 0 ? 1 : 0) : progress;

            // 测量字符大小
            charTb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double charWidth = charTb.DesiredSize.Width;
            double charHeight = charTb.DesiredSize.Height;

            // 设置Clip区域，实现填充效果
            if (charTb.Clip == null || !(charTb.Clip is RectangleGeometry))
            {
                charTb.Clip = new RectangleGeometry { Rect = new Rect(0, 0, charWidth * fillPro, charHeight) };
            }
            else
            {
                (charTb.Clip as RectangleGeometry).Rect = new Rect(0, 0, charWidth * fillPro, charHeight);
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
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    
                    if (_currentLyricLine != null)
                    {
                        ((INotifyPropertyChanged)_currentLyricLine).PropertyChanged -= OnLyricLinePropertyChanged;
                    }
                    
                    // 清空DataContext，解除对ViewModel的强引用
                    this.DataContext = null;
                    
                    // 清理资源
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
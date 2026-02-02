using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 中心内容控件
    /// 负责显示专辑封面、圆形频谱和歌词
    /// </summary>
    public partial class CenterContentControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public CenterContentControl()
        {
            InitializeComponent();
            
            // 注册歌词更新消息，在歌词更新前清理旧资源
            WeakReferenceMessenger.Default.Register<MusicPlayer.Services.Messages.LyricsUpdatedMessage>(this, (recipient, message) =>
            {
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 收到歌词更新消息，开始清理旧歌词资源");
                CleanupLyricItems();
            });
            
            // 延迟初始化频谱控件（等页面基本渲染完成后再初始化）
            this.Loaded += async (s, e) => 
            {
                await Task.Delay(100); // 等待100ms，让页面基本渲染完成
                InitializeSpectrumControl();
            };
        }

        /// <summary>
        /// 初始化频谱控件（延迟调用，避免阻塞UI）
        /// </summary>
        private void InitializeSpectrumControl()
        {
            if (CircularSpectrum != null)
            {
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 延迟初始化频谱控件");
                // 如果频谱控件有需要初始化的逻辑，可以在这里调用
                // CircularSpectrum.Initialize();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    
                    // 清理附加行为
                    CleanupAttachedBehaviors();
                    
                    // 手动清理所有MultiLineLyricControl的绑定
                    CleanupMultiLineLyricControls();
                    
                    // 清理歌词项
                    CleanupLyricItems();
                    
                    // 清空ListBox ItemsSource，移除所有子项（包括MultiLineLyricControl）
                    if (LyricsListBox != null)
                    {
                        LyricsListBox.ItemsSource = null;
                    }
                    
                    // 调用CircularSpectrumControl的Dispose方法
                    if (CircularSpectrum != null)
                    {
                        CircularSpectrum.Dispose(); 
                    }
                    
                    // 清空DataContext，解除所有绑定
                    this.DataContext = null;
                    
                    // 清空页面内容，释放UI资源
                    this.Content = null;
                }
                _disposed = true;
            }
        }
        
        /// <summary>
        /// 手动清理所有MultiLineLyricControl的绑定
        /// </summary>
        private void CleanupMultiLineLyricControls()
        {
            try
            {
                // 查找所有ListBoxItem
                var listBoxItems = FindAllVisualChildren<ListBoxItem>(this);
                foreach (var listBoxItem in listBoxItems)
                {
                    // 查找ListBoxItem中的所有MultiLineLyricControl
                    var multiLineLyricControls = FindAllVisualChildren<MultiLineLyricControl>(listBoxItem);
                    foreach (var multiLineLyricControl in multiLineLyricControls)
                    {
                        // 调用MultiLineLyricControl的Cleanup方法清理绑定
                        multiLineLyricControl.Cleanup();
                    }
                }
            }
            catch { }
        }

        // 清理附加行为
        private void CleanupAttachedBehaviors()
        {
            // 清理LyricsScrollBehavior
            if (LyricsListBox != null)
            {
                // 重置手动滚动状态
                MusicPlayer.Helper.LyricsScrollBehavior.SetIsManualScrolling(LyricsListBox, false);
                
                // 禁用LyricsScrollBehavior的自动滚动和加载时滚动功能
                MusicPlayer.Helper.LyricsScrollBehavior.SetAutoScrollToCenter(LyricsListBox, false);
                MusicPlayer.Helper.LyricsScrollBehavior.SetScrollOnLoad(LyricsListBox, false);
                
                // 获取并清理ScrollViewer
                var scrollViewer = GetScrollViewer(LyricsListBox);
                if (scrollViewer != null)
                {
                    // 重置滚动位置
                    scrollViewer.ScrollToVerticalOffset(0);
                }
                
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 已清理LyricsScrollBehavior");
            }
            
            // 清理StackPanelMouseBehavior
            // 找到带有StackPanelMouseBehavior的StackPanel
            var stackPanelWithMouseBehavior = FindVisualChild<StackPanel>(this);
            if (stackPanelWithMouseBehavior != null)
            {
                // 通过将命令设置为null来清理StackPanelMouseBehavior
                MusicPlayer.Helper.StackPanelMouseBehavior.SetMouseEnterCommand(stackPanelWithMouseBehavior, null);
                MusicPlayer.Helper.StackPanelMouseBehavior.SetMouseLeaveCommand(stackPanelWithMouseBehavior, null);
                
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 已清理StackPanelMouseBehavior");
            }
        }
        
        // 获取ListBox中的ScrollViewer
        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer viewer)
                return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        
        // 清理歌词项
        private void CleanupLyricItems()
        {
            if (LyricsListBox != null)
            {
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 开始清理歌词项资源");
                
                // 先遍历所有ListBoxItem容器，调用Dispose方法释放资源
                for (int i = LyricsListBox.Items.Count - 1; i >= 0; i--)
                {
                    var listBoxItem = LyricsListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        // 获取DataTemplate中的所有MultiLineLyricControl并清理绑定
                        var lyricControls = FindAllVisualChildren<MultiLineLyricControl>(listBoxItem);
                        foreach (var lyricControl in lyricControls)
                        {
                            try
                            {
                                // 调用Cleanup方法清理内部资源（包含绑定清理）
                                lyricControl.Cleanup();
                            }
                            catch { }
                        }
                        
                        // 通过VisualTreeHelper获取所有WordByWordLyricItem
                        var lyricItems = FindAllVisualChildren<WordByWordLyricItem>(listBoxItem);
                        foreach (var lyricItem in lyricItems)
                        {
                            if (lyricItem is IDisposable disposableLyricItem)
                            {
                                disposableLyricItem.Dispose();
                                System.Diagnostics.Debug.WriteLine("CenterContentControl: 已释放歌词项资源");
                            }
                        }
                        
                        // 清空ListBoxItem的Content和DataContext
                        listBoxItem.Content = null;
                        listBoxItem.DataContext = null;
                    }
                }
                
                // 调用UpdateLayout确保UI更新
                LyricsListBox.UpdateLayout();
                
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 歌词项资源清理完成");
            }
        }
        
        // 查找所有指定类型的视觉子元素
        private List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var result = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    result.Add(typedChild);
                }
                result.AddRange(FindAllVisualChildren<T>(child));
            }
            return result;
        }
        
        // 查找视觉子元素的辅助方法
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var foundChild = FindVisualChild<T>(child);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
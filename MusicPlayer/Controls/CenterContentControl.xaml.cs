using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;
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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    
                    // 调用CircularSpectrumControl的Dispose方法
                    if (CircularSpectrum != null)
                    {
                        CircularSpectrum.Dispose(); 
                    }
                    
                    // 清理附加行为
                    CleanupAttachedBehaviors();
                    
                    // 清理歌词项
                    CleanupLyricItems();
                    
                    // 清空DataContext，解除对ViewModel的强引用
                    this.DataContext = null;
                    
                    // 清空页面内容，释放UI资源
                    this.Content = null;
                }
                _disposed = true;
            }
        }

        // 清理附加行为
        private void CleanupAttachedBehaviors()
        {
            // 清理LyricsScrollBehavior
            if (LyricsListBox != null)
            {
                // 禁用LyricsScrollBehavior的自动滚动和加载时滚动功能
                MusicPlayer.Helper.LyricsScrollBehavior.SetAutoScrollToCenter(LyricsListBox, false);
                MusicPlayer.Helper.LyricsScrollBehavior.SetScrollOnLoad(LyricsListBox, false);
            }
            
            // 清理StackPanelMouseBehavior
            // 找到带有StackPanelMouseBehavior的StackPanel
            var stackPanelWithMouseBehavior = FindVisualChild<StackPanel>(this);
            if (stackPanelWithMouseBehavior != null)
            {
                // 通过将命令设置为null来清理StackPanelMouseBehavior
                MusicPlayer.Helper.StackPanelMouseBehavior.SetMouseEnterCommand(stackPanelWithMouseBehavior, null);
                MusicPlayer.Helper.StackPanelMouseBehavior.SetMouseLeaveCommand(stackPanelWithMouseBehavior, null);
            }
        }
        
        // 清理歌词项
        private void CleanupLyricItems()
        {
            if (LyricsListBox != null)
            {
                // 遍历所有歌词项，调用Dispose方法（如果实现了IDisposable）
                foreach (var item in LyricsListBox.Items)
                {
                    // 获取ListBoxItem
                    var listBoxItem = LyricsListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        // 通过VisualTreeHelper获取所有WordByWordLyricItem
                        var lyricItems = FindAllVisualChildren<WordByWordLyricItem>(listBoxItem);
                        foreach (var lyricItem in lyricItems)
                        {
                            if (lyricItem is IDisposable disposableLyricItem)
                            {
                                disposableLyricItem.Dispose();
                            }
                        }
                    }
                }
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
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
        }

       

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 开始执行Dispose方法");
                    
                    // 取消消息订阅
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 已取消所有消息订阅");
                    
                    // 清理附加行为
                    CleanupAttachedBehaviors();
                    
                    // 手动清理所有MultiLineLyricControl的绑定
                    CleanupMultiLineLyricControls();
                    
                    // 清理歌词项
                    CleanupLyricItems();
                    
                    // 清空ListBox ItemsSource，移除所有子项（包括MultiLineLyricControl）
                    if (LyricsListBox != null)
                    {
                        System.Diagnostics.Debug.WriteLine("CenterContentControl: 清空LyricsListBox的ItemsSource");
                        LyricsListBox.ItemsSource = null;
                        LyricsListBox.DataContext = null;
                    }

                    // 调用CircularSpectrumControl的Dispose方法
                    if (CircularSpectrum != null)
                    {
                        System.Diagnostics.Debug.WriteLine("CenterContentControl: 释放CircularSpectrumControl资源");
                        CircularSpectrum.Dispose();
                        CircularSpectrum.DataContext = null;
                    }

                    // 清理所有子控件的DataContext
                    CleanupChildControlsDataContext(this);
                    
                    // 清空自身DataContext，解除所有绑定
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 清空自身DataContext");
                    this.DataContext = null;
                    
                    // 清空页面内容，释放UI资源
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 清空页面内容");
                    this.Content = null;
                }
                _disposed = true;
                System.Diagnostics.Debug.WriteLine("CenterContentControl: Dispose方法执行完成");
            }
        }
        
        /// <summary>
        /// 递归清理所有子控件的DataContext
        /// </summary>
        private void CleanupChildControlsDataContext(DependencyObject parent)
        {
            if (parent == null)
                return;
            
            // 清理当前控件的DataContext
            if (parent is FrameworkElement element)
            {
                element.DataContext = null;
            }
            
            // 递归清理所有子控件
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                CleanupChildControlsDataContext(child);
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
        /// <summary>
        /// 清理歌词项
        /// </summary> 
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
                        
                        // 从视觉树中移除ListBoxItem
                        if (listBoxItem.Parent is Panel parentPanel)
                        {
                            parentPanel.Children.Remove(listBoxItem);
                        }
                    }
                }
                
                // 智能清理策略：临时设置为null并立即重新绑定
                try
                {
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 执行智能清理策略");
                    
                    // 保存当前的DataContext，确保重新绑定时使用正确的上下文
                    var currentDataContext = LyricsListBox.DataContext;
                    
                    // 临时设置ItemsSource为null，触发UI清理旧项
                    LyricsListBox.ItemsSource = null;
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 临时设置ItemsSource为null");
                    
                    // 强制UI更新，确保清理生效
                    LyricsListBox.UpdateLayout();
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 强制UI更新");
                    
                    // 立即重新绑定到Lyrics属性，保持数据绑定关系
                    if (currentDataContext != null)
                    {
                        // 使用Binding对象重新绑定，确保与XAML中的绑定一致
                        var binding = new Binding("Lyrics");
                        binding.Source = currentDataContext;
                        LyricsListBox.SetBinding(ListBox.ItemsSourceProperty, binding);
                        System.Diagnostics.Debug.WriteLine("CenterContentControl: 重新绑定到Lyrics属性");
                    }
                    
                    // 再次强制UI更新，确保歌词正常显示
                    LyricsListBox.UpdateLayout();
                    System.Diagnostics.Debug.WriteLine("CenterContentControl: 再次强制UI更新");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CenterContentControl: 智能清理策略执行失败: {ex.Message}");
                    // 清理失败时，尝试恢复绑定
                    try
                    {
                        var currentDataContext = LyricsListBox.DataContext;
                        if (currentDataContext != null)
                        {
                            var binding = new Binding("Lyrics");
                            binding.Source = currentDataContext;
                            LyricsListBox.SetBinding(ListBox.ItemsSourceProperty, binding);
                            LyricsListBox.UpdateLayout();
                        }
                    }
                    catch { }
                }
                
                // 添加强制垃圾回收，确保资源及时释放
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 执行垃圾回收");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                System.Diagnostics.Debug.WriteLine("CenterContentControl: 垃圾回收完成");
                
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
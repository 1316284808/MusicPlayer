using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 专辑封面可视范围检测服务
    /// 实现精确的封面可见性检测和智能加载策略
    /// </summary>
    public class AlbumArtViewportService
    {
        private readonly Dictionary<FrameworkElement, ScrollViewer> _scrollViewers = new();
        private readonly Dictionary<string, DateTime> _lastLoadTime = new();
        private readonly object _lockObject = new();
        
        // 可视范围缓冲区（上下各扩展的项数）
        private const int ViewportBuffer = 2;
        
        // 滚动延迟加载时间（毫秒）
        private const int ScrollLoadDelay = 200;
        
        // 单个封面最小加载间隔（毫秒）
        private const int MinLoadInterval = 50;
        
        /// <summary>
        /// 注册滚动视图
        /// </summary>
        /// <param name="itemsControl">列表控件</param>
        public void RegisterScrollViewer(ItemsControl itemsControl)
        {
            if (itemsControl == null) return;
            
            // 查找ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(itemsControl);
            if (scrollViewer != null)
            {
                lock (_lockObject)
                {
                    _scrollViewers[itemsControl] = scrollViewer;
                    
                    // 订阅滚动事件
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                    scrollViewer.ScrollChanged += OnScrollChanged;
                    
                    // 订阅大小变化事件
                    scrollViewer.SizeChanged -= OnSizeChanged;
                    scrollViewer.SizeChanged += OnSizeChanged;
                }
            }
        }
        
        /// <summary>
        /// 注销滚动视图
        /// </summary>
        /// <param name="itemsControl">列表控件</param>
        public void UnregisterScrollViewer(ItemsControl itemsControl)
        {
            if (itemsControl == null) return;
            
            lock (_lockObject)
            {
                if (_scrollViewers.TryGetValue(itemsControl, out var scrollViewer))
                {
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                    scrollViewer.SizeChanged -= OnSizeChanged;
                    _scrollViewers.Remove(itemsControl);
                }
            }
        }
        
        /// <summary>
        /// 检测项目是否在可视范围内
        /// </summary>
        /// <param name="container">容器元素</param>
        /// <param name="itemsControl">父级控件</param>
        /// <returns>是否在可视范围内</returns>
        public bool IsInViewport(FrameworkElement container, ItemsControl itemsControl)
        {
            if (container == null || itemsControl == null || !container.IsVisible)
                return false;
                
            ScrollViewer scrollViewer;
            lock (_lockObject)
            {
                if (!_scrollViewers.TryGetValue(itemsControl, out scrollViewer))
                    return false;
            }
            
            try
            {
                // 获取容器相对于ScrollViewer的位置
                var containerBounds = container.TransformToAncestor(scrollViewer)
                    .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                
                // 获取视口范围（包含缓冲区）
                var viewport = GetViewportWithBuffer(scrollViewer, itemsControl);
                
                // 检查是否在视口范围内
                return viewport.IntersectsWith(containerBounds);
            }
            catch
            {
                // 如果布局计算失败，保守处理：认为不可见
                return false;
            }
        }
        
        /// <summary>
        /// 检查歌曲是否应该加载封面
        /// </summary>
        /// <param name="song">歌曲</param>
        /// <param name="container">容器元素</param>
        /// <param name="itemsControl">父级控件</param>
        /// <returns>是否应该加载封面</returns>
        public bool ShouldLoadAlbumArt(Song song, FrameworkElement container, ItemsControl itemsControl)
        {
            if (song == null || container == null || itemsControl == null)
                return false;
                
            // 检查是否在可视范围内
            if (!IsInViewport(container, itemsControl))
                return false;
            
            // 检查加载间隔，避免过于频繁的加载
            var songKey = song.FilePath;
            var now = DateTime.Now;
            
            lock (_lockObject)
            {
                if (_lastLoadTime.TryGetValue(songKey, out var lastLoadTime))
                {
                    if ((now - lastLoadTime).TotalMilliseconds < MinLoadInterval)
                        return false;
                }
                
                _lastLoadTime[songKey] = now;
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取可视范围内的歌曲项目
        /// </summary>
        /// <param name="itemsControl">列表控件</param>
        /// <returns>可视范围内的歌曲容器</returns>
        public IEnumerable<(FrameworkElement container, Song song)> GetVisibleItems(ItemsControl itemsControl)
        {
            if (itemsControl == null)
                return Enumerable.Empty<(FrameworkElement, Song)>();
            
            var results = new List<(FrameworkElement, Song)>();
            
            // 遍历所有可见的容器
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null && container.IsVisible)
                {
                    // 获取歌曲数据
                    if (itemsControl.Items[i] is Song song)
                    {
                        results.Add((container, song));
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 获取离开可视范围的歌曲项目
        /// </summary>
        /// <param name="itemsControl">列表控件</param>
        /// <param name="visibleItems">当前可视范围内的项目</param>
        /// <returns>离开可视范围的歌曲容器</returns>
        public IEnumerable<(FrameworkElement container, Song song)> GetOutOfViewportItems(
            ItemsControl itemsControl, 
            IEnumerable<(FrameworkElement container, Song song)> visibleItems)
        {
            if (itemsControl == null || visibleItems == null)
                return Enumerable.Empty<(FrameworkElement, Song)>();
            
            var visibleSet = new HashSet<Song>(visibleItems.Select(item => item.song));
            var results = new List<(FrameworkElement, Song)>();
            
            // 遍历所有项目，找出那些不在可视范围内但已加载封面的项目
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                // 获取歌曲数据
                if (itemsControl.Items[i] is Song song)
                {
                    // 检查是否不在可视范围内但已加载封面
                    if (!visibleSet.Contains(song) && (song.AlbumArt != null || song.OriginalAlbumArt != null))
                    {
                        // 尝试获取容器（可能已被虚拟化机制回收）
                        var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                        
                        // 无论容器是否存在，都应该释放封面资源
                        // 使用null作为容器值，表示容器已被虚拟化
                        results.Add((container, song));
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 滚动事件处理
        /// </summary>
        private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;
                
            // 滚动时延迟处理，避免频繁操作
            await Task.Delay(ScrollLoadDelay);
            
            // 查找对应的ItemsControl
            ItemsControl itemsControl = null;
            lock (_lockObject)
            {
                var pair = _scrollViewers.FirstOrDefault(x => x.Value == scrollViewer);
                itemsControl = pair.Key as ItemsControl;
            }
            
            if (itemsControl != null)
            {
                // 触发可视范围更新事件
                ViewportChanged?.Invoke(itemsControl, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// 大小变化事件处理
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;
                
            // 查找对应的ItemsControl
            ItemsControl itemsControl = null;
            lock (_lockObject)
            {
                var pair = _scrollViewers.FirstOrDefault(x => x.Value == scrollViewer);
                itemsControl = pair.Key as ItemsControl;
            }
            
            if (itemsControl != null)
            {
                // 触发可视范围更新事件
                ViewportChanged?.Invoke(itemsControl, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// 获取包含缓冲区的视口范围
        /// </summary>
        /// <param name="scrollViewer">滚动视图</param>
        /// <param name="itemsControl">列表控件</param>
        /// <returns>视口范围</returns>
        private Rect GetViewportWithBuffer(ScrollViewer scrollViewer, ItemsControl itemsControl)
        {
            // 基础视口
            var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
            
            // 计算缓冲区高度（基于项目高度的估算）
            var bufferHeight = 0;
            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement firstContainer)
            {
                var itemHeight = firstContainer.ActualHeight;
                bufferHeight = (int)(itemHeight * ViewportBuffer);
            }
            
            // 扩展视口范围
            viewport.Y -= bufferHeight;
            viewport.Height += 2 * bufferHeight;
            
            return viewport;
        }
        
        /// <summary>
        /// 查找指定类型的视觉子元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="parent">父元素</param>
        /// <returns>找到的元素</returns>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            
            return null;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                foreach (var pair in _scrollViewers)
                {
                    pair.Value.ScrollChanged -= OnScrollChanged;
                    pair.Value.SizeChanged -= OnSizeChanged;
                }
                
                _scrollViewers.Clear();
                _lastLoadTime.Clear();
            }
        }
        
        /// <summary>
        /// 视口变化事件
        /// </summary>
        public event EventHandler<EventArgs>? ViewportChanged;
    }
}
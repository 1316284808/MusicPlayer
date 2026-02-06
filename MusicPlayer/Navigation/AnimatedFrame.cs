using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services.Messages;
using MusicPlayer.Page;
using System.Windows.Navigation;

namespace MusicPlayer.Navigation
{
    /// <summary>
    /// 支持页面切换动画的Frame控件
    /// </summary>
    public class AnimatedFrame : Frame
    {
        private Storyboard? _fadeInStoryboard;
        private Storyboard? _fadeOutStoryboard;
        private bool _isNavigating = false;
        private object? _pendingNavigationContent;
        private Uri? _pendingNavigationSource;
        private object? _pendingNavigationExtraData;
        private bool _pendingNavigationWithExtraData = false;
        private bool _pendingGoBack = false;
        private bool _pendingGoForward = false;
        private IServiceProvider? _serviceProvider;
        private IMessagingService? _messagingService;

        public AnimatedFrame()
        {
            this.Loaded += AnimatedFrame_Loaded;
            this.Navigated += AnimatedFrame_Navigated;
            // 禁用导航历史栈
            this.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            this.NavigationService.RemoveBackEntry();
            
            // 尝试获取服务提供者和消息服务
            try
            {
                if (Application.Current is App app)
                {
                    _serviceProvider = app.ServiceProvider;
                    if (_serviceProvider != null)
                    {
                        _messagingService = _serviceProvider.GetService<IMessagingService>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AnimatedFrame: 初始化服务失败: {ex.Message}");
            }
        }

        private void AnimatedFrame_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化动画资源
            CreateAnimations();

            // 初始状态为透明
            this.Opacity = 0;

            // 延迟执行初始淡入动画
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_fadeInStoryboard != null)
                {
                    _fadeInStoryboard.Begin();
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void CreateAnimations()
        {
            // 淡入动画
            _fadeInStoryboard = new Storyboard();
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
         
            };
            Storyboard.SetTarget(fadeInAnimation, this);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));
            _fadeInStoryboard.Children.Add(fadeInAnimation);
          
            // 淡出动画
            _fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
           
            };
            Storyboard.SetTarget(fadeOutAnimation, this);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            _fadeOutStoryboard.Children.Add(fadeOutAnimation);
           
        }

        private void AnimatedFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // 导航完成后立即清理历史记录，禁用导航历史栈
            while (this.NavigationService.CanGoBack)
            {
                this.NavigationService.RemoveBackEntry();
            }
            
            // 导航完成后执行淡入动画
            if (_fadeInStoryboard != null)
            {
                // 先设置为透明，然后执行淡入动画
                this.Opacity = 0;

                // 使用延迟确保内容已加载
                this.Dispatcher.BeginInvoke(new Action(() => {
                    _fadeInStoryboard.Begin();
                    
                    // 发送导航完成消息，通知其他组件更新状态
                    if (_messagingService != null && e.Content != null)
                    {
                        Type? pageType = null;
                        
                        // 根据页面内容类型判断是哪个页面
                        if (e.Content is PlaylistPage)
                        {
                            pageType = typeof(PlaylistPage);
                        }
                        else if (e.Content is SettingsPage)
                        {
                            pageType = typeof(SettingsPage);
                        }
                        else if (e.Content is PlayerPage)
                        {
                            pageType = typeof(PlayerPage);
                        }
                        else if (e.Content is SingerPage)
                        {
                            pageType = typeof(SingerPage);
                        }
                        else if (e.Content is AlbumPage)
                        {
                            pageType = typeof(AlbumPage);
                        }
                        else if (e.Content is HeartPage)
                        {
                            pageType = typeof(HeartPage);
                        }
                        else if (e.Content is PlaylistDetailPage)
                        {
                            pageType = typeof(PlaylistDetailPage);
                        }
                        if (pageType != null)
                        {
                            _messagingService.Send(new NavigationCompletedMessage(pageType));
                            System.Diagnostics.Debug.WriteLine($"AnimatedFrame: 发送导航完成消息，页面类型: {pageType.Name}");
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// 重写导航方法，添加动画效果
        /// </summary>
        public new void Navigate(object content)
        {
            if (_fadeOutStoryboard != null && this.Content != null && !_isNavigating)
            {
                // 先执行淡出动画，然后导航
                _isNavigating = true;
                _pendingNavigationContent = content;
                _pendingNavigationSource = null;
                _pendingNavigationExtraData = null;
                _pendingNavigationWithExtraData = false;

                _fadeOutStoryboard.Completed += OnFadeOutCompleted;
                _fadeOutStoryboard.Begin();
            }
            else
            {
                // 如果没有当前内容或正在导航，直接导航
                base.Navigate(content);
            }
        }

        /// <summary>
        /// 重写导航方法，添加动画效果
        /// </summary>
        public new void Navigate(Uri source)
        {
            if (_fadeOutStoryboard != null && this.Content != null && !_isNavigating)
            {
                // 先执行淡出动画，然后导航
                _isNavigating = true;
                _pendingNavigationContent = null;
                _pendingNavigationSource = source;
                _pendingNavigationExtraData = null;
                _pendingNavigationWithExtraData = false;

                _fadeOutStoryboard.Completed += OnFadeOutCompleted;
                _fadeOutStoryboard.Begin();
            }
            else
            {
                // 如果没有当前内容或正在导航，直接导航
                base.Navigate(source);
            }
        }

        /// <summary>
        /// 重写导航方法，添加动画效果
        /// </summary>
        public new void Navigate(object content, object extraData)
        {
            if (_fadeOutStoryboard != null && this.Content != null && !_isNavigating)
            {
                // 先执行淡出动画，然后导航
                _isNavigating = true;
                _pendingNavigationContent = content;
                _pendingNavigationSource = null;
                _pendingNavigationExtraData = extraData;
                _pendingNavigationWithExtraData = true;

                _fadeOutStoryboard.Completed += OnFadeOutCompleted;
                _fadeOutStoryboard.Begin();
            }
            else
            {
                // 如果没有当前内容或正在导航，直接导航
                base.Navigate(content, extraData);
            }
        }

        /// <summary>
        /// 重写返回上一页方法，添加动画效果
        /// </summary>
        public new void GoBack()
        {
            if (_fadeOutStoryboard != null && this.Content != null && !_isNavigating)
            {
                // 先执行淡出动画，然后返回
                _isNavigating = true;
                _pendingNavigationContent = null;
                _pendingNavigationSource = null;
                _pendingNavigationExtraData = null;
                _pendingNavigationWithExtraData = false;
                _pendingGoBack = true;

                _fadeOutStoryboard.Completed += OnFadeOutCompleted;
                _fadeOutStoryboard.Begin();
            }
            else
            {
                // 如果没有当前内容或正在导航，直接返回
                base.GoBack();
            }
        }

        /// <summary>
        /// 重写前进到下一页方法，添加动画效果
        /// </summary>
        public new void GoForward()
        {
            if (_fadeOutStoryboard != null && this.Content != null && !_isNavigating)
            {
                // 先执行淡出动画，然后前进
                _isNavigating = true;
                _pendingNavigationContent = null;
                _pendingNavigationSource = null;
                _pendingNavigationExtraData = null;
                _pendingNavigationWithExtraData = false;
                _pendingGoForward = true;

                _fadeOutStoryboard.Completed += OnFadeOutCompleted;
                _fadeOutStoryboard.Begin();
            }
            else
            {
                // 如果没有当前内容或正在导航，直接前进
                base.GoForward();
            }
        }

        private void OnFadeOutCompleted(object? sender, EventArgs e)
        {
            // 移除事件处理器
            if (_fadeOutStoryboard != null)
            {
                _fadeOutStoryboard.Completed -= OnFadeOutCompleted;
            }

            // 执行实际的导航
            if (_pendingGoBack)
            {
                base.GoBack();
            }
            else if (_pendingGoForward)
            {
                base.GoForward();
            }
            else if (_pendingNavigationWithExtraData && _pendingNavigationContent != null && _pendingNavigationExtraData != null)
            {
                base.Navigate(_pendingNavigationContent, _pendingNavigationExtraData);
            }
            else if (_pendingNavigationContent != null)
            {
                base.Navigate(_pendingNavigationContent);
            }
            else if (_pendingNavigationSource != null)
            {
                base.Navigate(_pendingNavigationSource);
            }

            // 重置导航状态
            _isNavigating = false;
            _pendingNavigationContent = null;
            _pendingNavigationSource = null;
            _pendingNavigationExtraData = null;
            _pendingNavigationWithExtraData = false;
            _pendingGoBack = false;
            _pendingGoForward = false;
        }
    }
}
using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Page;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Navigation
{
    /// <summary>
    /// 导航服务，用于管理页面之间的导航
    /// </summary>
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private AnimatedFrame? _mainFrame;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// 获取当前页面类型
        /// </summary>
        public Type? CurrentPageType => _mainFrame?.Content?.GetType();

        /// <summary>
        /// 设置主框架
        /// </summary>
        /// <param name="frame">主框架</param>
        public void SetMainFrame(Frame frame)
        {
            _mainFrame = frame as AnimatedFrame;
        }

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="pageUri">页面URI</param>
        public void NavigateTo(string pageUri)
        {
            if (_mainFrame != null)
            {
                // 清理当前页面的ViewModel资源
                CleanupCurrentPageViewModel();
                
                // 根据URI类型创建对应的Page实例
                if (pageUri.Contains("HomePage.xaml"))
                {
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var homePage = new HomePage(mainViewModel);
                    _mainFrame.Navigate(homePage);
                }
                else if (pageUri.Contains("SettingsPage.xaml"))
                {
                    var settingsViewModel = _serviceProvider.GetRequiredService<ISettingsPageViewModel>();
                    var settingsPage = new SettingsPage(settingsViewModel);
                    _mainFrame.Navigate(settingsPage);
                }
                else if (pageUri.Contains("PlayerPage.xaml"))
                {
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var playerPage = new PlayerPage(mainViewModel);
                    _mainFrame.Navigate(playerPage);
                }
                else if (pageUri.Contains("AlbumPage.xaml"))
                {
                    var albumViewModel = _serviceProvider.GetRequiredService<IAlbumViewModel>();
                    var albumPage = new AlbumPage(albumViewModel);
                    albumViewModel.Initialize(); // 确保每次进入页面都重新加载数据
                    _mainFrame.Navigate(albumPage);
                }
                else if (pageUri.Contains("SingerPage.xaml"))
                {
                    var singerViewModel = _serviceProvider.GetRequiredService<ISingerViewModel>();
                    var singerPage = new SingerPage(singerViewModel);
                    singerViewModel.Initialize(); // 确保每次进入页面都重新加载数据
                    _mainFrame.Navigate(singerPage);
                }
                else
                {
                    // 回退到默认的URI导航
                    _mainFrame.Navigate(new Uri(pageUri, UriKind.Relative));
                }
            }
        }

        /// <summary>
        /// 导航到首页
        /// </summary>
        public void NavigateToHome()
        {
            NavigateTo("Page/HomePage.xaml");
        }

        /// <summary>
        /// 导航到设置页面
        /// </summary>
        public void NavigateToSettings()
        {
            NavigateTo("Page/SettingsPage.xaml");
        }

        /// <summary>
        /// 导航到播放页面
        /// </summary>
        public void NavigateToPlayer()
        {
            NavigateTo("Page/PlayerPage.xaml");
        }

        /// <summary>
        /// 导航到歌手页面
        /// </summary>
        public void NavigateToSinger()
        {
            NavigateTo("Page/SingerPage.xaml");
        }

        /// <summary>
        /// 导航到专辑页面
        /// </summary>
        public void NavigateToAlbum()
        {
            NavigateTo("Page/AlbumPage.xaml");
        }

        /// <summary>
        /// 检查是否可以返回上一页
        /// </summary>
        /// <returns>如果可以返回返回true，否则返回false</returns>
        public bool CanGoBack()
        {
            return _mainFrame != null && _mainFrame.CanGoBack;
        }

        /// <summary>
        /// 返回上一页
        /// </summary>
        //public void GoBack()
        //{
        //    if (_mainFrame != null && _mainFrame.CanGoBack)
        //    {
        //        _mainFrame.GoBack();
        //    }
        //}

        /// <summary>
        /// 前进到下一页
        /// </summary>
        public void GoForward()
        {
            if (_mainFrame != null && _mainFrame.CanGoForward)
            {
                // 清理当前页面的ViewModel资源
                CleanupCurrentPageViewModel();
                _mainFrame.GoForward();
            }
        }
        
        /// <summary>
        /// 返回上一页
        /// </summary>
        public void GoBack()
        {
            if (_mainFrame != null && _mainFrame.CanGoBack)
            {
                // 清理当前页面的ViewModel资源
                CleanupCurrentPageViewModel();
                _mainFrame.GoBack();
            }
        }
        
        /// <summary>
        /// 清理当前页面的ViewModel资源
        /// </summary>
        private void CleanupCurrentPageViewModel()
        {
            if (_mainFrame == null || _mainFrame.Content == null)
                return;
            
            try
            {
                // 获取当前页面的DataContext
                var page = _mainFrame.Content as System.Windows.Controls.Page;
                if (page != null)
                {
                    // 检查是否是SingerPage
                    if (page is SingerPage singerPage && singerPage.DataContext is ISingerViewModel singerViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine("NavigationService: 清理SingerViewModel资源");
                        singerViewModel.Cleanup();
                        return;
                    }
                    
                    // 检查是否是AlbumPage
                    if (page is AlbumPage albumPage && albumPage.DataContext is IAlbumViewModel albumViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine("NavigationService: 清理AlbumViewModel资源");
                        albumViewModel.Cleanup();
                        return;
                    }
                    
                    // 检查是否是HomePage（包含PlaylistControl）
                    if (page is HomePage homePage)
                    {
                        // 获取MainViewModel，它包含PlaylistViewModel
                        if (homePage.DataContext is IMainViewModel mainViewModel)
                        {
                            System.Diagnostics.Debug.WriteLine("NavigationService: 清理PlaylistViewModel资源");
                            mainViewModel.PlaylistViewModel.Cleanup();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavigationService: 清理当前页面ViewModel资源失败: {ex.Message}");
            }
        }
    }
}
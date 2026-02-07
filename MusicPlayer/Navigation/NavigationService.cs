using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Core.Models;
using MusicPlayer.Page;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Navigation
{
    /// <summary>
    /// 导航服务，用于管理页面之间的导航（已修复内存泄漏）
    /// </summary>
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private AnimatedFrame? _mainFrame;
        private List<string> _navigationHistory = new List<string>();
        private int _currentIndex = -1;
        
        // 当前歌单详情页导航参数
        private PlaylistDetailParams? _currentPlaylistDetailParams;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Type? CurrentPageType => _mainFrame?.Content?.GetType();
        
        public string? CurrentPageUri => _currentIndex >= 0 && _currentIndex < _navigationHistory.Count ? _navigationHistory[_currentIndex] : null;
        public string? PreviousPageUri => _currentIndex > 0 ? _navigationHistory[_currentIndex - 1] : null;
        public string? NextPageUri => _currentIndex < _navigationHistory.Count - 1 ? _navigationHistory[_currentIndex + 1] : null;

        public void SetMainFrame(Frame frame)
        {
            _mainFrame = frame as AnimatedFrame;
        }

        // 当前活跃的页面实例
        private System.Windows.Controls.Page? _currentPage;

        public void NavigateTo(string pageUri)
        {
            if (_mainFrame != null)
            {
                System.Diagnostics.Debug.WriteLine($"NavigationService: 开始导航到页面: {pageUri}");
                 
                // 1. 强制清理当前页面资源
                ForceCleanupCurrentPage();
                
                // 2. 清理导航历史记录（Frame内置的）
                while (_mainFrame.CanGoBack)
                {
                    _mainFrame.RemoveBackEntry();
                }
                
                // 3. 更新导航历史
                // 如果当前不是在列表末尾，截断列表（新导航会清除前进历史）
                if (_currentIndex < _navigationHistory.Count - 1)
                {
                    _navigationHistory.RemoveRange(_currentIndex + 1, _navigationHistory.Count - _currentIndex - 1);
                }
                // 添加新页面到历史记录
                _navigationHistory.Add(pageUri);
                _currentIndex = _navigationHistory.Count - 1;
                
                // 4. 创建页面和对应的Transient ViewModel
                System.Windows.Controls.Page? newPage = null;
                
                if (pageUri.Contains("PlaylistPage.xaml"))
                {
                    // 获取Transient ViewModel
                    var playlistViewModel = _serviceProvider.GetRequiredService<IPlaylistViewModel>();
                    newPage = new PlaylistPage(playlistViewModel);
                    playlistViewModel.Initialize();
                }
                else if (pageUri.Contains("PlaylistDetailPage.xaml"))
                {
                    var playlistDetailViewModel = _serviceProvider.GetRequiredService<IPlaylistDetailViewModel>();
                    newPage = new PlaylistDetailPage(playlistDetailViewModel);
                    // 将导航参数传递给ViewModel的Initialize方法
                    (playlistDetailViewModel as MusicPlayer.ViewModels.PlaylistDetailViewModel)?.Initialize(_currentPlaylistDetailParams);
                    // 清空导航参数，避免影响下次导航
                    _currentPlaylistDetailParams = null;
                }
                else if (pageUri.Contains("SettingsPage.xaml"))
                {
                    var settingsViewModel = _serviceProvider.GetRequiredService<ISettingsPageViewModel>();
                    newPage = new SettingsPage(settingsViewModel);
                    settingsViewModel.Initialize();
                }
                else if (pageUri.Contains("PlayerPage.xaml"))
                {
                    var centerContentViewModel = _serviceProvider.GetRequiredService<ICenterContentViewModel>();
                    newPage = new PlayerPage(centerContentViewModel);
                    centerContentViewModel.Initialize();
                }
                else if (pageUri.Contains("AlbumPage.xaml"))
                {
                    var albumViewModel = _serviceProvider.GetRequiredService<IAlbumViewModel>();
                    newPage = new AlbumPage(albumViewModel);
                    albumViewModel.Initialize();
                }
                else if (pageUri.Contains("SingerPage.xaml"))
                {
                    var singerViewModel = _serviceProvider.GetRequiredService<ISingerViewModel>();
                    newPage = new SingerPage(singerViewModel);
                    singerViewModel.Initialize();
                }
                else if (pageUri.Contains("HeartPage.xaml"))
                {
                    var heartViewModel = _serviceProvider.GetRequiredService<IHeartViewModel>();
                    newPage = new HeartPage(heartViewModel);
                    heartViewModel.Initialize();
                }
                
                // 5. 执行导航
                if (newPage != null)
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationService: 导航到新页面: {newPage.GetType().Name}");
                    _mainFrame.Navigate(newPage);
                    // 更新当前页面引用
                    _currentPage = newPage;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationService: 通过URI导航: {pageUri}");
                    _mainFrame.Navigate(new Uri(pageUri, UriKind.Relative));
                }
                
                System.Diagnostics.Debug.WriteLine($"NavigationService: 导航完成到页面: {pageUri}");
            }
        }

        public void NavigateToPlaylist() => NavigateTo("Page/PlaylistPage.xaml");
        public void NavigateToSettings() => NavigateTo("Page/SettingsPage.xaml");
        public void NavigateToPlayer() => NavigateTo("Page/PlayerPage.xaml");
        public void NavigateToSinger() => NavigateTo("Page/SingerPage.xaml");
        public void NavigateToAlbum() => NavigateTo("Page/AlbumPage.xaml");
        public void NavigateToHeart() => NavigateTo("Page/HeartPage.xaml");
        
        /// <summary>
        /// 导航到歌单详情页，支持传递导航参数
        /// </summary>
        /// <param name="playlistDetailParams">导航参数</param>
        public void NavigateToPlaylistDetail(PlaylistDetailParams? playlistDetailParams = null)
        {
            // 保存导航参数
            _currentPlaylistDetailParams = playlistDetailParams;
            
            // 调用原有的导航方法
            NavigateTo("Page/PlaylistDetailPage.xaml");
        }
        
        /// <summary>
        /// 导航到歌单详情页（默认重载）
        /// </summary>
        public void NavigateToPlaylistDetail() => NavigateToPlaylistDetail(null);


        /// <summary>
        /// 强制清理当前页面资源
        /// </summary>
        private void ForceCleanupCurrentPage()
        {
            System.Diagnostics.Debug.WriteLine("NavigationService: 开始执行ForceCleanupCurrentPage方法");
            
            // 清理_currentPage引用的页面
            if (_currentPage != null)
            {
                System.Diagnostics.Debug.WriteLine($"NavigationService: 清理当前活跃页面: {_currentPage.GetType().Name}");
                
                try
                {
                    // 强制释放页面资源
                    if (_currentPage is IDisposable disposablePage)
                    {
                        disposablePage.Dispose();
                        System.Diagnostics.Debug.WriteLine($"NavigationService: 已释放页面: {_currentPage.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationService: 清理页面时出错: {ex.Message}");
                }
                finally
                {
                    _currentPage = null;
                }
            }
            
            // 清理Frame中的当前内容
            if (_mainFrame != null && _mainFrame.Content != null)
            {
                System.Diagnostics.Debug.WriteLine($"NavigationService: 清理Frame中的当前内容");
                CleanupCurrentPage();
            }
            
            System.Diagnostics.Debug.WriteLine("NavigationService: ForceCleanupCurrentPage方法执行完成");
        }

        public bool CanGoBack() => _currentIndex > 0;
        public void GoBack()
        {
            if (_mainFrame != null && CanGoBack())
            {
                System.Diagnostics.Debug.WriteLine("NavigationService: 开始执行GoBack方法");
                
                // 强制清理当前页面资源
                ForceCleanupCurrentPage();
                
                // 移动到上一页
                _currentIndex--;
                string previousPage = _navigationHistory[_currentIndex];
                
                System.Diagnostics.Debug.WriteLine($"NavigationService: 导航到上一页: {previousPage}");
                
                // 根据上一页URI导航
                System.Windows.Controls.Page? newPage = null;
                
                if (previousPage.Contains("PlaylistPage.xaml"))
                {
                    var playlistViewModel = _serviceProvider.GetRequiredService<IPlaylistViewModel>();
                    newPage = new PlaylistPage(playlistViewModel);
                    playlistViewModel.Initialize();
                }
                else if (previousPage.Contains("PlaylistDetailPage.xaml"))
                {
                    var playlistDetailViewModel = _serviceProvider.GetRequiredService<IPlaylistDetailViewModel>();
                    newPage = new PlaylistDetailPage(playlistDetailViewModel);
                    playlistDetailViewModel.Initialize();
                }
                else if (previousPage.Contains("SettingsPage.xaml"))
                {
                    var settingsViewModel = _serviceProvider.GetRequiredService<ISettingsPageViewModel>();
                    newPage = new SettingsPage(settingsViewModel);
                    settingsViewModel.Initialize();
                }
                else if (previousPage.Contains("PlayerPage.xaml"))
                {
                    var centerContentViewModel = _serviceProvider.GetRequiredService<ICenterContentViewModel>();
                    newPage = new PlayerPage(centerContentViewModel);
                    centerContentViewModel.Initialize();
                }
                else if (previousPage.Contains("AlbumPage.xaml"))
                {
                    var albumViewModel = _serviceProvider.GetRequiredService<IAlbumViewModel>();
                    newPage = new AlbumPage(albumViewModel);
                    albumViewModel.Initialize();
                }
                else if (previousPage.Contains("SingerPage.xaml"))
                {
                    var singerViewModel = _serviceProvider.GetRequiredService<ISingerViewModel>();
                    newPage = new SingerPage(singerViewModel);
                    singerViewModel.Initialize();
                }
                else if (previousPage.Contains("HeartPage.xaml"))
                {
                    var heartViewModel = _serviceProvider.GetRequiredService<IHeartViewModel>();
                    newPage = new HeartPage(heartViewModel);
                    heartViewModel.Initialize();
                }
                
                // 执行导航
                if (newPage != null)
                {
                    _mainFrame.Navigate(newPage);
                    // 更新当前页面引用
                    _currentPage = newPage;
                }
                
                System.Diagnostics.Debug.WriteLine("NavigationService: GoBack方法执行完成");
            }
        }

        public bool CanGoForward() => _currentIndex < _navigationHistory.Count - 1;
        public void GoForward()
        {
            if (_mainFrame != null && CanGoForward())
            {
                // 清理当前页面资源
                CleanupCurrentPage();
                
                // 移动到下一页
                _currentIndex++;
                string nextPage = _navigationHistory[_currentIndex];
                
                // 根据下一页URI导航
                if (nextPage.Contains("PlaylistPage.xaml"))
                {
                    var playlistViewModel = _serviceProvider.GetRequiredService<IPlaylistViewModel>();
                    var playlistPage = new PlaylistPage(playlistViewModel);
                    playlistViewModel.Initialize();
                    _mainFrame.Navigate(playlistPage);
                }
                else if (nextPage.Contains("PlaylistDetailPage.xaml"))
                {
                    var playlistDetailViewModel = _serviceProvider.GetRequiredService<IPlaylistDetailViewModel>();
                    var playlistDetailPage = new PlaylistDetailPage(playlistDetailViewModel);
                    playlistDetailViewModel.Initialize();
                    _mainFrame.Navigate(playlistDetailPage);
                }
                else if (nextPage.Contains("SettingsPage.xaml"))
                {
                    var settingsViewModel = _serviceProvider.GetRequiredService<ISettingsPageViewModel>();
                    var settingsPage = new SettingsPage(settingsViewModel);
                    settingsViewModel.Initialize();
                    _mainFrame.Navigate(settingsPage);
                }
                else if (nextPage.Contains("PlayerPage.xaml"))
                {
                    var centerContentViewModel = _serviceProvider.GetRequiredService<ICenterContentViewModel>();
                    var playerPage = new PlayerPage(centerContentViewModel);
                    centerContentViewModel.Initialize();
                    _mainFrame.Navigate(playerPage);
                }
                else if (nextPage.Contains("AlbumPage.xaml"))
                {
                    var albumViewModel = _serviceProvider.GetRequiredService<IAlbumViewModel>();
                    var albumPage = new AlbumPage(albumViewModel);
                    albumViewModel.Initialize();
                    _mainFrame.Navigate(albumPage);
                }
                else if (nextPage.Contains("SingerPage.xaml"))
                {
                    var singerViewModel = _serviceProvider.GetRequiredService<ISingerViewModel>();
                    var singerPage = new SingerPage(singerViewModel);
                    singerViewModel.Initialize();
                    _mainFrame.Navigate(singerPage);
                }
                else if (nextPage.Contains("HeartPage.xaml"))
                {
                    var heartViewModel = _serviceProvider.GetRequiredService<IHeartViewModel>();
                    var heartPage = new HeartPage(heartViewModel);
                    heartViewModel.Initialize();
                    _mainFrame.Navigate(heartPage);
                }
            }
        }
        
        /// <summary>
        /// 清理当前页面资源
        /// </summary>
        private void CleanupCurrentPage()
        {
            if (_mainFrame == null || _mainFrame.Content == null)
            {
                return;
            }

            
            try
            {
                // 获取当前页面
                var currentPage = _mainFrame.Content;
                System.Diagnostics.Debug.WriteLine($"NavigationService: 开始清理页面资源: {currentPage.GetType().Name}");

                // 1. 清理页面资源（如果页面实现了IDisposable）
                if (currentPage is IDisposable disposablePage)
                {
                    disposablePage.Dispose();
                    System.Diagnostics.Debug.WriteLine($"NavigationService: 已释放页面资源: {currentPage.GetType().Name}");
                }

                // 2. 清理ViewModel资源（如果页面有ViewModel并且实现了IViewModelLifecycle）
                // 这里需要根据不同页面类型处理，因为每个页面的ViewModel属性名可能不同
                if (currentPage is PlaylistPage playlistPage)
                {
                    if (playlistPage.DataContext is MusicPlayer.ViewModels.ObservableObject homeViewModel)
                    {
                        homeViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理PlaylistPage的ViewModel");
                    }
                }
                else if (currentPage is PlaylistDetailPage playlistDetailPage)
                {
                    if (playlistDetailPage.DataContext is MusicPlayer.ViewModels.ObservableObject playlistDetailViewModel)
                    {
                        playlistDetailViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理PlaylistDetailPage的ViewModel");
                    }
                }
                else if (currentPage is SettingsPage settingsPage)
                {
                    if (settingsPage.DataContext is MusicPlayer.ViewModels.ObservableObject settingsViewModel)
                    {
                        settingsViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理SettingsPage的ViewModel");
                    }
                }
                else if (currentPage is PlayerPage playerPage)
                {
                    if (playerPage.DataContext is MusicPlayer.ViewModels.ObservableObject playerViewModel)
                    {
                        playerViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理PlayerPage的ViewModel");
                    }
                }
                else if (currentPage is AlbumPage albumPage)
                {
                    if (albumPage.DataContext is MusicPlayer.ViewModels.ObservableObject albumViewModel)
                    {
                        albumViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理AlbumPage的ViewModel");
                    }
                }
                else if (currentPage is SingerPage singerPage)
                {
                    if (singerPage.DataContext is MusicPlayer.ViewModels.ObservableObject singerViewModel)
                    {
                        singerViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理SingerPage的ViewModel");
                    }
                }
                else if (currentPage is HeartPage heartPage)
                {
                    if (heartPage.DataContext is MusicPlayer.ViewModels.ObservableObject heartViewModel)
                    {
                        heartViewModel.Cleanup();
                        System.Diagnostics.Debug.WriteLine("NavigationService: 已清理HeartPage的ViewModel");
                    }
                }
                
                // 3. 重置页面引用
                System.Diagnostics.Debug.WriteLine("NavigationService: 重置Frame的Content");
                _mainFrame.Content = null;
                
                // 4. 强制垃圾回收
                System.Diagnostics.Debug.WriteLine("NavigationService: 执行垃圾回收");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                System.Diagnostics.Debug.WriteLine("NavigationService: 页面资源清理完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavigationService: 清理页面资源失败: {ex.Message}");
            }
        }
    }
}
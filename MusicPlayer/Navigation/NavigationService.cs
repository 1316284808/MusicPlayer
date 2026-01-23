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

        public void NavigateTo(string pageUri)
        {
            if (_mainFrame != null)
            {
                 
                // 1. 清理导航历史记录（Frame内置的）
                while (_mainFrame.CanGoBack)
                {
                    _mainFrame.RemoveBackEntry();
                }
                
                // 2. 清理当前页面资源
                CleanupCurrentPage();

                // 3. 更新导航历史
                // 如果当前不是在列表末尾，截断列表（新导航会清除前进历史）
                if (_currentIndex < _navigationHistory.Count - 1)
                {
                    _navigationHistory.RemoveRange(_currentIndex + 1, _navigationHistory.Count - _currentIndex - 1);
                }
                // 添加新页面到历史记录
                _navigationHistory.Add(pageUri);
                _currentIndex = _navigationHistory.Count - 1;
                if (pageUri.Contains("PlaylistPage.xaml"))
                {
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var playlistPage = new PlaylistPage(mainViewModel);
                    _mainFrame.Navigate(playlistPage);
                }
                else if (pageUri.Contains("PlaylistDetailPage.xaml"))
                {
                    var playlistDetailViewModel = _serviceProvider.GetRequiredService<IPlaylistDetailViewModel>();
                    var playlistDetailPage = new PlaylistDetailPage(playlistDetailViewModel);
                    // 将导航参数传递给ViewModel的Initialize方法
                    (playlistDetailViewModel as MusicPlayer.ViewModels.PlaylistDetailViewModel)?.Initialize(_currentPlaylistDetailParams);
                    _mainFrame.Navigate(playlistDetailPage);
                    // 清空导航参数，避免影响下次导航
                    _currentPlaylistDetailParams = null;
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
                    albumViewModel.Initialize();
                    _mainFrame.Navigate(albumPage);
                }
                else if (pageUri.Contains("SingerPage.xaml"))
                {
                    var singerViewModel = _serviceProvider.GetRequiredService<ISingerViewModel>();
                    var singerPage = new SingerPage(singerViewModel);
                    singerViewModel.Initialize();
                    _mainFrame.Navigate(singerPage);
                }
                else if (pageUri.Contains("HeartPage.xaml"))
                {
                    var heartViewModel = _serviceProvider.GetRequiredService<IHeartViewModel>();
                    var heartPage = new HeartPage(heartViewModel);
                    heartViewModel.Initialize();
                    _mainFrame.Navigate(heartPage);
                }
                else
                {
                    _mainFrame.Navigate(new Uri(pageUri, UriKind.Relative));
                }
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


        public bool CanGoBack() => _currentIndex > 0;
        public void GoBack()
        {
            if (_mainFrame != null && CanGoBack())
            {
                // 清理当前页面资源
                CleanupCurrentPage();
                
                // 移动到上一页
                _currentIndex--;
                string previousPage = _navigationHistory[_currentIndex];
                
                // 根据上一页URI导航
                if (previousPage.Contains("PlaylistPage.xaml"))
                {
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var playlistPage = new PlaylistPage(mainViewModel);
                    _mainFrame.Navigate(playlistPage);
                }
                else if (previousPage.Contains("PlaylistDetailPage.xaml"))
                {
                    var playlistDetailViewModel = _serviceProvider.GetRequiredService<IPlaylistDetailViewModel>();
                    var playlistDetailPage = new PlaylistDetailPage(playlistDetailViewModel);
                    playlistDetailViewModel.Initialize();
                    _mainFrame.Navigate(playlistDetailPage);
                }
                else if (previousPage.Contains("SettingsPage.xaml"))
                {
                    var settingsViewModel = _serviceProvider.GetRequiredService<ISettingsPageViewModel>();
                    var settingsPage = new SettingsPage(settingsViewModel);
                    _mainFrame.Navigate(settingsPage);
                }
                else if (previousPage.Contains("PlayerPage.xaml"))
                {
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var playerPage = new PlayerPage(mainViewModel);
                    _mainFrame.Navigate(playerPage);
                }
                else if (previousPage.Contains("AlbumPage.xaml"))
                {
                    var albumViewModel = _serviceProvider.GetRequiredService<IAlbumViewModel>();
                    var albumPage = new AlbumPage(albumViewModel);
                    albumViewModel.Initialize();
                    _mainFrame.Navigate(albumPage);
                }
                else if (previousPage.Contains("SingerPage.xaml"))
                {
                    var singerViewModel = _serviceProvider.GetRequiredService<ISingerViewModel>();
                    var singerPage = new SingerPage(singerViewModel);
                    singerViewModel.Initialize();
                    _mainFrame.Navigate(singerPage);
                }
                else if (previousPage.Contains("HeartPage.xaml"))
                {
                    var heartViewModel = _serviceProvider.GetRequiredService<IHeartViewModel>();
                    var heartPage = new HeartPage(heartViewModel);
                    heartViewModel.Initialize();
                    _mainFrame.Navigate(heartPage);
                }
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
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var playlistPage = new PlaylistPage(mainViewModel);
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
                    _mainFrame.Navigate(settingsPage);
                }
                else if (nextPage.Contains("PlayerPage.xaml"))
                {
                    var mainViewModel = _serviceProvider.GetRequiredService<IMainViewModel>();
                    var playerPage = new PlayerPage(mainViewModel);
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

                // 1. 清理页面资源（如果页面实现了IDisposable）
               

                // 2. 清理ViewModel资源（如果页面有ViewModel并且实现了IViewModelLifecycle）
            // 这里需要根据不同页面类型处理，因为每个页面的ViewModel属性名可能不同
            if (currentPage is PlaylistPage playlistPage)
            {
                if (playlistPage.DataContext is MusicPlayer.ViewModels.ObservableObject homeViewModel)
                {
                    homeViewModel.Cleanup();
                }
            }
            else if (currentPage is PlaylistDetailPage playlistDetailPage)
            {
                if (playlistDetailPage.DataContext is MusicPlayer.ViewModels.ObservableObject playlistDetailViewModel)
                {
                    playlistDetailViewModel.Cleanup();
                }
            }
            else if (currentPage is SettingsPage settingsPage)
            {
                if (settingsPage.DataContext is MusicPlayer.ViewModels.ObservableObject settingsViewModel)
                {
                    settingsViewModel.Cleanup();
                }
            }
           
            else if (currentPage is AlbumPage albumPage)
            {
                if (albumPage.DataContext is MusicPlayer.ViewModels.ObservableObject albumViewModel)
                {
                    albumViewModel.Cleanup();
                }
            }
            else if (currentPage is SingerPage singerPage)
            {
                if (singerPage.DataContext is MusicPlayer.ViewModels.ObservableObject singerViewModel)
                {
                    singerViewModel.Cleanup();
                }
            }
            else if (currentPage is HeartPage heartPage)
            {
                if (heartPage.DataContext is MusicPlayer.ViewModels.ObservableObject heartViewModel)
                {
                    heartViewModel.Cleanup();
                }
            }
                if (currentPage is IDisposable disposablePage)
                {
                    disposablePage.Dispose();
                }
                // 3. 重置页面引用
                _mainFrame.Content = null;
                
                // 4. 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavigationService: 清理页面资源失败: {ex.Message}");
            }
        }
    }
}
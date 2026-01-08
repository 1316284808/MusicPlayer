using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// AlbumPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlbumPage : System.Windows.Controls.Page
    {
        private readonly IAlbumViewModel _albumViewModel;

        /// <summary>
        /// 构造函数，使用依赖注入获取ViewModel
        /// </summary>
        public AlbumPage()
        {
            InitializeComponent();
            
            // 通过依赖注入获取ViewModel
            var app = System.Windows.Application.Current as App;
            if (app?.ServiceProvider != null)
            {
                _albumViewModel = app.ServiceProvider.GetRequiredService<IAlbumViewModel>();
                DataContext = _albumViewModel;
                
                // 初始化数据
                _albumViewModel.Initialize();
            }
        }
    }
}

using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Helper;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// SingerControl.xaml 的交互逻辑
    /// </summary>
    public partial class SingerControl : UserControl
    {
        private readonly ISingerViewModel _singerViewModel;

        /// <summary>
        /// 构造函数，使用依赖注入获取ViewModel
        /// </summary>
        public SingerControl()
        {
            InitializeComponent();

            // 通过依赖注入获取ViewModel
            var app = System.Windows.Application.Current as App;
            if (app?.ServiceProvider != null)
            {
                _singerViewModel = app.ServiceProvider.GetRequiredService<ISingerViewModel>();

                //先设置DataContext，确保UI绑定生效
               DataContext = _singerViewModel;
                System.Diagnostics.Debug.WriteLine("SingerControl: 获取到SingerViewModel，DataContext已设置");

                //直接初始化数据
                _singerViewModel.Initialize();
                System.Diagnostics.Debug.WriteLine($"SingerControl: 初始化完成，歌手数量: {_singerViewModel.SingerCount}");

               // 页面加载后，手动触发一次SingerAlbumArtBehavior重新检查
                this.Loaded += (sender, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("SingerControl: 页面加载完成，手动触发封面加载");
                   // 延迟一小段时间，确保所有UI元素都已初始化
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        var listBox = this.FindName("SingerListBox") as System.Windows.Controls.ListBox;
                        if (listBox != null)
                        {
                            System.Diagnostics.Debug.WriteLine("SingerControl: 找到ListBox，手动触发封面加载");
                           // 重新附加行为，触发加载
                            SingerAlbumArtBehavior.SetIsEnabled(listBox, false);
                            SingerAlbumArtBehavior.SetIsEnabled(listBox, true);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };
            }
        }
    }
}

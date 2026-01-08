using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using MusicPlayer.Core.Models;
namespace MusicPlayer.ViewModels
{
    

    /// <summary>
    /// 歌手页面视图模型接口
    /// </summary>
    public interface ISingerViewModel : IViewModel
    {
        /// <summary>
        /// 过滤后的歌手列表
        /// </summary>
        ObservableCollection<SingerInfo> FilteredSingers { get; }

        /// <summary>
        /// 歌手列表
        /// </summary>
        ObservableCollection<SingerInfo> Singers { get; }

        /// <summary>
        /// 歌手总数
        /// </summary>
        int SingerCount { get; }

        /// <summary>
        /// 搜索文本
        /// </summary>
        string SearchText { get; set; }

        /// <summary>
        /// 搜索框是否展开
        /// </summary>
        bool IsSearchExpanded { get; set; }

        /// <summary>
        /// 播放歌手歌曲命令
        /// </summary>
        ICommand PlaySingerCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 加载歌手数据
        /// </summary>
        void LoadSingers();

        /// <summary>
        /// 刷新歌手数据
        /// </summary>
        void RefreshSingers();
    }
}
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using MusicPlayer.Core.Models;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 专辑页面视图模型接口
    /// </summary>
    public interface IAlbumViewModel : IViewModel
    {
        /// <summary>
        /// 过滤后的专辑列表
        /// </summary>
        ObservableCollection<AlbumInfo> FilteredAlbums { get; }

        /// <summary>
        /// 专辑列表
        /// </summary>
        ObservableCollection<AlbumInfo> Albums { get; }

        /// <summary>
        /// 专辑总数
        /// </summary>
        int AlbumCount { get; }

        /// <summary>
        /// 搜索文本
        /// </summary>
        string SearchText { get; set; }

        /// <summary>
        /// 搜索框是否展开
        /// </summary>
        bool IsSearchExpanded { get; set; }

        /// <summary>
        /// 播放专辑歌曲命令
        /// </summary>
        ICommand PlayAlbumCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 索引列表
        /// </summary>
        List<string> IndexList { get; }

        /// <summary>
        /// 当前选中的索引
        /// </summary>
        string CurrentIndex { get; set; }

        /// <summary>
        /// 加载专辑数据
        /// </summary>
        void LoadAlbums();

        /// <summary>
        /// 刷新专辑数据
        /// </summary>
        void RefreshAlbums();
    }
}
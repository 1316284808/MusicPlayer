using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放列表模型类 - 表示用户创建的歌曲分组
    /// 使用MVVM工具包实现属性变更通知，支持数据绑定
    /// </summary>
    public partial class Playlist : ObservableObject
    {
        /// <summary>播放列表ID（数据库主键）</summary>
        [ObservableProperty]
        private int _id = -1;

        /// <summary>播放列表名称</summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>播放列表描述</summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>创建时间</summary>
        [ObservableProperty]
        private DateTime _createdTime = DateTime.Now;

        /// <summary>更新时间</summary>
        [ObservableProperty]
        private DateTime _updatedTime = DateTime.Now;

        /// <summary>是否为默认播放列表</summary>
        [ObservableProperty]
        private bool _isDefault = false;

        /// <summary>歌曲数量（非持久化，运行时计算）</summary>
        [ObservableProperty]
        private int _songCount = 0;

        /// <summary>是否正在播放（非持久化，运行时计算）</summary>
        [ObservableProperty]
        private bool _isPlaying = false;
    }
}
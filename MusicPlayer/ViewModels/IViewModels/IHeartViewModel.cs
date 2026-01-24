using System.Windows.Input;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌单页面视图模型接口
    /// </summary>
    public interface IHeartViewModel : IViewModel
    {
        /// <summary>
        /// 添加音乐命令
        /// </summary>
        ICommand AddMusicCommand { get; }
        
        /// <summary>
        /// 选择目录命令
        /// </summary>
        ICommand SelectDirectoryCommand { get; }
        
        /// <summary>
        /// 从文件夹导入歌曲
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        void ImportSongsFromFolder(string folderPath);
    }
}
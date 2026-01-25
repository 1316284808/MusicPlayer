using System;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 对话框服务接口 - 抽象UI对话框操作
    /// 解决Service层直接依赖MessageBox的MVVM违规问题
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// 显示信息对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户确认结果</returns>
        Task<bool> ShowInformationAsync(string message, string title = "信息");

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户确认结果</returns>
        Task<bool> ShowWarningAsync(string message, string title = "警告");

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户确认结果</returns>
        Task<bool> ShowErrorAsync(string message, string title = "错误");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "确认");

        /// <summary>
        /// 同步显示信息对话框（向后兼容）
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        void ShowInformation(string message, string title = "信息");
        
        /// <summary>
        /// 显示新建歌单对话框
        /// </summary>
        /// <returns>返回歌单名称和描述，如果取消则返回null</returns>
        Task<string[]> ShowCreatePlaylistDialogAsync(string title);

        /// <summary>
        /// 显示修改歌单对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="currentName">当前歌单名称</param>
        /// <param name="currentDescription">当前歌单描述</param>
        /// <returns>返回修改后的歌单名称和描述，如果取消则返回null</returns>
        Task<string[]> ShowEditPlaylistDialogAsync(string title, string currentName, string currentDescription);
    }
}
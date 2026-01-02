using System.ComponentModel;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// ViewModel基础接口 - 定义所有ViewModel的通用契约
    /// 让View层依赖抽象而非具体实现，符合依赖倒置原则
    /// </summary>
    public interface IViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 视图模型初始化方法
        /// 在设置DataContext后调用，用于初始化状态和订阅事件
        /// </summary>
        void Initialize();

        /// <summary>
        /// 视图模型清理方法
        /// 在View卸载时调用，用于释放资源和取消订阅事件
        /// </summary>
        void Cleanup();
    }
}
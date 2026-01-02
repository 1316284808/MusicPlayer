using System.ComponentModel;
using System.Runtime.CompilerServices;
using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// ViewModel基类 - 统一实现INotifyPropertyChanged接口和IViewModelLifecycle接口
    /// 提供属性变更通知的通用实现，消除重复代码
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged, IViewModelLifecycle
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 视图模型初始化方法
        /// 子类可以重写此方法实现特定的初始化逻辑
        /// </summary>
        public virtual void Initialize()
        {
            // 默认实现为空，子类可重写
        }

        /// <summary>
        /// 视图模型清理方法
        /// 子类可以重写此方法实现特定的清理逻辑
        /// </summary>
        public virtual void Cleanup()
        {
            // 默认实现为空，子类可重写
        }
    }
}

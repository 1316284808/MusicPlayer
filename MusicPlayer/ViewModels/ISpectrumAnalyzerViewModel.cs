using System.Collections.ObjectModel;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 频谱分析器视图模型接口
    /// 定义频谱分析器的基本功能，支持全局单例模式
    /// </summary>
    public interface ISpectrumAnalyzerViewModel : IViewModel
    {
        /// <summary>
        /// 音频频谱数据集合
        /// </summary>
        ObservableCollection<double> SpectrumData { get; }
        
        /// <summary>
        /// 圆形频谱中心X坐标
        /// </summary>
        double CenterX { get; set; }
        
        /// <summary>
        /// 圆形频谱中心Y坐标
        /// </summary>
        double CenterY { get; set; }
        
        /// <summary>
        /// 圆形频谱内圆半径
        /// </summary>
        double InnerRadius { get; set; }
        
        /// <summary>
        /// 圆形频谱最大条高度
        /// </summary>
        double MaxBarHeight { get; set; }
        
        /// <summary>
        /// 窗口是否最大化
        /// </summary>
        bool IsWindowMaximized { get; set; }
        
        /// <summary>
        /// 激活ViewModel - 开始接收频谱数据
        /// </summary>
        void Activate();
        
        /// <summary>
        /// 停用ViewModel - 停止接收频谱数据
        /// </summary>
        void Deactivate();
    }
}
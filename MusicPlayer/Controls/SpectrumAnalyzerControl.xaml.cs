using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// 频谱分析器控件 - 负责显示音频频谱可视化
    /// 暂时弃用，没有做圆形频谱和线性频谱的打算
    /// </summary>
    public partial class SpectrumAnalyzerControl : UserControl
    {
        public SpectrumAnalyzerControl()
        {
            InitializeComponent();
        }
    }
}
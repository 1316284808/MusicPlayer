using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// ButtonControl.xaml 的交互逻辑
    /// 音质徽标显示控件，根据音频文件格式显示HiRes或SQ徽标
    /// </summary>
    public partial class ButtonControl : UserControl
    {
        /// <summary>
        /// 音频格式依赖属性，用于绑定数据源
        /// </summary>
        public static readonly DependencyProperty AudioFormatProperty =
            DependencyProperty.Register("AudioFormat", typeof(string), typeof(ButtonControl), 
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// 音频格式属性，表示当前音频文件的格式
        /// </summary>
        public string AudioFormat
        {
            get { return (string)GetValue(AudioFormatProperty); }
            set { SetValue(AudioFormatProperty, value); }
        }

        public ButtonControl()
        {
            InitializeComponent();
        }
    }
}

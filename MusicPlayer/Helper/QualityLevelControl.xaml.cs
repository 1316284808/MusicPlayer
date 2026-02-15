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
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// QualityLevelControl.xaml 的交互逻辑
    /// 音质徽标显示控件，根据音频质量等级显示HiRes或SQ徽标
    /// </summary>
    public partial class QualityLevelControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// 音质等级依赖属性，用于绑定数据源
        /// </summary>
        public static readonly DependencyProperty QualityLevelProperty = 
            DependencyProperty.Register("QualityLevel", typeof(AudioQualityLevel), typeof(QualityLevelControl), 
                new PropertyMetadata(AudioQualityLevel.HQ));

        /// <summary>
        /// 音质等级属性，表示当前音频文件的质量等级
        /// </summary>
        public AudioQualityLevel QualityLevel
        {
            get { return (AudioQualityLevel)GetValue(QualityLevelProperty); }
            set { SetValue(QualityLevelProperty, value); }
        }

        public QualityLevelControl()
        {
            InitializeComponent();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

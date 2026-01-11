using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Core.Models
{
    public partial class LyricLine : ObservableObject
    {
        [ObservableProperty]
        private TimeSpan _time;
        
        [ObservableProperty]
        private string _text = string.Empty;
        
        /// <summary>
        /// 当前行的结束时间
        /// </summary>
        [ObservableProperty]
        private TimeSpan _endTime;
        
        /// <summary>
        /// 目标高亮的字索引（实际计算出的高亮位置）
        /// </summary>
        [ObservableProperty]
        private int _targetHighlightedIndex;
        
        /// <summary>
        /// 当前高亮的字索引（用于动画过渡）
        /// </summary>
        [ObservableProperty]
        private int _currentHighlightedIndex;
        
        /// <summary>
        /// 高亮的文本（用于UI绑定）
        /// </summary>
        [ObservableProperty]
        private string _highlightedText;
        
        /// <summary>
        /// 更新高亮索引，添加平滑过渡效果
        /// </summary>
        /// <param name="targetIndex">目标高亮索引</param>
        public void UpdateHighlightSmoothly(int targetIndex)
        {
            TargetHighlightedIndex = targetIndex;
            
            // 计算目标索引和当前索引的差值
            int diff = TargetHighlightedIndex - CurrentHighlightedIndex;
            
            // 添加缓冲效果，每次只移动1-2个字符，实现平滑过渡
            if (Math.Abs(diff) > 2)
            {
                // 大差值时，移动2个字符
                CurrentHighlightedIndex += diff > 0 ? 2 : -2;
            }
            else if (Math.Abs(diff) > 0)
            {
                // 小差值时，移动1个字符
                CurrentHighlightedIndex += diff > 0 ? 1 : -1;
            }
            
            // 确保索引在有效范围内
            CurrentHighlightedIndex = Math.Max(0, Math.Min(Text.Length, CurrentHighlightedIndex));
        }
    }
}
using System;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 排序选项数据模型
    /// </summary>
    public class SortOption
    {
        /// <summary>
        /// 排序规则
        /// </summary>
        public SortRule Value { get; set; }
        
        /// <summary>
        /// 显示文本
        /// </summary>
        public string Display { get; set; } = string.Empty;
        
        /// <summary>
        /// 重写ToString方法，便于显示
        /// </summary>
        public override string ToString()
        {
            return Display;
        }
    }
}
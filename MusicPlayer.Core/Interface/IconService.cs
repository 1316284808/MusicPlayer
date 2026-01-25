using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 图标服务 - 提供Segoe Fluent Icons字体映射功能
    /// </summary>
    public static class IconService
    {
        /// <summary>
        /// 获取图标Unicode字符
        /// </summary>
        /// <param name="kind">图标类型</param>
        /// <returns>对应的Segoe Fluent Icons Unicode字符</returns>
        public static string GetIconChar(IconKind kind)
        {
            return kind switch
            {
                // 播放控制图标
                IconKind.Previous => "\uf8ac",//上一首
                IconKind.Play => "\uF5b0",//播放
                IconKind.Pause => "\uF8ae",//暂停
                IconKind.Next => "\uf8ad",//下一首
                IconKind.Lyrics => "\uf4a5",//歌词

                // 音量控制图标
                IconKind.VolumeHigh => "\uE995",//高音量
                IconKind.VolumeMedium => "\uE994",//中音量
                IconKind.VolumeLow => "\uE993",//低音量
                IconKind.VolumeMute => "\uE74F", //静音


                // 播放模式图标
                IconKind.PlayModeDefault => "\uE8AB",//默认播放模式
                IconKind.RepeatOne => "\uE8ED",//单曲循环
                IconKind.RepeatAll => "\uE8EE",//全部循环
                IconKind.Shuffle => "\uE8B1",//随机播放

                // 窗口控制图标
                IconKind.Minimize => "\uef2d",//最小化
                IconKind.Maximize => "\uef2e",//最大化
                IconKind.Restore => "\uef2f",//还原
                IconKind.Close => "\uef2c",//关闭

                // 播放列表图标
                IconKind.Collapse => "\ue96f",//折叠
                IconKind.Expand => "\ue970",//展开
                IconKind.Add => "\uf8aa",//添加
                IconKind.MusicNote => "\uE76e",//音乐符号
                IconKind.Folder => "\uE8B7",//文件夹
                // 排序规则图标
                IconKind.SortDefault => "\uE8CB",//排序
                IconKind.Clock => "\uE917",//时钟
                IconKind.Text => "\uE8D3",//文本文件
                IconKind.InPerson => "\uE77B",//空心歌手头像
                IconKind.Person => "\uea8c",//实心歌手头像
                IconKind.Album => "\ue735",//实心专辑封面
                IconKind.InAlbum => "\ue734",//空心专辑封面
                IconKind.Time => "\uEe93",//时间
                IconKind.Storage => "\uE8F1",//文件柜
                IconKind.InSettings => "\ue713",//空心设置-开
                IconKind.Settings => "\uf8b0",//实心设置-关
                IconKind.Heart => "\uEB51",//爱心空心
                IconKind.HeartFill => "\uEB52",//实心爱心-关
                IconKind.Delete => "\ue74d", //垃圾桶
                IconKind.Back=> "\uf0d5",//返回上一页
                IconKind.BackHome => "\uea8a",// 空心小房子-开
                IconKind.InHome => "\ue80f",//实心小房子 -关
                IconKind.InList => "\ue8b7",//默认列表-关
                IconKind.List => "\ue8d5",//空心列表-开
                IconKind.Search=> "\uf78b",//搜索 放大镜
                IconKind.Sound=> "\ueb49",//音频设置
                IconKind.Theme=> "\uf354",//主题设置
                IconKind.AppIcon=> "\ue7e7",//程序托盘设置
               IconKind.Spectrum=>"\uec4a",//频谱可视化设置
               IconKind.ClearList=> "\ue7bc" ,//清除列表
               IconKind.RoBot => "\ue99a",//音频引擎设置
              IconKind.Equalizer=> "\ued4d",//均衡器设置
              IconKind.SaveAs=> "\uea35",//保存 eb4e
                IconKind.Reset=> "\uedab",//重置
              IconKind.Position=> "\ue759",//查找歌曲位置  	e759  e7c8  f272	Bullseye
                IconKind.BigFont => "\uecc8",   //字体+  ecc8                       
                IconKind.SmallFont => "\uecc9", //字体-  ecc9                                                  
                IconKind.Right => "\ue8e2",//左对齐 edd9
                IconKind.Center => "\ue8e3", //中间对齐 ee95
                IconKind.Left => "\ue8e4", //右对齐 edda 
               IconKind .Photo=> "\ue7c5", //图片 e91b
               IconKind.NewFolder=> "\ue8f4",//新建路径
               IconKind. FolderOpen=> "\ue838",//打开文件夹
              IconKind.MusicInfo=> "\ue90b",//播放队列
                IconKind.LocaleLanguage => "\uf2b7",//语言设置 
                IconKind.Update => "\uf67b",//更新 	f67b
                _ => ""
            };
        }
    }
}
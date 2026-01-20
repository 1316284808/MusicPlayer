namespace MusicPlayer.Core.Enums
{
    /// <summary>
    /// 图标类型枚举
    /// </summary>
    public enum IconKind
    {
        /// <summary>
        /// 无图标
        /// </summary>
        None, 
        Previous,//上一曲
        Play,//播放
        Pause,//暂停
        Next,//下一曲
        Lyrics, // 
        VolumeHigh,//高
        VolumeMedium,//中
        VolumeLow,//低
        VolumeMute,  //音量关
        PlayModeDefault,
        RepeatOne,//单曲循环
        RepeatAll,//列表循环
        Shuffle, //随机
        Minimize,//-
        Maximize,//最大化
        Restore,
        Close,//X 
        Collapse,//>
        Expand,//<
        Add,//+
        MusicNote, 
        Settings,//设置/开
        InSettings,//实心设置/关
        Folder, //
        SortDefault,
        Clock,//时间排序
        Text,
        Person,
        InPerson,
        Album,
        InAlbum,
        Time,
        Storage, 
        Heart,// 心形图标，表示收藏或喜欢 /关
        HeartFill,//实心爱心/开
        Delete,// 删除图标 
        Back,//返回上一页
        InHome,//展开Tree
        BackHome,//收起Tree
        List ,//默认列表
        InList,//实心列表
        Search,//搜索
        Sound,//音频设置
       Theme,//主题设置
       AppIcon,//程序托盘
        Spectrum,//频谱可视化
        ClearList,//清除列表
        RoBot,//音频引擎设置
        Equalizer,//均衡器
        SaveAs,//保存
        Reset,//重置
        Position,// 定位
        BigFont,//字体加大
        SmallFont,//字体减小
        Left,//左对齐
        Center,//中间对齐
        Right,//右对齐
        Photo,//图片
        NewFolder,//新建路径
        FolderOpen,//打开文件夹
        MusicInfo,//播放队列
        LocaleLanguage,//语言

    }
}
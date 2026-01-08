using System.Windows;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 窗口状态消息
    /// </summary>
    public class WindowStateMessage : RequestMessage<WindowState> 
    { 
        public WindowState Value { get; set; }
        
        public WindowStateMessage(WindowState value) 
        { 
            Value = value; 
        }
    }

    /// <summary>
    /// 窗口状态变化消息
    /// </summary>
    public class WindowStateChangedMessage : ValueChangedMessage<WindowState> 
    { 
        public WindowStateChangedMessage(WindowState value) : base(value) { }
    }

    /// <summary>
    /// 关闭窗口消息
    /// </summary>
    public class CloseWindowMessage : RequestMessage<bool> { }

    /// <summary>
    /// 最小化窗口消息
    /// </summary>
    public class MinimizeWindowMessage : RequestMessage<bool> { }

    /// <summary>
    /// 切换最大化/还原窗口消息
    /// </summary>
    public class ToggleMaximizeWindowMessage : RequestMessage<bool> { }

    /// <summary>
    /// 窗口位置变化消息
    /// </summary>
    public class WindowPositionChangedMessage : ValueChangedMessage<Point>
    {
        public WindowPositionChangedMessage(Point position) : base(position) { }
    }
    
    /// <summary>
    /// 窗口尺寸变化消息
    /// </summary>
    public class WindowSizeChangedMessage : ValueChangedMessage<Size>
    {
        public WindowSizeChangedMessage(Size size) : base(size) { }
    }
    

    
    /// <summary>
    /// 语言变化消息
    /// </summary>
    public class LanguageChangedMessage : ValueChangedMessage<string>
    {
        public LanguageChangedMessage(string language) : base(language) { }
    }

    /// <summary>
    /// 切换壁纸消息
    /// </summary>
    public class ToggleWallpaperMessage : RequestMessage<bool> { }

    /// <summary>
    /// 导航到设置页面消息
    /// </summary>
    public class NavigateToSettingsMessage : RequestMessage<bool> { }

    /// <summary>
    /// 导航到主页消息
    /// </summary>
    public class NavigateToHomeMessage : RequestMessage<bool> { }

    /// <summary>
    /// 返回上一页消息
    /// </summary>
    public class GoBackMessage : RequestMessage<bool> { }

    /// <summary>
    /// 显示桌面歌词消息
    /// </summary>
    public class ShowLyricsMessage : RequestMessage<bool> { }

    /// <summary>
    /// 过滤收藏歌曲消息
    /// </summary>
    public class FilterFavoriteSongsMessage : RequestMessage<bool> { }

    /// <summary>
    /// 显示所有歌曲消息
    /// </summary>
    public class ShowAllSongsMessage : RequestMessage<bool> { }

    /// <summary>
    /// 内存清理请求消息
    /// </summary>
    public class MemoryCleanupRequestedMessage : RequestMessage<bool> { }

    /// <summary>
    /// 搜索框焦点请求消息
    /// </summary>
    public class SearchBoxFocusRequestMessage : RequestMessage<bool> { }

    /// <summary>
    /// 导航完成通知消息
    /// </summary>
    public class NavigationCompletedMessage : ValueChangedMessage<Type>
    {
        public NavigationCompletedMessage(Type pageType) : base(pageType) { }
    }

    /// <summary>
    /// 导航到歌手页面消息
    /// </summary>
    public class NavigateToSingerPageMessage : RequestMessage<bool> { }

    /// <summary>
    /// 导航到专辑页面消息
    /// </summary>
    public class NavigateToAlbumPageMessage : RequestMessage<bool> { }
}
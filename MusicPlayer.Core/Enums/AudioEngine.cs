using System.ComponentModel;

namespace MusicPlayer.Core.Enums
{
    /// <summary>
    /// 音频引擎类型
    /// </summary>
    public enum AudioEngine
    {
        /// <summary>
        /// 自动选择（由程序自主选择最适合的音频引擎）
        /// </summary>
        [Description("自动选择")]
        Auto = 0,
        
        /// <summary>
        /// DirectX 音频引擎
        /// </summary>
        [Description("DirectX")]
        DirectSound = 1,

        /// <summary>
        /// Windows 共享模式（Shared Mode）：多个应用可同时播放，由系统混音。
        /// </summary>
        [Description("Windows会话(共享)")]
        WASAPI = 2

        #region 太麻烦了，暂不做考虑。
        ///// <summary>
        ///// 独占模式（ Exclusive Mode）：绕过系统混音器，直接访问音频硬件，延迟最低、音质无损。
        ///// </summary>
        //[Description("Windows会话(独占)")]
        //WASAPI_Exclusive, 
        #endregion



    }
}
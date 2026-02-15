namespace MusicPlayer.Core.Enums
{
    /// <summary>
    /// 音频质量等级枚举
    /// 根据采样率、位深和编码格式划分音质等级
    /// </summary>
    public enum AudioQualityLevel
    {
        /// <summary>
        /// 高品质（有损压缩格式或参数无法获取）
        /// 例如：MP3, AAC, M4A, OGG, WMA 等
        /// 以及无法获取音频参数的情况
        /// </summary>
        HQ = 0,

        /// <summary>
        /// 高品质（CD级无损音质）
        /// 标准：采样率 44.1-48kHz，位深 16-24bit，无损格式
        /// 格式：FLAC, APE, WAV 等
        /// </summary>
        SQ = 1,

        /// <summary>
        /// 高解析度音频（Hi-Res）
        /// 标准：采样率 ≥ 96kHz，位深 ≥ 24bit，无损格式
        /// 格式：FLAC, WAV, ALAC, DSD, APE 等
        /// </summary>
        HiRes = 2
    }
}

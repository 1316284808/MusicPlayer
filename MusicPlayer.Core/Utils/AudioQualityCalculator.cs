using System;
using System.IO;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Utils
{
    /// <summary>
    /// 音频质量计算工具类
    /// 根据音频格式、采样率、位深计算音质等级
    /// </summary>
    public static class AudioQualityCalculator
    {
        // 无损格式列表（文件扩展名，小写）
        private static readonly string[] LosslessFormats = { ".flac", ".wav", ".ape", ".alac", ".aiff", ".dsd" };

        // 有损格式列表（文件扩展名，小写）
        private static readonly string[] LossyFormats = { ".mp3", ".aac", ".m4a", ".ogg", ".oga", ".wma" };

        /// <summary>
        /// 计算音频质量等级
        /// </summary>
        /// <param name="fileExtension">文件扩展名（包含点，如 ".flac"）</param>
        /// <param name="sampleRate">采样率（Hz）</param>
        /// <param name="bitsPerSample">位深（bits），可能为 null</param>
        /// <returns>音质等级</returns>
        public static AudioQualityLevel Calculate(string fileExtension, int sampleRate, int? bitsPerSample)
        {
            // 无法获取音频参数，返回 HQ
            if (string.IsNullOrEmpty(fileExtension) || sampleRate <= 0)
            {
                return AudioQualityLevel.HQ;
            }

            // 标准化扩展名
            string ext = fileExtension.ToLowerInvariant();

            // 有损格式，直接返回 HQ
            if (IsLossyFormat(ext))
            {
                return AudioQualityLevel.HQ;
            }

            // 无损格式，根据参数判断
            if (IsLosslessFormat(ext))
            {
                // Hi-Res 标准：采样率 ≥ 96kHz 且 位深 ≥ 24bit
                if (sampleRate >= 96000 && bitsPerSample.HasValue && bitsPerSample.Value >= 24)
                {
                    return AudioQualityLevel.HiRes;
                }

                // SQ 标准：采样率 44.1kHz-48kHz，位深 16-24bit
                // 或采样率 ≥ 96kHz 但位深 < 24bit（降级为 SQ）
                if (sampleRate >= 44100 && sampleRate <= 48000)
                {
                    // CD 标准音质
                    if (bitsPerSample.HasValue && bitsPerSample.Value >= 16)
                    {
                        return AudioQualityLevel.SQ;
                    }
                }
                else if (sampleRate >= 44100)
                {
                    // 高采样率但位深不足，降级为 SQ
                    if (bitsPerSample.HasValue && bitsPerSample.Value >= 16)
                    {
                        return AudioQualityLevel.SQ;
                    }
                }

                // 无损格式但参数低于 CD 标准，标记为 SQ（仍优于有损）
                if (sampleRate >= 22050 && bitsPerSample.HasValue && bitsPerSample.Value >= 16)
                {
                    return AudioQualityLevel.SQ;
                }

                // 参数异常，标记为 HQ
                return AudioQualityLevel.HQ;
            }

            // 不在已知格式列表中，返回 HQ
            return AudioQualityLevel.HQ;
        }

        /// <summary>
        /// 判断是否为无损格式
        /// </summary>
        /// <param name="extension">文件扩展名（小写）</param>
        /// <returns>是否为无损格式</returns>
        private static bool IsLosslessFormat(string extension)
        {
            return Array.Exists(LosslessFormats, f => f == extension);
        }

        /// <summary>
        /// 判断是否为有损格式
        /// </summary>
        /// <param name="extension">文件扩展名（小写）</param>
        /// <returns>是否为有损格式</returns>
        private static bool IsLossyFormat(string extension)
        {
            return Array.Exists(LossyFormats, f => f == extension);
        }

        /// <summary>
        /// 获取音质等级的显示名称
        /// </summary>
        /// <param name="level">音质等级</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(AudioQualityLevel level)
        {
            return level switch
            {
                AudioQualityLevel.HiRes => "HiRes",
                AudioQualityLevel.SQ => "SQ",
                AudioQualityLevel.HQ => "HQ",
                _ => "HQ"
            };
        }
    }
}

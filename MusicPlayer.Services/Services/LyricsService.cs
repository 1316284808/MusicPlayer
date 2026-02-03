using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using TagLib;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 歌词服务 - 负责歌词文件的加载和解析
    /// 支持从多个来源加载歌词：配置目录、歌曲同目录、TagLib内嵌歌词
    /// 支持LRC和SRT两种歌词格式
    /// </summary>
    public class LyricsService : ILyricsService
    {
        private readonly IConfigurationService _configurationService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        public LyricsService(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        /// <summary>
        /// 加载歌词文件
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>歌词行集合，如果没有找到歌词则返回空列表</returns>
        public List<LyricLine> LoadLyrics(string filePath)
        {
            try
            {
                // 获取用户设置的歌词目录
                string lyricDirectory = _configurationService.CurrentConfiguration.LyricDirectory;
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                // 1. 优先从用户指定的歌词目录查找
                if (!string.IsNullOrEmpty(lyricDirectory))
                {
                    // 查找LRC文件
                    var lrcPathInSpecifiedDir = Path.Combine(lyricDirectory, $"{fileName}.lrc");
                    if (Paths.FileExists(lrcPathInSpecifiedDir))
                    {
                        return ParseLrc(System.IO.File.ReadAllText(lrcPathInSpecifiedDir, System.Text.Encoding.UTF8));
                    }

                    // 查找SRT文件
                    var srtPathInSpecifiedDir = Path.Combine(lyricDirectory, $"{fileName}.srt");
                    if (Paths.FileExists(srtPathInSpecifiedDir))
                    {
                        return ParseSrt(System.IO.File.ReadAllText(srtPathInSpecifiedDir, System.Text.Encoding.UTF8));
                    }
                }

                // 2. 从音频文件的TagLib元数据中查找内嵌歌词
                try
                {
                    var tagFile = TagLib.File.Create(filePath);
                    if (!string.IsNullOrEmpty(tagFile.Tag.Lyrics))
                    {
                        var lrcLyrics = ParseLrc(tagFile.Tag.Lyrics);
                        if (lrcLyrics.Any())
                        {
                            return lrcLyrics;
                        }
                    }
                }
                catch (Exception)
                {
                    // 忽略TagLib读取错误，继续从文件查找
                }

                // 3. 从音频文件同目录查找歌词文件
                var lrcPath = Paths.GetLrcFilePath(filePath);
                if (Paths.FileExists(lrcPath))
                {
                    return ParseLrc(System.IO.File.ReadAllText(lrcPath, System.Text.Encoding.UTF8));
                }

                var srtPath = Paths.GetSrtFilePath(filePath);
                if (Paths.FileExists(srtPath))
                {
                    return ParseSrt(System.IO.File.ReadAllText(srtPath, System.Text.Encoding.UTF8));
                }

                // 没有找到歌词，返回空列表
                return new List<LyricLine>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LyricsService.LoadLyrics: 加载歌词失败 - {filePath} - {ex.Message}");
                return new List<LyricLine>();
            }
        }

        /// <summary>
        /// 异步加载歌词文件
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>歌词行集合</returns>
        public Task<List<LyricLine>> LoadLyricsAsync(string filePath)
        {
            return Task.Run(() => LoadLyrics(filePath));
        }

        /// <summary>
        /// 解析SRT格式的歌词
        /// </summary>
        /// <param name="srtContent">SRT格式内容</param>
        /// <returns>歌词行集合</returns>
        private List<LyricLine> ParseSrt(string srtContent)
        {
            var lyrics = new List<LyricLine>();
            
            // 使用双换行符分割字幕块
            var blocks = srtContent.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var block in blocks)
            {
                var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 3)
                {
                    // 解析时间行（序号后的第一行）
                    var timeLine = lines[1];
                    var timeMatch = Regex.Match(timeLine, @"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})");
                    
                    if (timeMatch.Success)
                    {
                        // 解析开始时间
                        var startTime = new TimeSpan(0, 
                            int.Parse(timeMatch.Groups[1].Value), 
                            int.Parse(timeMatch.Groups[2].Value), 
                            int.Parse(timeMatch.Groups[3].Value))
                            .Add(TimeSpan.FromMilliseconds(int.Parse(timeMatch.Groups[4].Value)));
                        
                        // 合并所有文本行（从第二行开始）
                        var textLines = lines.Skip(2).ToArray();
                        var text = string.Join("\n", textLines).Trim();
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            // 创建歌词行对象
                            var lyricLine = new LyricLine { Time = startTime };
                            
                            // SRT格式通常不支持双语歌词，直接作为原文
                            lyricLine.OriginalText = text;
                            
                            lyrics.Add(lyricLine);
                        }
                    }
                }
            }
            
            // 按时间排序并返回
            return lyrics.OrderBy(l => l.Time).ToList();
        }

        /// <summary>
        /// 解析LRC格式的歌词
        /// </summary>
        /// <param name="lrcContent">LRC格式内容</param>
        /// <returns>歌词行集合</returns>
        private List<LyricLine> ParseLrc(string lrcContent)
        {
            var lyrics = new List<LyricLine>();
            var lines = lrcContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var tempLyrics = new Dictionary<TimeSpan, List<string>>();

            foreach (var line in lines)
            {
                // 支持一行中有多个时间戳 [00:12.34][00:56.78]歌词
                var timeMatches = Regex.Matches(line, @"\[(\d{2}):(\d{2})\.(\d{2,3})\]");
                var textMatch = Regex.Match(line, @"\[[\d:.]+\](.*)$");
                
                if (timeMatches.Count > 0 && textMatch.Success)
                {
                    var text = textMatch.Groups[1].Value.Trim();
                    
                    // 跳过空文本和元数据行
                    if (string.IsNullOrEmpty(text) || text.StartsWith("["))
                        continue;
                    
                    // 处理每个时间戳
                    foreach (Match timeMatch in timeMatches)
                    {
                        var minutes = int.Parse(timeMatch.Groups[1].Value);
                        var seconds = int.Parse(timeMatch.Groups[2].Value);
                        var millisStr = timeMatch.Groups[3].Value;
                        
                        // 处理2位和3位毫秒
                        var milliseconds = millisStr.Length == 2 ? 
                            int.Parse(millisStr) * 10 : 
                            int.Parse(millisStr);
                        
                        var time = new TimeSpan(0, 0, minutes, seconds, milliseconds);
                        
                        // 按时间戳分组歌词
                        if (!tempLyrics.ContainsKey(time))
                        {
                            tempLyrics[time] = new List<string>();
                        }
                        tempLyrics[time].Add(text);
                    }
                }
            }

            // 将分组的歌词转换为LyricLine对象
            foreach (var kvp in tempLyrics.OrderBy(x => x.Key))
            {
                var time = kvp.Key;
                var texts = kvp.Value;
                
                // 创建歌词行对象
                var lyricLine = new LyricLine { Time = time };
                
                // 检查是否有多个文本行（双语歌词）
                if (texts.Count > 1)
                {
                    var firstText = texts[0].Trim();
                    var secondText = texts[1].Trim();
                    
                    // 检测语言类型
                    var firstHasChinese = ContainsChineseChars(firstText);
                    var secondHasChinese = ContainsChineseChars(secondText);
                    
                    // 智能判断原文和翻译
                    if (firstHasChinese && !secondHasChinese)
                    {
                        // 第一行是中文，第二行不是中文 → 第二行作为原文，第一行作为翻译
                        lyricLine.OriginalText = secondText;
                        lyricLine.TranslatedText = firstText;
                    }
                    else if (!firstHasChinese && secondHasChinese)
                    {
                        // 第一行不是中文，第二行是中文 → 第一行作为原文，第二行作为翻译
                        lyricLine.OriginalText = firstText;
                        lyricLine.TranslatedText = secondText;
                    }
                    else
                    {
                        // 其他情况保持原有逻辑
                        lyricLine.OriginalText = firstText;
                        lyricLine.TranslatedText = secondText;
                    }
                }
                else
                {
                    // 单文本行
                    var text = texts[0];
                    
                    // 检查单行内是否有双语歌词（包含分隔符）
                    if (text.Contains("｜") || text.Contains("|"))
                    {
                        var separator = text.Contains("｜") ? '｜' : '|';
                        var parts = text.Split(new[] { separator }, 2);
                        if (parts.Length == 2)
                        {
                            var firstPart = parts[0].Trim();
                            var secondPart = parts[1].Trim();
                            
                            // 检测语言类型
                            var firstHasChinese = ContainsChineseChars(firstPart);
                            var secondHasChinese = ContainsChineseChars(secondPart);
                            
                            // 智能判断原文和翻译
                            if (firstHasChinese && !secondHasChinese)
                            {
                                // 第一部分是中文，第二部分不是中文 → 第二部分作为原文，第一部分作为翻译
                                lyricLine.OriginalText = secondPart;
                                lyricLine.TranslatedText = firstPart;
                            }
                            else if (!firstHasChinese && secondHasChinese)
                            {
                                // 第一部分不是中文，第二部分是中文 → 第一部分作为原文，第二部分作为翻译
                                lyricLine.OriginalText = firstPart;
                                lyricLine.TranslatedText = secondPart;
                            }
                            else
                            {
                                // 其他情况保持原有逻辑
                                lyricLine.OriginalText = firstPart;
                                lyricLine.TranslatedText = secondPart;
                            }
                        }
                        else
                        {
                            // 单语言文本
                            lyricLine.OriginalText = text.Trim();
                        }
                    }
                    else
                    {
                        // 单语言文本
                        lyricLine.OriginalText = text.Trim();
                    }
                }
                
                // 添加到歌词列表
                lyrics.Add(lyricLine);
            }
            
            return lyrics;
        }

        /// <summary>
        /// 检测文本是否包含中文字符
        /// </summary>
        /// <param name="text">要检测的文本</param>
        /// <returns>是否包含中文字符</returns>
        private bool ContainsChineseChars(string text)
        {
            // 使用正则表达式检测中文字符（Unicode范围：\u4e00-\u9fa5）
            return System.Text.RegularExpressions.Regex.IsMatch(text, "[\\u4e00-\\u9fa5]");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 当前没有需要释放的资源
        }
    }
}
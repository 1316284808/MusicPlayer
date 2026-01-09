using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using TagLib;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放列表服务 - 负责歌曲信息的提取和元数据处理
    /// 不再直接处理数据库操作，数据持久化由 PlaylistDataService 和 PlaylistCacheService 负责
    /// </summary>
    public class PlaylistService : IPlaylistService, IDisposable
    {
        private readonly IConfigurationService _configurationService;

        public PlaylistService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }
        
        public List<Song> LoadSongsFromFolder(string folderPath)
        {
            var songs = new List<Song>();
            // Focus on commonly supported formats
            var supportedFiles = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".oga", ".aac", ".wma" };

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => supportedFiles.Contains(Path.GetExtension(f).ToLower())))
            {
                var song = ExtractSongInfo(file);
                if (song != null)
                {
                    songs.Add(song);
                }
            }
            return songs;
        }

        public Song? ExtractSongInfo(string filePath)
        {
            try
            {
                // 检查文件是否存在
                if (!System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ExtractSongInfo: 文件不存在: {filePath}");
                    return null;
                }

                TagLib.File tagFile = null;
                try
                {
                    tagFile = TagLib.File.Create(filePath);
                }
                catch (TagLib.CorruptFileException cex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExtractSongInfo: TagLib文件损坏异常: {filePath} - {cex.Message}");
                    
                    // 创建基础歌曲对象，不依赖TagLib
                    return new Song
                    {
                        FilePath = filePath,
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        Artist = "Unknown Artist",
                        Album = "Unknown Album",
                        Duration = TimeSpan.Zero,
                        FileSize = new FileInfo(filePath).Length,
                        AddedTime = DateTime.Now,
                        DelayAlbumArtLoading = true
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExtractSongInfo: 创建TagLib文件失败: {filePath} - {ex.Message}");
                    
                    // 创建基础歌曲对象，不依赖TagLib
                    return new Song
                    {
                        FilePath = filePath,
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        Artist = "Unknown Artist",
                        Album = "Unknown Album",
                        Duration = TimeSpan.Zero,
                        FileSize = new FileInfo(filePath).Length,
                        AddedTime = DateTime.Now,
                        DelayAlbumArtLoading = true
                    };
                }

                // 安全地读取TagLib属性
                var song = new Song
                {
                    FilePath = filePath,
                    Title = string.IsNullOrEmpty(tagFile?.Tag?.Title) ? Path.GetFileNameWithoutExtension(filePath) : tagFile.Tag.Title,
                    Artist = string.IsNullOrEmpty(tagFile?.Tag?.FirstPerformer) ? "Unknown Artist" : tagFile.Tag.FirstPerformer,
                    Album = string.IsNullOrEmpty(tagFile?.Tag?.Album) ? "Unknown Album" : tagFile.Tag.Album,
                    Duration = tagFile?.Properties?.Duration ?? TimeSpan.Zero,
                    FileSize = new FileInfo(filePath).Length,
                    AddedTime = DateTime.Now,
                    DelayAlbumArtLoading = true // 设置延迟加载标志，封面将在需要时才加载
                };
                
                // 注释：专辑封面数据将在需要时通过懒加载机制加载，而不是在导入时全部加载
                // song.AlbumArtData = LoadAlbumArtData(tagFile);
                
                System.Diagnostics.Debug.WriteLine($"ExtractSongInfo: 成功提取歌曲信息: {song.Title}");
                return song;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExtractSongInfo: 未知异常: {filePath} - {ex.Message}");
                return null;
            }
        }

        private byte[]? LoadAlbumArtData(TagLib.File tagFile)
        {
            try
            {
                if (tagFile?.Tag?.Pictures?.Length > 0)
                {
                    var picture = tagFile.Tag.Pictures[0];
                    try
                    {
                        // 优化：直接返回字节数组，不转换为Base64字符串，减少内存占用
                        return picture.Data.Data;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAlbumArtData: 加载专辑封面数据失败: {ex.Message}");
            }
            return null;
        }

        public List<LyricLine> LoadLyrics(string filePath)
        {
            // 获取用户设置的歌词目录
            string lyricDirectory = _configurationService.CurrentConfiguration.LyricDirectory;
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // 1. Check for embedded lyrics first (preferred)
            try
            {
                var tagFile = TagLib.File.Create(filePath);
                if (!string.IsNullOrEmpty(tagFile.Tag.Lyrics))
                {
                    // Try to parse as LRC format first, then as plain text
                    var lrcLyrics = ParseLrc(tagFile.Tag.Lyrics);
                    if (lrcLyrics.Any())
                    {
                        return lrcLyrics;
                    }
                }
            }
            catch (Exception) { }

            // 2. Check for external .lrc file in specified lyric directory
            if (!string.IsNullOrEmpty(lyricDirectory))
            {
                var lrcPathInSpecifiedDir = Path.Combine(lyricDirectory, $"{fileName}.lrc");
                if (Paths.FileExists(lrcPathInSpecifiedDir))
                {
                    return ParseLrc(System.IO.File.ReadAllText(lrcPathInSpecifiedDir, System.Text.Encoding.UTF8));
                }

                // 3. Check for external .srt file in specified lyric directory
                var srtPathInSpecifiedDir = Path.Combine(lyricDirectory, $"{fileName}.srt");
                if (Paths.FileExists(srtPathInSpecifiedDir))
                {
                    return ParseSrt(System.IO.File.ReadAllText(srtPathInSpecifiedDir, System.Text.Encoding.UTF8));
                }
            }

            // 4. Check for external .lrc file in song directory
            var lrcPath = Paths.GetLrcFilePath(filePath);
            if (Paths.FileExists(lrcPath))
            {
                return ParseLrc(System.IO.File.ReadAllText(lrcPath, System.Text.Encoding.UTF8));
            }

            // 5. Check for external .srt file in song directory
            var srtPath = Paths.GetSrtFilePath(filePath);
            if (Paths.FileExists(srtPath))
            {
                return ParseSrt(System.IO.File.ReadAllText(srtPath, System.Text.Encoding.UTF8));
            }

            return new List<LyricLine>();
        }

        private List<LyricLine> ParseSrt(string srtContent)
        {
            var lyrics = new List<LyricLine>();
            
            // Split by double newlines to separate subtitle blocks
            var blocks = srtContent.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var block in blocks)
            {
                var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 3)
                {
                    // Parse time line (line 1 after sequence number)
                    var timeLine = lines[1];
                    var timeMatch = Regex.Match(timeLine, @"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})");
                    
                    if (timeMatch.Success)
                    {
                        var startTime = new TimeSpan(0, 
                            int.Parse(timeMatch.Groups[1].Value), 
                            int.Parse(timeMatch.Groups[2].Value), 
                            int.Parse(timeMatch.Groups[3].Value))
                            .Add(TimeSpan.FromMilliseconds(int.Parse(timeMatch.Groups[4].Value)));
                        
                        // Combine all text lines (starting from line 2)
                        var textLines = lines.Skip(2).ToArray();
                        var text = string.Join("\n", textLines).Trim();
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            lyrics.Add(new LyricLine { Time = startTime, Text = text });
                        }
                    }
                }
            }
            
            return lyrics.OrderBy(l => l.Time).ToList();
        }

        private List<LyricLine> ParseLrc(string lrcContent)
        {
            var lyrics = new List<LyricLine>();
            var lines = lrcContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var tempLyrics = new Dictionary<TimeSpan, List<string>>();

            foreach (var line in lines)
            {
                // Support multiple time stamps in one line [00:12.34][00:56.78]歌词
                var timeMatches = Regex.Matches(line, @"\[(\d{2}):(\d{2})\.(\d{2,3})\]");
                var textMatch = Regex.Match(line, @"\[[\d:.]+\](.*)$");
                
                if (timeMatches.Count > 0 && textMatch.Success)
                {
                    var text = textMatch.Groups[1].Value.Trim();
                    
                    // Skip empty text and metadata lines
                    if (string.IsNullOrEmpty(text) || text.StartsWith("["))
                        continue;
                    
                    foreach (Match timeMatch in timeMatches)
                    {
                        var minutes = int.Parse(timeMatch.Groups[1].Value);
                        var seconds = int.Parse(timeMatch.Groups[2].Value);
                        var millisStr = timeMatch.Groups[3].Value;
                        
                        // Handle both 2-digit and 3-digit milliseconds
                        var milliseconds = millisStr.Length == 2 ? 
                            int.Parse(millisStr) * 10 : 
                            int.Parse(millisStr);
                        
                        var time = new TimeSpan(0, 0, minutes, seconds, milliseconds);
                        
                        // Group lyrics by time stamp
                        if (!tempLyrics.ContainsKey(time))
                        {
                            tempLyrics[time] = new List<string>();
                        }
                        tempLyrics[time].Add(text);
                    }
                }
            }

            // Convert grouped lyrics to LyricLine objects
            foreach (var kvp in tempLyrics.OrderBy(x => x.Key))
            {
                var time = kvp.Key;
                var texts = kvp.Value;
                
                // If multiple texts exist for the same timestamp, treat as bilingual
                if (texts.Count > 1)
                {
                    // Combine as bilingual lyrics (original + translation)
                    var combinedText = string.Join("\n", texts);
                    lyrics.Add(new LyricLine { Time = time, Text = combinedText });
                }
                else
                {
                    // Single text line
                    var text = texts[0];
                    
                    // Check for bilingual lyrics in single line (contains separators)
                    if (text.Contains("｜") || text.Contains("|"))
                    {
                        var separator = text.Contains("｜") ? '｜' : '|';
                        var parts = text.Split(new[] { separator }, 2);
                        if (parts.Length == 2)
                        {
                            var combinedText = parts[0].Trim() + "\n" + parts[1].Trim();
                            lyrics.Add(new LyricLine { Time = time, Text = combinedText });
                        }
                        else
                        {
                            lyrics.Add(new LyricLine { Time = time, Text = text });
                        }
                    }
                    else
                    {
                        lyrics.Add(new LyricLine { Time = time, Text = text });
                    }
                }
            }
            
            return lyrics;
        }
        public void Dispose()
        {
            
        }
    }
}
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

        
        public void Dispose()
        {
            
        }
    }
}
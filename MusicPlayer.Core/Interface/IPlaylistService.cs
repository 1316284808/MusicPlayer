using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    public interface IPlaylistService : IDisposable
    {
        List<Song> LoadSongsFromFolder(string folderPath);
        Song? ExtractSongInfo(string filePath);
        List<LyricLine> LoadLyrics(string filePath);
  
    }
}
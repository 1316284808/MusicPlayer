using NAudio.Wave;
using NAudio.Vorbis;
using System;

namespace MusicPlayer.Core.Audio
{
    public class VorbisAudioFileReader : AudioFileReader
    {
        private readonly VorbisWaveReader _vorbisReader;

        public VorbisAudioFileReader(VorbisWaveReader vorbisReader) 
            : base(CreateTempWaveFile(vorbisReader))
        {
            _vorbisReader = vorbisReader;
        }

        private static string CreateTempWaveFile(VorbisWaveReader vorbisReader)
        {
            // Create a temporary WAV file from the Vorbis reader
            var tempPath = System.IO.Path.GetTempFileName();
            tempPath = System.IO.Path.ChangeExtension(tempPath, ".wav");

            WaveFileWriter.CreateWaveFile(tempPath, vorbisReader);
            return tempPath;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _vorbisReader?.Dispose();
                // Clean up the temporary file
                try
                {
                    if (System.IO.File.Exists(FileName))
                    {
                        System.IO.File.Delete(FileName);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            base.Dispose(disposing);
        }
    }
}

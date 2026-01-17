using MusicPlayer.Core.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TagLibFile = TagLib.File;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 封面数据也是数据，放在这里没毛病
    /// </summary>
    public static class AlbumArtLoader
    {
        // 缓存相关配置
        private static readonly string _cacheDirectory;
        
        // 静态构造函数，初始化缓存目录
        static AlbumArtLoader() { 
             
            _cacheDirectory = Paths.AlbumArtCacheDirectory;// 
            // 确保缓存目录存在
            Directory.CreateDirectory(_cacheDirectory);
        }
        
        // 异步加载封面
        public static async Task<BitmapImage?> LoadAlbumArtAsync(string filePath)
        {
            // 1. 尝试从磁盘缓存加载
            var cachedBitmap = TryLoadFromDiskCache(filePath);
            if (cachedBitmap != null)
            {
                return cachedBitmap;
            }
            
            // 2. 从文件提取封面数据
            byte[]? albumArtData = null;
            try
            {
                await Task.Run(() => {
                    if (!System.IO.File.Exists(filePath))
                        return;
                        
                    var tagFile = TagLibFile.Create(filePath);
                    if (tagFile?.Tag?.Pictures?.Length > 0)
                    {
                        var picture = tagFile.Tag.Pictures[0];
                        albumArtData = picture.Data.Data;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 从文件提取封面数据失败: {ex.Message}");
            }
            
            // 3. 生成默认封面
            if (albumArtData == null || albumArtData.Length == 0)
            {
                return GetDefaultAlbumArt();
            }
            
            // 4. 保存到磁盘缓存
            SaveToDiskCache(filePath, albumArtData);
            
            // 5. 返回封面
            return LoadBitmapFromBytes(albumArtData);
        }
        
        // 同步加载封面
        public static BitmapImage? LoadAlbumArt(string filePath)
        {
            // 1. 尝试从磁盘缓存加载
            var cachedBitmap = TryLoadFromDiskCache(filePath);
            if (cachedBitmap != null)
            {
                return cachedBitmap;
            }
            
            // 2. 从文件提取封面数据
            byte[]? albumArtData = null;
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    var tagFile = TagLibFile.Create(filePath);
                    if (tagFile?.Tag?.Pictures?.Length > 0)
                    {
                        var picture = tagFile.Tag.Pictures[0];
                        albumArtData = picture.Data.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 从文件提取封面数据失败: {ex.Message}");
            }
            
            // 3. 生成默认封面
            if (albumArtData == null || albumArtData.Length == 0)
            {
                return GetDefaultAlbumArt();
            }
            
            // 4. 保存到磁盘缓存
            SaveToDiskCache(filePath, albumArtData);
            
            // 5. 返回封面
            return LoadBitmapFromBytes(albumArtData);
        }
        
        // 尝试从磁盘缓存加载
        public static BitmapImage? TryLoadFromDiskCache(string filePath)
        {
            try
            {
                var cacheFilePath = GetCacheFilePath(filePath);
                if (System.IO.File.Exists(cacheFilePath))
                {
                    return LoadBitmapFromFile(cacheFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 从缓存加载封面失败: {ex.Message}");
            }
            
            return null;
        }
        
        // 保存到磁盘缓存
        private static void SaveToDiskCache(string filePath, byte[] albumArtData)
        {
            try
            {
                var cacheFilePath = GetCacheFilePath(filePath);
                
                // 添加重试逻辑，处理文件被锁定的情况
                int retryCount = 0;
                const int maxRetries = 3;
                const int retryDelay = 100;
                
                while (retryCount < maxRetries)
                {
                    try
                    {
                        System.IO.File.WriteAllBytes(cacheFilePath, albumArtData);
                        return; // 保存成功，退出重试循环
                    }
                    catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                    {
                        // 文件被锁定，重试
                        retryCount++;
                        System.Threading.Thread.Sleep(retryDelay);
                    }
                }
                
                // 多次重试失败，记录错误
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 保存封面到缓存失败，多次重试后仍被锁定: {cacheFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 保存封面到缓存失败: {ex.Message}");
            }
        }
        
        // 生成缓存文件路径
        private static string GetCacheFilePath(string originalFilePath)
        {
            // 使用文件路径的哈希值作为缓存文件名
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(originalFilePath);
            var hash = sha256.ComputeHash(bytes);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
            
            // 使用jpg格式存储缓存
            return Path.Combine(_cacheDirectory, $"{hashString}.jpg");
        }
        
        // 字节数组转BitmapImage
        private static BitmapImage? LoadBitmapFromBytes(byte[] albumArtData)
        {
            try
            {
                var bitmap = new BitmapImage();
                using var stream = new MemoryStream(albumArtData);
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // 忽略颜色配置文件，解决某些图像无法加载的问题
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结BitmapImage，使其可以在UI线程外使用
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 字节数组转BitmapImage失败: {ex.Message}");
                return null;
            }
        }
        
        // 从文件加载BitmapImage
        private static BitmapImage? LoadBitmapFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // 忽略颜色配置文件，解决某些图像无法加载的问题
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结BitmapImage，使其可以在UI线程外使用
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 从文件加载BitmapImage失败: {ex.Message}");
                return null;
            }
        }
        
        // 获取默认封面
        public static BitmapImage GetDefaultAlbumArt()
        {
            try
            {
                var bitmap = new BitmapImage();
                var defaultCoverPath = Path.Combine(Paths.ExecutableDirectory, "resources", "MusicPlayer.png");
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(defaultCoverPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结图像以确保线程安全
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 获取默认封面失败: {ex.Message}");
                // 失败时返回一个空的BitmapImage
                var bitmap = new BitmapImage();
                bitmap.Freeze();
                return bitmap;
            }
        }
        
        // 异步获取默认封面
        public static async Task<BitmapImage> GetDefaultAlbumArtAsync()
        {
            try
            {
                // 在后台线程创建BitmapImage
                return await Task.Run(() => {
                    var bitmap = new BitmapImage();
                    var defaultCoverPath = Path.Combine(Paths.ExecutableDirectory, "resources", "MusicPlayer.png");
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(defaultCoverPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // 冻结图像以确保线程安全
                    return bitmap;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 异步获取默认封面失败: {ex.Message}");
                // 失败时返回一个空的BitmapImage
                var bitmap = new BitmapImage();
                bitmap.Freeze();
                return bitmap;
            }
        }
        
        // 清理过期缓存
        public static void CleanupExpiredCache(TimeSpan maxAge)
        {
            try
            {
                if (!System.IO.Directory.Exists(_cacheDirectory))
                    return;
                
                var now = DateTime.Now;
                foreach (var file in System.IO.Directory.GetFiles(_cacheDirectory))
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    if (now - fileInfo.LastWriteTime > maxAge)
                    {
                        System.IO.File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 清理过期缓存失败: {ex.Message}");
            }
        }
    }
}
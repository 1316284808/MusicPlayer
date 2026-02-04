using MusicPlayer.Core.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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
        
        // 异步加载封面 - 支持指定尺寸
        public static async Task<BitmapImage?> LoadAlbumArtAsync(string filePath, int width, int height)
        {
            // 1. 加载原始图像（从缓存或文件）
            var originalImage = await LoadAlbumArtAsync(filePath);
            if (originalImage == null)
            {
                return GetDefaultAlbumArt();
            }
            
            // 2. 缩放裁切图像
            return ScaleAndCropImage(originalImage, width, height);
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
        
        // 缩放并裁切图像到指定尺寸
        private static BitmapImage? ScaleAndCropImage(BitmapImage originalImage, int targetWidth, int targetHeight)
        {
            try
            {
                // 检查原始图像是否为空
                if (originalImage == null)
                {
                    return null;
                }
                
                // 检查目标尺寸是否有效
                if (targetWidth <= 0 || targetHeight <= 0)
                {
                    return null;
                }
                
                // 检查目标尺寸是否与原始图像尺寸相同，如果相同，直接返回原始图像的副本
                if (Math.Abs(originalImage.Width - targetWidth) < 0.1 && Math.Abs(originalImage.Height - targetHeight) < 0.1)
                {
                    // 创建原始图像的副本
                    return LoadBitmapFromBytes(ConvertBitmapImageToBytes(originalImage));
                }
                
                // 创建DrawingVisual用于绘制缩放裁切后的图像
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    // 计算缩放比例，保持宽高比
                    double scaleX = (double)targetWidth / originalImage.Width;
                    double scaleY = (double)targetHeight / originalImage.Height;
                    double scale = Math.Max(scaleX, scaleY);
                    
                    // 计算缩放后的图像尺寸
                    int scaledWidth = (int)(originalImage.Width * scale);
                    int scaledHeight = (int)(originalImage.Height * scale);
                    
                    // 计算居中位置
                    int offsetX = (targetWidth - scaledWidth) / 2;
                    int offsetY = (targetHeight - scaledHeight) / 2;
                    
                    // 绘制图像（居中裁剪）
                    drawingContext.DrawImage(
                        originalImage,
                        new Rect(offsetX, offsetY, scaledWidth, scaledHeight));
                }
                
                // 创建RenderTargetBitmap来渲染DrawingVisual
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                    targetWidth,
                    targetHeight,
                    96,  // DPI X
                    96,  // DPI Y
                    PixelFormats.Pbgra32);
                    
                renderTargetBitmap.Render(drawingVisual);
                
                // 将RenderTargetBitmap转换为BitmapImage
                BitmapImage result = new BitmapImage();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // 保存到PNG格式，保证质量
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                    encoder.Save(memoryStream);
                    
                    // 加载到BitmapImage
                    result.BeginInit();
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.StreamSource = memoryStream;
                    result.EndInit();
                    result.Freeze(); // 冻结以确保线程安全
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 缩放裁切图像失败: {ex.Message}");
                return null;
            }
        }
        
        // 将BitmapImage转换为字节数组
        private static byte[] ConvertBitmapImageToBytes(BitmapImage bitmapImage)
        {
            try
            {
                using MemoryStream memoryStream = new MemoryStream();
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumArtLoader: 将BitmapImage转换为字节数组失败: {ex.Message}");
                return Array.Empty<byte>();
            }
        }
    }
}
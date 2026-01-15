using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 歌曲模型类 - 表示音频文件的基本信息和元数据
    /// 使用MVVM工具包实现属性变更通知，支持数据绑定
    /// 已优化：实现专辑封面懒加载机制
    /// </summary>
    public partial class Song : ObservableObject
    {
        /// <summary>歌曲ID（数据库主键）</summary>
        [ObservableProperty]
        private int _id = -1;

        /// <summary>音频文件完整路径</summary>
        [ObservableProperty]
        private string _filePath = string.Empty;

        /// <summary>歌曲标题</summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>艺术家/演唱者</summary>
        [ObservableProperty]
        private string _artist = string.Empty;

        /// <summary>专辑名称</summary>
        [ObservableProperty]
        private string _album = string.Empty;

        /// <summary>歌曲时长</summary>
        [ObservableProperty]
        private TimeSpan _duration;

        /// <summary>
        /// Duration属性变化时的处理
        /// </summary>
        partial void OnDurationChanged(TimeSpan value)
        {
            OnPropertyChanged(nameof(TotalTimeText));
        }



        /// <summary>文件大小（字节）</summary>
        [ObservableProperty]
        private long _fileSize;

        /// <summary>添加时间戳</summary>
        [ObservableProperty]
        private DateTime _addedTime = DateTime.Now;

        /// <summary>是否收藏</summary>
        [ObservableProperty]
        private bool _heart = false;

        /// <summary>是否已删除（逻辑删除，不实际删除文件）</summary>
        [ObservableProperty]
        private bool _isDeleted = false;

        /// <summary>是否延迟加载专辑封面（用于批量处理优化）</summary>
        private bool _delayAlbumArtLoading = false;

        /// <summary>静态属性：是否启用封面缓存，根据配置动态设置</summary>
        public static bool IsCoverCacheEnabled { get; set; } = true;

        /// <summary>专辑封面图像（可选）- 懒加载</summary>
        private BitmapImage? _albumArt;
        private BitmapImage? _originalAlbumArt; // 存储原图（用于旋转封面等需要高清显示的场合）
        private byte[]? _albumArtData; // 存储封面图像原始数据，用于懒加载（优化：从Base64字符串改为字节数组）
        private bool _isLoadingAlbumArt = false; // 防止加载过程中的递归循环
        private bool _isLoadingOriginalAlbumArt = false; // 防止原图加载过程中的递归循环



        /// <summary>
        /// 专辑封面
        /// 优先从磁盘缓存加载，缓存不存在则从文件提取并保存到缓存
        /// </summary>
        [LiteDB.BsonIgnore]
        public BitmapImage? AlbumArt
        {
            get
            {
                // 如果封面为空，且没有设置延迟加载，且不在加载过程中，则自动加载
                if (_albumArt == null && !_delayAlbumArtLoading && !_isLoadingAlbumArt)
                {
                    // 先尝试从缓存加载
                    if (!TryLoadAlbumArtFromCache() && (_albumArtData == null || _albumArtData.Length == 0))
                    {
                        // 缓存不存在且没有封面数据，从文件加载
                        EnsureAlbumArtDataLoaded();
                        if (_albumArtData != null && _albumArtData.Length > 0)
                        {
                            LoadAlbumArtFromData();
                            // 保存到缓存
                            SaveAlbumArtToCache();
                        }
                    }
                    else if (_albumArtData != null && _albumArtData.Length > 0)
                    {
                        // 已有封面数据，直接加载
                        LoadAlbumArtFromData();
                    }
                }
                return _albumArt;
            }
            private set => SetProperty(ref _albumArt, value);
        }

        /// <summary>
        /// 原图专辑封面（用于旋转封面等需要高清显示的场合）
        /// 即取即用即走即清策略，不使用缓存
        /// </summary>
        [LiteDB.BsonIgnore]
        public BitmapImage? OriginalAlbumArt
        {
            get
            {
                // 如果原图封面为空，且有封面数据，且不在加载过程中，则自动加载原图
                if (_originalAlbumArt == null && _albumArtData != null && _albumArtData.Length > 0 && !_isLoadingOriginalAlbumArt)
                {
                    EnsureOriginalAlbumArtLoaded();
                }
                return _originalAlbumArt;
            }
            private set => SetProperty(ref _originalAlbumArt, value);
        }

        /// <summary>
        /// 设置专辑封面数据（用于序列化和懒加载）
        /// 优化：使用字节数组而非Base64字符串，减少内存占用
        /// </summary>
        public byte[]? AlbumArtData
        {
            get => _albumArtData;
            set
            {
                if (_albumArtData != value)
                {
                    _albumArtData = value;

                    // 如果已有加载的图像，需要重新加载
                    if (_albumArt != null)
                    {
                        _albumArt = null;
                        OnPropertyChanged(nameof(AlbumArt));
                    }
                }
            }
        }

        /// <summary>
        /// 获取专辑封面数据的Base64字符串表示（按需转换）
        /// 仅在需要序列化到JSON或其他文本格式时使用
        /// </summary>
        public string? AlbumArtDataBase64
        {
            get => _albumArtData != null ? Convert.ToBase64String(_albumArtData) : null;
        }

        /// <summary>
        /// 是否延迟加载专辑封面（用于批量处理优化）
        /// </summary>
        public bool DelayAlbumArtLoading
        {
            get => _delayAlbumArtLoading;
            set
            {
                if (_delayAlbumArtLoading != value)
                {
                    _delayAlbumArtLoading = value;
                    OnPropertyChanged(nameof(DelayAlbumArtLoading));

                    // 如果取消延迟加载且有封面数据，立即加载
                    if (!value && _albumArt == null && _albumArtData != null && _albumArtData.Length > 0)
                    {
                        EnsureAlbumArtLoaded();
                    }
                }
            }
        }

        /// <summary>
        /// 从文件中加载专辑封面数据
        /// </summary>
        private void LoadAlbumArtDataFromFile()
        {
            if (string.IsNullOrEmpty(FilePath) || !System.IO.File.Exists(FilePath))
                return;
                
            try
            {
                var tagFile = TagLib.File.Create(FilePath);
                if (tagFile?.Tag?.Pictures?.Length > 0)
                {
                    var picture = tagFile.Tag.Pictures[0];
                    _albumArtData = picture.Data.Data;
                //    System.Diagnostics.Debug.WriteLine($"Song: 从文件加载专辑封面数据成功 - {Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Song: 从文件加载专辑封面数据失败 - {Title}, 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从数据中加载专辑封面 - 使用多重尝试和更健壮的图像处理，包含图像预缩放
        /// </summary>
        private void LoadAlbumArtFromData()
        {
            // 防止递归调用
            if (_isLoadingAlbumArt)
            {
                return;
            }

            if (_albumArtData == null || _albumArtData.Length == 0)
            {
                return;
            }

            // 确保在UI线程上创建BitmapImage
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => LoadAlbumArtFromData());
                return;
            }

            try
            {
                _isLoadingAlbumArt = true; // 设置加载标志

                // 直接使用字节数组，无需从Base64转换（优化内存使用）
                var imageBytes = _albumArtData;

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return;
                }

                // 验证图像数据完整性
                if (imageBytes.Length < 100) // 太小的数据可能不是有效的图像
                {
                    _albumArt = null;
                    OnPropertyChanged(nameof(AlbumArt));
                    return;
                }

                // 检查图像文件头以验证格式
                if (!IsValidImageData(imageBytes))
                {
                    _albumArt = null;
                    OnPropertyChanged(nameof(AlbumArt));
                    return;
                }

                // 尝试多种图像加载方法，优先使用预缩放版本
                BitmapImage? loadedImage = null;

                // 方法1: 预缩放加载（推荐，节省内存）
                loadedImage = TryLoadImageWithPreScaling(imageBytes, 160); // 预缩放到160px宽度

                // 方法2: 标准加载
                if (loadedImage == null)
                {
                    loadedImage = TryLoadImage(imageBytes, BitmapCreateOptions.None);
                }

                // 方法3: 忽略缓存加载
                if (loadedImage == null)
                {
                    loadedImage = TryLoadImage(imageBytes, BitmapCreateOptions.IgnoreImageCache);
                }

                // 方法4: 延迟加载
                if (loadedImage == null)
                {
                    loadedImage = TryLoadImageWithDelayLoading(imageBytes);
                }

                // 方法5: 使用WPF原生方式作为最后手段
                if (loadedImage == null)
                {
                    loadedImage = TryLoadImageWithWPFNative(imageBytes);
                }

                // 方法6: 使用更安全的异步加载方式
                if (loadedImage == null)
                {
                    loadedImage = TryLoadImageWithSafeAsync(imageBytes);
                }

                if (loadedImage != null && loadedImage.Width > 0 && loadedImage.Height > 0)
                {
                    try
                    {
                        // 冻结图像以确保线程安全
                        loadedImage.Freeze();
                        _albumArt = loadedImage;
                        OnPropertyChanged(nameof(AlbumArt));
                    //    System.Diagnostics.Debug.WriteLine($"Song: 专辑封面加载成功 - {_title}, 尺寸: {loadedImage.Width}x{loadedImage.Height}");
                    }
                    catch (Exception freezeEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Song: 冻结图像失败 - {_title}, 错误: {freezeEx.Message}");
                        _albumArt = loadedImage;
                        OnPropertyChanged(nameof(AlbumArt));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Song: 专辑封面加载失败 - {_title}");
                    _albumArt = null;
                    OnPropertyChanged(nameof(AlbumArt));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Song: 加载专辑封面异常 - {_title}, 错误: {ex.Message}");
                _albumArt = null;
                OnPropertyChanged(nameof(AlbumArt));
            }
            finally
            {
                _isLoadingAlbumArt = false; // 无论成功失败，都清除加载标志
            }
        }

        /// <summary>
        /// 尝试加载图像数据 - 忽略颜色配置文件以避免WPF颜色上下文错误
        /// </summary>
        private BitmapImage? TryLoadImage(byte[] imageBytes, BitmapCreateOptions createOptions)
        {
            try
            {
                // 验证图像数据完整性
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return null;
                }

                // 使用using语句确保流正确释放
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();

                    try
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        // 添加IgnoreColorProfile选项避免颜色上下文错误
                        bitmap.CreateOptions = createOptions | BitmapCreateOptions.IgnoreColorProfile;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 冻结图像以确保线程安全
                    }
                    catch (Exception)
                    {
                    }

                    // 验证结果
                    if (bitmap.Width > 0 && bitmap.Height > 0)
                    {
                        return bitmap;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试延迟加载图像 - 忽略颜色配置文件以避免WPF颜色上下文错误
        /// </summary>
        private BitmapImage? TryLoadImageWithDelayLoading(byte[] imageBytes)
        {
            try
            {
                // 验证图像数据完整性
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return null;
                }

                // 使用using语句确保流正确释放
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();

                    try
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnDemand; // 延迟加载
                        // 添加IgnoreColorProfile选项避免颜色上下文错误
                        bitmap.CreateOptions = BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();

                        // 强制图像加载
                        bitmap.DownloadCompleted += (s, e) => { };
                    }
                    catch (Exception)
                    {
                    }

                    if (bitmap.Width > 0 && bitmap.Height > 0)
                    {
                        return bitmap;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 使用WPF原生的图像加载方式 - 忽略颜色配置文件
        /// </summary>
        private BitmapImage? TryLoadImageWithWPFNative(byte[] imageBytes)
        {
            try
            {
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    // 添加IgnoreColorProfile选项避免颜色上下文错误
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();

                    if (bitmap.Width > 0 && bitmap.Height > 0)
                    {
                        return bitmap;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试预缩放图像加载 - 节省内存的关键优化，忽略颜色配置文件
        /// </summary>
        private BitmapImage? TryLoadImageWithPreScaling(byte[] imageBytes, int maxWidth)
        {
            try
            {
                // 验证图像数据完整性
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return null;
                }

                // 使用using语句确保流正确释放
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();

                    try
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        // 添加IgnoreColorProfile选项避免颜色上下文错误
                        bitmap.CreateOptions = BitmapCreateOptions.None | BitmapCreateOptions.IgnoreColorProfile;

                        // 设置解码像素宽度，这是预缩放的关键
                        bitmap.DecodePixelWidth = maxWidth;

                        // 启用内存优化选项
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 冻结图像以确保线程安全
                    }
                    catch (Exception)
                    {
                    }

                    // 验证结果
                    if (bitmap.Width > 0 && bitmap.Height > 0)
                    {
                        return bitmap;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 验证图像数据的有效性
        /// </summary>
        private bool IsValidImageData(byte[] data)
        {
            if (data == null || data.Length < 8) return false;

            // 检查常见的图像文件头
            // JPEG: FF D8 FF
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
                data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
                return true;

            // GIF: "GIF"
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return true;

            // BMP: "BM"
            if (data[0] == 0x42 && data[1] == 0x4D)
                return true;

            return false;
        }

        /// <summary>
        /// 强制加载专辑封面（用于需要立即显示的情况）
        /// </summary>
        public void EnsureAlbumArtLoaded()
        {
            if (_albumArt == null)
            {
                // 优先从缓存加载
                if (TryLoadAlbumArtFromCache())
                {
                    return;
                }
                
                // 如果还没有加载封面数据，先从文件中加载封面数据
                if (_albumArtData == null || _albumArtData.Length == 0)
                {
                    EnsureAlbumArtDataLoaded(); // 使用新的方法，确保数据已加载
                }
                
                // 如果有封面数据，加载为图像
                if (_albumArtData != null && _albumArtData.Length > 0)
                {
                    LoadAlbumArtFromData();
                    // 保存到缓存
                    SaveAlbumArtToCache();
                }
            }
        }

        /// <summary>
        /// 强制加载原图专辑封面（用于旋转封面等需要高清显示的场合）
        /// </summary>
        public void EnsureOriginalAlbumArtLoaded()
        {
            // 如果还没有加载封面数据，先从文件中加载封面数据
            if (_albumArtData == null || _albumArtData.Length == 0)
            {
                EnsureAlbumArtDataLoaded();
            }
            
            // 如果有封面数据，加载原图
            if (_originalAlbumArt == null && _albumArtData != null && _albumArtData.Length > 0)
            {
                LoadOriginalAlbumArtFromData();
            }
        }

        /// <summary>
        /// 释放专辑封面资源（用于内存优化）
        /// 即走即清策略，直接释放资源
        /// </summary>
        public void ReleaseAlbumArt()
        {
            if (_albumArt != null)
            {
                try
                {
                    // 释放图像资源
                    if (_albumArt.Dispatcher != null && !_albumArt.Dispatcher.HasShutdownStarted)
                    {
                        _albumArt.Dispatcher.Invoke(() =>
                        {
                            if (_albumArt != null)
                            {
                                _albumArt.UriSource = null;
                                _albumArt.StreamSource = null;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Song: 释放封面资源失败 - {Title}, 错误: {ex.Message}");
                }

                _albumArt = null;
                OnPropertyChanged(nameof(AlbumArt));

                System.Diagnostics.Debug.WriteLine($"Song: 已释放缩略图封面 - {Title}");
            }
        }

        /// <summary>
        /// 释放原图专辑封面资源（用于内存优化）
        /// 即走即清策略，直接释放资源
        /// </summary>
        public void ReleaseOriginalAlbumArt()
        {
            if (_originalAlbumArt != null)
            {
                try
                {
                    // 释放图像资源
                    if (_originalAlbumArt.Dispatcher != null && !_originalAlbumArt.Dispatcher.HasShutdownStarted)
                    {
                        _originalAlbumArt.Dispatcher.Invoke(() =>
                        {
                            if (_originalAlbumArt != null)
                            {
                                _originalAlbumArt.UriSource = null;
                                _originalAlbumArt.StreamSource = null;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Song: 释放原图封面资源失败 - {Title}, 错误: {ex.Message}");
                }

                _originalAlbumArt = null;
                OnPropertyChanged(nameof(OriginalAlbumArt));

                System.Diagnostics.Debug.WriteLine($"Song: 已释放原图封面 - {Title}");
            }
        }

        /// <summary>
        /// 释放所有封面资源（包括缓存和原始数据）
        /// 即走即清策略，彻底释放内存
        /// </summary>
        public void ReleaseAllAlbumArt()
        {
            ReleaseAlbumArt();
            ReleaseOriginalAlbumArt();
            
            // 释放封面原始数据 - 这是最大的内存占用源
            if (_albumArtData != null)
            {
                _albumArtData = null;
                System.Diagnostics.Debug.WriteLine($"Song: 已释放封面原始数据 - {Title}");
            }

            System.Diagnostics.Debug.WriteLine($"Song: 已释放所有封面资源 - {Title}");
        }
        
        /// <summary>
        /// 确保封面数据已加载（如果需要从文件重新加载）
        /// 用于在释放数据后重新加载封面
        /// </summary>
        public void EnsureAlbumArtDataLoaded()
        {
            if (_albumArtData == null && !string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath))
            {
                LoadAlbumArtDataFromFile();
                //System.Diagnostics.Debug.WriteLine($"Song: 从文件重新加载封面数据 - {Title}");
                
                // 如果加载到了封面数据，保存到缓存
                if (_albumArtData != null && _albumArtData.Length > 0)
                {
                    SaveAlbumArtToCache();
                }
            }
        }



        /// <summary>
        /// 从数据中加载原图专辑封面 - 加载高清原图，不进行预缩放
        /// </summary>
        private void LoadOriginalAlbumArtFromData()
        {
            // 防止递归调用
            if (_isLoadingOriginalAlbumArt)
            {
                return;
            }

            if (_albumArtData == null || _albumArtData.Length == 0)
            {
                return;
            }

            try
            {
                _isLoadingOriginalAlbumArt = true; // 设置加载标志

                // 直接使用字节数组，无需从Base64转换（优化内存使用）
                var imageBytes = _albumArtData;

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return;
                }

                // 验证图像数据完整性
                if (imageBytes.Length < 100) // 太小的数据可能不是有效的图像
                {
                    _originalAlbumArt = null;
                    OnPropertyChanged(nameof(OriginalAlbumArt));
                    return;
                }

                // 检查图像文件头以验证格式
                if (!IsValidImageData(imageBytes))
                {
                    _originalAlbumArt = null;
                    OnPropertyChanged(nameof(OriginalAlbumArt));
                    return;
                }

                // 确保在UI线程上创建BitmapImage
                if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => LoadOriginalAlbumArtFromData());
                    return;
                }

                // 加载原图 - 使用using语句确保流正确释放
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();

                    try
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        // 添加IgnoreColorProfile选项避免颜色上下文错误
                        bitmap.CreateOptions = BitmapCreateOptions.None | BitmapCreateOptions.IgnoreColorProfile;

                        // 关键：不设置DecodePixelWidth，加载原图尺寸
                        // bitmap.DecodePixelWidth = maxWidth; // 注释掉这一行，加载原图

                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                    }
                    catch (Exception initEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"原图专辑封面初始化失败: {initEx.Message}");
                    }

                    // 验证结果
                    if (bitmap.Width > 0 && bitmap.Height > 0)
                    {
                        try
                        {
                            bitmap.Freeze(); // 冻结图像以确保线程安全
                        }
                        catch (Exception)
                        {
                        }
                        _originalAlbumArt = bitmap;
                        OnPropertyChanged(nameof(OriginalAlbumArt));
                    }
                    else
                    {
                        _originalAlbumArt = null;
                        OnPropertyChanged(nameof(OriginalAlbumArt));
                    }
                }
            }
            catch (Exception)
            {
                _originalAlbumArt = null;
                OnPropertyChanged(nameof(OriginalAlbumArt));
            }
            finally
            {
                _isLoadingOriginalAlbumArt = false; // 无论成功失败，都清除加载标志
            }
        }

        /// <summary>
        /// 使用更安全的异步加载方式 - 专门解决System.NotSupportedException异常
        /// </summary>
        private BitmapImage? TryLoadImageWithSafeAsync(byte[] imageBytes)
        {
            try
            {
                // 验证图像数据完整性
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return null;
                }

                // 检查图像文件头以验证格式
                if (!IsValidImageData(imageBytes))
                {
                    return null;
                }

                // 确保在UI线程上创建BitmapImage
                if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    BitmapImage? result = null;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        result = TryLoadImageWithSafeAsync(imageBytes);
                    });
                    return result;
                }

                // 使用using语句确保流正确释放
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();

                    try
                    {
                        bitmap.BeginInit();

                        // 关键安全设置：避免System.NotSupportedException异常
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.None |
                                               BitmapCreateOptions.IgnoreColorProfile;

                        // 设置解码像素宽度，避免内存问题
                        bitmap.DecodePixelWidth = 160;

                        bitmap.StreamSource = ms;
                        bitmap.EndInit();

                        // 不再等待下载，直接完成初始化
                        // 这可以防止NotSupportedException

                        // 验证结果
                        if (bitmap.Width > 0 && bitmap.Height > 0)
                        {
                            // 立即冻结，避免跨线程访问问题
                            bitmap.Freeze();
                            return bitmap;
                        }
                    }
                    catch (System.NotSupportedException nsEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Song: 安全异步加载 - NotSupportedException: {nsEx.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Song: 安全异步加载异常 - {ex.Message}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Song: 安全异步加载外层异常 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 确定两个Song对象是否相等（基于文件路径）
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is Song other)
            {
                return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// 获取Song对象的哈希码（基于文件路径）
        /// </summary>
        public override int GetHashCode()
        {
            return FilePath?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
        }

        /// <summary>
        /// 生成文件路径的哈希值，用于缓存文件名
        /// </summary>
        /// <returns>文件路径的SHA1哈希值</returns>
        private string GenerateFileHash()
        {
            if (string.IsNullOrEmpty(FilePath))
                return string.Empty;

            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(FilePath);
                var hash = sha1.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// 获取封面缓存文件路径
        /// </summary>
        /// <returns>封面缓存文件的完整路径</returns>
        private string GetAlbumArtCachePath()
        {
            var hash = GenerateFileHash();
            return Path.Combine(Paths.AlbumArtCacheDirectory, $"{hash}.png");
        }

        /// <summary>
        /// 从缓存加载封面
        /// </summary>
        /// <returns>是否成功从缓存加载</returns>
        private bool TryLoadAlbumArtFromCache()
        {
            var cachePath = GetAlbumArtCachePath();
            if (File.Exists(cachePath))
            {
                try
                {
                    var imageBytes = File.ReadAllBytes(cachePath);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        // 使用现有的图像加载逻辑
                        _albumArtData = imageBytes;
                        LoadAlbumArtFromData();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Song: 从缓存加载封面失败 - {Title}, 错误: {ex.Message}");
                }
            }
            return false;
        }

        /// <summary>
        /// 将封面保存到缓存
        /// </summary>
        private void SaveAlbumArtToCache()
        {
            // 根据静态配置决定是否保存缓存
            if (!IsCoverCacheEnabled || _albumArtData == null || _albumArtData.Length == 0)
                return;

            try
            {
                // 确保缓存目录存在
                Paths.EnsureDirectoryExists(Paths.AlbumArtCacheDirectory);
                
                var cachePath = GetAlbumArtCachePath();
                File.WriteAllBytes(cachePath, _albumArtData);
                //System.Diagnostics.Debug.WriteLine($"Song: 封面已保存到缓存 - {Title}, 路径: {cachePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Song: 保存封面到缓存失败 - {Title}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取音频文件格式（扩展名）
        /// </summary>
        public string AudioFormat
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return string.Empty;

                return System.IO.Path.GetExtension(FilePath).ToLowerInvariant();
            }
        }

        /// <summary>
        /// 返回Song对象的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"{Title} - {Artist}";
        }

        /// <summary>
        /// 格式化的歌曲时长文本
        /// </summary>
        public string TotalTimeText
        {
            get
            {
                if (Duration == TimeSpan.Zero)
                    return "--:--";

                if (Duration.TotalHours >= 1)
                    return $"{(int)Duration.TotalHours}:{Duration:mm\\:ss}";
                else
                    return Duration.ToString(@"mm\:ss");
            }
        }
    }
}
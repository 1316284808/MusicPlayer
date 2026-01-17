using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;
using System.Windows.Input;
using System.ComponentModel;
using ChinesePinyinConverter;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌手页面视图模型
    /// </summary>
    public class SingerViewModel : ObservableObject, ISingerViewModel
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly IMessagingService _messagingService;

        private ObservableCollection<SingerInfo> _singers = new();
        private ObservableCollection<SingerInfo> _filteredSingers = new();
        private string _currentIndex = "ALL";
        private string _searchText = string.Empty;
        private bool _isSearchExpanded = false;
        private readonly List<string> _indexList = new() { "ALL","#", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

     

        /// <summary>
        /// 歌手列表
        /// </summary>
        public ObservableCollection<SingerInfo> Singers => _singers;

        /// <summary>
        /// 过滤后的歌手列表
        /// </summary>
        public ObservableCollection<SingerInfo> FilteredSingers => _filteredSingers;

        /// <summary>
        /// 歌手总数
        /// </summary>
        public int SingerCount => _singers.Count;

        /// <summary>
        /// 索引列表
        /// </summary>
        public List<string> IndexList => _indexList;

        /// <summary>
        /// 当前选中的索引
        /// </summary>
        public string CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (_currentIndex != value)
                {
                    _currentIndex = value;
                    OnPropertyChanged(nameof(CurrentIndex));
                    FilterSingersByIndex(value);
                }
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterSingersByIndexAndSearch(_currentIndex, value);
                }
            }
        }

        /// <summary>
        /// 搜索框是否展开
        /// </summary>
        public bool IsSearchExpanded
        {
            get => _isSearchExpanded;
            set
            {
                if (_isSearchExpanded != value)
                {
                    _isSearchExpanded = value;
                    OnPropertyChanged(nameof(IsSearchExpanded));
                }
            }
        }

        /// <summary>
        /// 播放歌手歌曲命令
        /// </summary>
        public ICommand PlaySingerCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        public ICommand SearchButtonClickCommand { get; }

        public SingerViewModel(
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService,
            IMessagingService messagingService)
        {
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));

            PlaySingerCommand = new RelayCommand<string>(ExecutePlaySinger);
            SearchButtonClickCommand = new RelayCommand(ExecuteSearchButtonClick);
            
            // 注册消息处理器
            RegisterMessageHandlers();
            
            LoadSingers();
        }
        
        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public override void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("SingerViewModel: Initialize 方法被调用");
            LoadSingers();
        }

        /// <summary>
        /// 清理视图模型资源
        /// </summary>
        public override void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("SingerViewModel: Cleanup 方法被调用");
            
            // 清理所有歌手的封面资源
            foreach (var singer in _singers)
            {
                singer.CoverImage = null;
            }
            
            // 清理过滤后的歌手列表的封面资源
            foreach (var singer in _filteredSingers)
            {
                singer.CoverImage = null;
            }
        }

        /// <summary>
        /// 加载歌手列表
        /// </summary>
        public void LoadSingers()
        {
            System.Diagnostics.Debug.WriteLine("SingerViewModel: 开始加载歌手列表");

            // 从播放列表中获取所有歌手
            var playlist = _playlistDataService.DataSource;
            System.Diagnostics.Debug.WriteLine($"SingerViewModel: 获取到播放列表，歌曲数量: {playlist?.Count}");

            var singerGroups = (playlist ?? Enumerable.Empty<Core.Models.Song>())
                .Where(song => !string.IsNullOrEmpty(song.Artist))
                .GroupBy(song => song.Artist!)
                .Select(group =>
                {
                    // 获取该歌手的第一首歌曲，用于懒加载封面
                    var firstSong = group.FirstOrDefault();

                    return new SingerInfo
                    {
                        Name = group.Key,
                        SongCount = group.Count(),
                        CoverImage = null, // 初始时不加载封面，支持懒加载
                        FirstSongFilePath = firstSong?.FilePath // 保存第一首歌的路径，用于懒加载
                    };
                })
                .OrderBy(singer => singer.Name) // 默认按名称排序
                .ToList();

            System.Diagnostics.Debug.WriteLine($"SingerViewModel: 找到 {singerGroups.Count} 位歌手");

            // 更新UI
            _singers.Clear();
            foreach (var singer in singerGroups)
            {
                _singers.Add(singer);
            }

            // 初始化过滤后的歌手列表
            FilterSingersByIndex(_currentIndex);

            // 通知UI更新
            OnPropertyChanged(nameof(SingerCount));
            OnPropertyChanged(nameof(Singers)); // 通知Singers集合已更新
            OnPropertyChanged(nameof(FilteredSingers)); // 通知FilteredSingers集合已更新
            System.Diagnostics.Debug.WriteLine($"SingerViewModel: 歌手列表加载完成，歌手数量: {_singers.Count}");

            // 通知SingerAlbumArtBehavior重新加载封面
            //System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    System.Diagnostics.Debug.WriteLine("SingerViewModel: 通知SingerAlbumArtBehavior数据已更新");
            //}));
        }

        /// <summary>
        /// 刷新歌手数据
        /// </summary>
        public void RefreshSingers()
        {
            LoadSingers();
        }

        // 当前播放状态
        private bool _isPlaying = false;
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 监听当前歌曲变化消息
            _messagingService.Register<CurrentSongChangedMessage>(this, OnCurrentSongChanged);
            
            // 监听播放状态变化消息
            _messagingService.Register<PlaybackStateChangedMessage>(this, OnPlaybackStateChanged);
        }
        
        /// <summary>
        /// 处理当前歌曲变化消息
        /// </summary>
        private void OnCurrentSongChanged(object recipient, CurrentSongChangedMessage message)
        {
            UpdateSingerPlayingState(message.Value, _isPlaying);
        }
        
        /// <summary>
        /// 处理播放状态变化消息
        /// </summary>
        private void OnPlaybackStateChanged(object recipient, PlaybackStateChangedMessage message)
        {
            _isPlaying = message.Value;
            UpdateSingerPlayingState(_playlistDataService.CurrentSong, _isPlaying);
        }
        
        /// <summary>
        /// 更新歌手播放状态
        /// </summary>
        private void UpdateSingerPlayingState(Core.Models.Song? currentSong, bool isPlaying)
        {
            // 先重置所有歌手的播放状态
            foreach (var singer in _singers)
            {
                singer.IsPlaying = false;
            }
            
            // 如果有当前播放歌曲，且歌手不为空，则更新对应歌手的播放状态
            if (currentSong != null && !string.IsNullOrEmpty(currentSong.Artist))
            {
                var singer = _singers.FirstOrDefault(s => 
                    string.Equals(s.Name, currentSong.Artist, StringComparison.OrdinalIgnoreCase));
                
                if (singer != null)
                {
                    // 从PlayerStateService获取当前播放上下文
                    var context = _playbackContextService.CurrentPlaybackContext;
                    singer.IsPlaying = isPlaying && 
                                      context.Type == Core.Enums.PlaybackContextType.Artist && 
                                      string.Equals(context.Identifier, singer.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        
        /// <summary>
        /// 执行播放选中歌手的歌曲
        /// </summary>
        /// <param name="singerName">歌手名称</param>
        private void ExecutePlaySinger(string? singerName)
        {
            if (string.IsNullOrEmpty(singerName))
                return;

            // 获取该歌手的第一首歌
            var playlist = _playlistDataService.DataSource;
            var firstSong = playlist
                .Where(song => !string.IsNullOrEmpty(song.Artist) && 
                              string.Equals(song.Artist, singerName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (firstSong != null)
            {
                // 设置播放上下文为歌手列表
                var context = PlaybackContext.CreateArtist(singerName);
                _playbackContextService.SetPlaybackContext(context.Type, context.Identifier, context.DisplayName);
                
                System.Diagnostics.Debug.WriteLine($"SingerViewModel: 设置播放上下文为歌手: {singerName}");

                // 发送播放消息
                _messagingService.Send(new PlaySelectedSongMessage(firstSong));
                
                // 更新歌手播放状态 - 发送播放消息后，播放状态会变为true
                UpdateSingerPlayingState(firstSong, true);
            }
        }
        
        /// <summary>
        /// 根据索引过滤歌手列表
        /// </summary>
        /// <param name="index">选中的索引</param>
        /// <summary>
        /// 执行搜索按钮点击
        /// </summary>
        private void ExecuteSearchButtonClick()
        {
            if (!IsSearchExpanded)
            {
                IsSearchExpanded = true;
                // 搜索框展开时，将索引定位到ALL，确保能显示所有符合条件的歌手
                CurrentIndex = "ALL";
                // 发送消息请求搜索框获取焦点
                _messagingService.Send(new SearchBoxFocusRequestMessage());
            }
            else
            {
                SearchText = string.Empty;
                IsSearchExpanded = false;
            }
        }

        /// <summary>
        /// 根据索引过滤歌手列表
        /// </summary>
        /// <param name="index">选中的索引</param>
        private void FilterSingersByIndex(string index)
        {
            FilterSingersByIndexAndSearch(index, SearchText);
        }

        /// <summary>
        /// 根据索引和搜索文本过滤歌手列表
        /// </summary>
        /// <param name="index">选中的索引</param>
        /// <param name="searchText">搜索文本</param>
        private void FilterSingersByIndexAndSearch(string index, string searchText)
        {
            System.Diagnostics.Debug.WriteLine($"SingerViewModel: 根据索引 {index} 和搜索文本 '{searchText}' 过滤歌手列表");
            
            _filteredSingers.Clear();
            
            var filteredByIndex = _singers.Where(singer =>
            {
                if (index == "ALL")
                {
                    return true;
                }
                else if (index == "#")
                {
                    string firstChar = singer.Name.Substring(0, 1).ToUpper();
                    
                    // 检查是否是中文
                    if (IsChineseChar(firstChar[0]))
                    {
                        // 获取中文首字母
                        string firstPinyin = GetFirstPinyin(firstChar[0]);
                        return firstPinyin == "#";
                    }
                    else
                    {
                        // 检查是否是A-Z字母
                        char firstCharValue = firstChar[0];
                        return !(firstCharValue >= 'A' && firstCharValue <= 'Z');
                    }
                }
                else
                {
                    string firstChar = singer.Name.Substring(0, 1).ToUpper();
                    
                    // 检查是否是中文
                    if (IsChineseChar(firstChar[0]))
                    {
                        // 获取中文首字母
                        string firstPinyin = GetFirstPinyin(firstChar[0]);
                        return firstPinyin == index;
                    }
                    else
                    {
                        // 直接比较首字母
                        return firstChar == index;
                    }
                }
            });

            // 应用搜索过滤
            var finalFiltered = string.IsNullOrWhiteSpace(searchText)
                ? filteredByIndex
                : filteredByIndex.Where(singer => singer.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            
            // 添加到过滤后的集合
            foreach (var singer in finalFiltered)
            {
                _filteredSingers.Add(singer);
            }
            
            System.Diagnostics.Debug.WriteLine($"SingerViewModel: 过滤完成，显示 {_filteredSingers.Count} 位歌手");
            OnPropertyChanged(nameof(FilteredSingers));
        }
        
        /// <summary>
        /// 判断字符是否为中文
        /// </summary>
        /// <param name="c">字符</param>
        /// <returns>是否为中文</returns>
        private bool IsChineseChar(char c)
        {
            return c >= 0x4E00 && c <= 0x9FFF;
        }
        
        /// <summary>
        /// 获取中文字符的拼音首字母
        /// </summary>
        /// <param name="c">中文字符</param>
        /// <returns>拼音首字母</returns>
        private string GetFirstPinyin(char c)
        {
            try
            {
                // 使用CingZeoi.ChinesePinyinConverter库获取拼音
                string charStr = c.ToString();
                var pinyinResult = PinyinConverter.ConvertToPinyin(charStr, false);
                if (pinyinResult != null)
                {
                    // 获取第一个拼音
                    var firstPinyin = pinyinResult.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstPinyin))
                    {
                        // 获取首字母
                        return firstPinyin.ToUpper().Substring(0, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerViewModel: 获取拼音首字母失败: {ex.Message}");
            }
            
            // 如果获取失败，返回默认值
            return "#";
        }
    }
}
# 🎵 MusicPlayer
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)
![WPF](https://img.shields.io/badge/WPF-UI-blue?style=flat-square)
![MVVM](https://img.shields.io/badge/Architecture-MVVM-green?style=flat-square)

## ✨ 主要特性

### 🎧 音频播放功能
- **多格式支持**: MP3, WAV, FLAC, OGG/Vorbis 等主流音频格式
- **完整播放控制**: 播放/暂停/停止、上一首/下一首、进度跳转、快进/快退
- **音频信息展示**: 显示歌曲标题、艺术家、专辑信息和专辑封面
- **音频可视化**: 实时频谱分析器，提供线形和环形两种视觉反馈模式
- **系统媒体控制**: 集成 Windows 系统媒体传输控制 (SMTC)，支持系统级媒体控制
- **均衡器支持**: 多频段均衡器，提供多种预设和自定义调节
- **歌词显示**: 支持歌词文件加载和同步显示，包括多行和逐字显示模式
- **收藏功能**: 支持歌曲收藏和收藏列表管理

### 📚 播放列表管理
- **本地音乐导入**: 支持单文件或批量导入音乐文件
- **搜索筛选**: 快速搜索和筛选播放列表中的歌曲
- **智能元数据**: 自动读取音乐文件的元数据信息
- **数据持久化**: 本地 LiteDB 数据库存储播放列表信息
- **灵活排序**: 支持按添加时间、标题、艺术家、专辑、时长、文件大小等多种规则排序
- **播放列表缓存**: 智能缓存机制，提高大量歌曲场景下的响应速度
- **歌曲管理**: 支持歌曲删除、收藏等操作
- **专辑和歌手视图**: 支持按专辑和歌手分类浏览音乐

### 🎨 现代化界面
- **Fluent Design**: 采用 WPF-UI 库实现的 Fluent Design 设计语言
- **自定义控件**: 模块化控件设计，包括标题栏、控制栏、播放列表、专辑、歌手等
- **响应式布局**: 自适应窗口大小变化
- **系统托盘**: 最小化到系统托盘，支持托盘菜单和快捷操作
- **歌词窗口**: 独立的歌词显示窗口，支持与主窗口同步
- **主题切换**: 支持四种不同的主题：None、Acrylic、Mica、MicaAlt

### ⚙️ 设置与配置
- **播放设置**: 音量控制、播放模式（顺序播放、单曲循环、列表循环、随机播放）
- **频谱设置**: 可启用/禁用频谱分析显示，支持线形和环形两种模式
- **均衡器设置**: 多频段均衡器调节，预设管理和自定义保存
- **音频引擎**: 支持多种音频引擎选择和配置
- **窗口行为**: 可配置关闭窗口时是完全退出还是最小化到托盘
- **播放列表设置**: 自定义播放列表显示和行为
- **配置持久化**: 所有设置自动保存到本地 LiteDB 数据库

### 🏗️ 分层架构设计
- **核心层 (MusicPlayer.Core)**: 包含数据模型、服务接口定义、音频处理组件和数据访问层
- **服务层 (MusicPlayer.Services)**: 业务逻辑服务实现，包括播放器服务、配置服务、消息处理等
- **表现层 (MusicPlayer)**: WPF 用户界面、视图模型和自定义控件
- **依赖注入**: 使用 Microsoft.Extensions.DependencyInjection 进行服务管理和生命周期控制
- **消息通信**: 基于消息模式的松耦合组件通信机制
- **日志系统**: 基于 Microsoft.Extensions.Logging 的日志系统，支持文件日志输出和分级记录

## 📱 界面预览

### 默认播放列表
![默认播放列表](Image/默认列表.png)

### 音乐库
![音乐库](Image/音乐库.png)

### 音频可视化效果
![显示频谱](Image/显示频谱.png)

### 关闭频谱显示
![关闭频谱](Image/关闭频谱.png)

### 歌手列表
![歌手列表](Image/歌手列表.png)

### 专辑列表
![专辑列表](Image/专辑列表.png)

### 数据管理
![数据管理](Image/数据管理.png)

 ### 设置界面
![设置界面](Image/设置界面.png)

### 音频设置
![音频设置](Image/音频设置.png)

## 🏗️ 技术架构

### 核心技术栈
- **开发框架**: C# + WPF (.NET 8.0)
- **UI 框架**: [WPF-UI 4.1.0](https://github.com/lepoco/wpfui) - 现代化 WPF UI 组件库
- **音频处理**: [NAudio 2.2.1](https://github.com/naudio/NAudio) + NAudio.Vorbis 1.5.0
- **元数据处理**: [TagLibSharp 2.3.0](https://github.com/mono/taglib-sharp)
- **MVVM 框架**: CommunityToolkit.Mvvm 8.4.0
- **依赖注入**: Microsoft.Extensions.DependencyInjection 10.0.0
- **数据存储**: [LiteDB 5.0.17](https://github.com/mbdavid/LiteDB) - 轻量级 NoSQL 数据库
- **系统托盘**: Hardcodet.NotifyIcon.Wpf 2.0.1
- **拼音转换**: CingZeoi.ChinesePinyinConverter 1.0.0
- **内存缓存**: System.Runtime.Caching 8.0.0
- **日志系统**: Microsoft.Extensions.Logging 10.0.0
- **混色文本**:[TextHighlighterTest] (https://github.com/TwilightLemon/TextHighlighterTest)
  （注：项目TextHighlighterTest采用MIT协议，在使用过程中基于该开源项目官方源码（commit:[2cbcf10]）有修改部分源码，感谢开源）
- **网格布局**:（VirtualizingWrapPanel）(https://github.com/sbaeumlisberger/VirtualizingWrapPanel)  
  （注：截至 2026-01-18，NuGet 上尚未提供支持  .NET 8 的 VirtualizingWrapPanel 2.4.1 版本。因此，本项目基于其官方源码（commit: [f5bd4c5f]）编译为程序集直接引用。  
  **未对原始代码做任何修改**，仅用于启用虚拟化网格布局功能。  
  一旦官方发布兼容的 NuGet 包，将立即切换回标准 NuGet 引用方式。）
 


### 架构模式
- **设计模式**: MVVM (Model-View-ViewModel)
- **分层架构**: 核心层与表现层分离，提高代码可维护性
- **服务定位器**: 使用 ServiceLocator 模式进行服务管理
- **事件通信**: 通过消息服务实现组件间通信

## 📦 项目结构

```
MusicPlayer/
├── MusicPlayer.sln                 # 解决方案文件
├── MusicPlayer.Core/               # 核心层项目
│   ├── MusicPlayer.Core.csproj     # 核心项目配置
│   ├── Audio/                      # 音频处理模块
│   │   ├── AudioEngineManager.cs   # 音频引擎管理器
│   │   ├── EqualizerFilter.cs      # 均衡器过滤器
│   │   ├── EqualizerStream.cs      # 均衡器流
│   │   ├── SpectrumAnalyzer.cs     # 频谱分析器
│   │   └── VorbisAudioFileReader.cs # OGG 格式音频读取器
│   ├── Data/                       # 数据访问层
│   │   ├── AlbumArtLoader.cs       # 专辑封面加载器
│   │   ├── ConfigurationDAL.cs     # 配置数据访问
│   │   ├── DBHelper.cs             # 数据库帮助类
│   │   ├── EqualizerPresetsDAL.cs  # 均衡器预设数据访问
│   │   └── PlaylistDataDAL.cs      # 播放列表数据访问
│   ├── Enums/                      # 枚举定义
│   │   ├── AudioEngine.cs          # 音频引擎枚举
│   │   ├── DataChangeType.cs       # 数据变更类型
│   │   ├── IconKind.cs             # 图标类型枚举
│   │   ├── MessageType.cs          # 消息类型枚举
│   │   ├── PageEnums.cs            # 页面枚举
│   │   ├── PlayMode.cs             # 播放模式枚举
│   │   ├── PlaybackContextType.cs  # 播放上下文类型
│   │   ├── SortRule.cs             # 排序规则枚举
│   │   └── Theme.cs                # 主题枚举
│   ├── Interface/                  # 服务接口定义
│   │   ├── IConfigurationService.cs # 配置服务接口
│   │   ├── ICustomPlaylistService.cs # 自定义播放列表服务接口
│   │   ├── IDialogService.cs       # 对话框服务接口
│   │   ├── IDispatcherService.cs   # 调度器服务接口
│   │   ├── IEqualizerPresetRepository.cs # 均衡器预设仓储接口
│   │   ├── IEqualizerService.cs    # 均衡器服务接口
│   │   ├── ILyricsService.cs       # 歌词服务接口
│   │   ├── IMessagingService.cs    # 消息服务接口
│   │   ├── INotificationService.cs # 通知服务接口
│   │   ├── IPlaybackContextProvider.cs # 播放上下文提供者接口
│   │   ├── IPlaybackContextService.cs # 播放上下文服务接口
│   │   ├── IPlaybackCoordinator.cs # 播放协调器接口
│   │   ├── IPlayerService.cs       # 播放器服务接口
│   │   ├── IPlayerStateService.cs  # 播放状态服务接口
│   │   ├── IPlaylistBusinessService.cs # 播放列表业务服务接口
│   │   ├── IPlaylistCacheService.cs # 播放列表缓存服务接口
│   │   ├── IPlaylistCommandHandler.cs # 播放列表命令处理器接口
│   │   ├── IPlaylistDataService.cs # 播放列表数据服务接口
│   │   ├── IPlaylistNavigationService.cs # 播放列表导航服务接口
│   │   ├── IPlaylistService.cs     # 播放列表服务接口
│   │   ├── IPlaylistStateService.cs # 播放列表状态服务接口
│   │   ├── IServiceCoordinator.cs  # 服务协调器接口
│   │   ├── IStateUpdater.cs        # 状态更新器接口
│   │   ├── ISystemMediaTransportService.cs # 系统媒体传输服务接口
│   │   ├── ISystemTrayService.cs   # 系统托盘服务接口
│   │   ├── ITimerService.cs        # 定时器服务接口
│   │   ├── IUINotificationService.cs # UI通知服务接口
│   │   └── IconService.cs          # 图标服务
│   ├── Models/                     # 数据模型
│   │   ├── AlbumInfo.cs            # 专辑信息模型
│   │   ├── EqualizerPreset.cs      # 均衡器预设模型
│   │   ├── EqualizerSettings.cs    # 均衡器设置模型
│   │   ├── ErrorInfo.cs            # 错误信息模型
│   │   ├── LyricLine.cs            # 歌词行模型
│   │   ├── Paths.cs                # 路径管理类
│   │   ├── PlaybackContext.cs      # 播放上下文模型
│   │   ├── PlayerConfiguration.cs  # 播放器配置模型
│   │   ├── PlayerStateModel.cs     # 播放器状态模型
│   │   ├── PlayerStatusInfo.cs     # 播放状态信息模型
│   │   ├── PlayerStatusResponse.cs # 播放状态响应模型
│   │   ├── Playlist.cs             # 播放列表模型
│   │   ├── PlaylistDetailParams.cs # 播放列表详情参数模型
│   │   ├── PlaylistSong.cs         # 播放列表歌曲模型
│   │   ├── SingerInfo.cs           # 歌手信息模型
│   │   ├── Song.cs                 # 歌曲信息模型
│   │   └── SortOption.cs           # 排序选项模型
├── MusicPlayer.Services/           # 服务层项目
│   ├── MusicPlayer.Services.csproj # 服务项目配置
│   ├── Coordinators/               # 协调器
│   │   └── PlaybackCoordinator.cs  # 播放协调器
│   ├── Handlers/                   # 消息处理器
│   │   ├── PlayerControlMessageHandler.cs # 播放控制消息处理器
│   │   ├── PlaylistMessageHandler.cs # 播放列表消息处理器
│   │   └── SystemMessageHandler.cs # 系统消息处理器
│   ├── Messages/                   # 消息定义
│   │   ├── ApplicationMessages.cs  # 应用程序消息
│   │   ├── AudioEngineChangedMessage.cs # 音频引擎变更消息
│   │   ├── CloseLyricsWindowMessage.cs # 关闭歌词窗口消息
│   │   ├── ErrorHandlingMessages.cs # 错误处理消息
│   │   ├── PlaybackMessages.cs      # 播放消息
│   │   ├── PlaylistMessages.cs     # 播放列表消息
│   │   ├── SavePresetFocusRequestMessage.cs # 保存预设焦点请求消息
│   │   ├── ServiceCoordinatorMessages.cs # 服务协调器消息
│   │   ├── SongDeletionMessages.cs # 歌曲删除消息
│   │   ├── SongFavoriteMessages.cs # 歌曲收藏消息
│   │   ├── SystemMessages.cs       # 系统消息
│   │   ├── VolumeMessages.cs       # 音量消息
│   │   └── WindowMessages.cs       # 窗口消息
│   ├── Providers/                  # 提供者
│   │   ├── AlbumProvider.cs        # 专辑提供者
│   │   ├── ArtistProvider.cs       # 艺术家提供者
│   │   ├── BasePlaybackContextProvider.cs # 基础播放上下文提供者
│   │   ├── CustomPlaylistProvider.cs # 自定义播放列表提供者
│   │   ├── DefaultPlaylistProvider.cs # 默认播放列表提供者
│   │   └── FavoritesProvider.cs    # 收藏夹提供者
│   └── Services/                   # 服务实现
│       ├── ConfigurationService.cs # 配置服务实现
│       ├── CustomPlaylistService.cs # 自定义播放列表服务实现
│       ├── DispatcherService.cs    # 调度器服务实现
│       ├── EqualizerPresetRepository.cs # 均衡器预设仓储实现
│       ├── EqualizerService.cs      # 均衡器服务实现
│       ├── ErrorHandlingService.cs # 错误处理服务实现
│       ├── LifecycleManagementService.cs # 生命周期管理服务
│       ├── LyricsService.cs        # 歌词服务实现
│       ├── MemoryMonitorService.cs # 内存监控服务实现
│       ├── MessagingService.cs     # 消息服务实现
│       ├── NotificationService.cs  # 通知服务实现
│       ├── PlaybackContextService.cs # 播放上下文服务实现
│       ├── PlayerService.cs        # 播放器服务实现
│       ├── PlayerStateService.cs   # 播放状态服务实现
│       ├── PlaylistBusinessService.cs # 播放列表业务服务实现
│       ├── PlaylistCacheService.cs # 播放列表缓存服务实现
│       ├── PlaylistCommandHandler.cs # 播放列表命令处理器
│       ├── PlaylistDataService.cs # 播放列表数据服务实现
│       ├── PlaylistNavigationService.cs # 播放列表导航服务实现
│       ├── PlaylistService.cs      # 播放列表服务实现
│       ├── PlaylistStateService.cs # 播放列表状态服务实现
│       ├── ServiceCoordinator.cs   # 服务协调器实现
│       ├── ServiceInitializationManager.cs # 服务初始化管理器
│       ├── TimerService.cs         # 定时器服务实现
│       └── WindowManagerService.cs # 窗口管理服务实现
├── MusicPlayer/                    # 表现层项目
│   ├── MusicPlayer.csproj          # 主项目配置
│   ├── App.xaml/cs                 # 应用程序入口
│   ├── MainWindow.xaml/cs          # 主窗口界面
│   ├── LyricsWindow.xaml/cs        # 歌词窗口
│   ├── Config/                     # 应用程序配置
│   │   ├── AppStartup.cs           # 应用启动类
│   │   ├── ApplicationInitializationService.cs # 应用初始化服务
│   │   ├── PlaybackContextInitializationService.cs # 播放上下文初始化服务
│   │   ├── ServiceCollectionExtensions.cs # 服务集合扩展
│   │   ├── SpectrumAnalyzerManager.cs # 频谱分析器管理器
│   │   ├── ViewModelLifecycleManager.cs # 视图模型生命周期管理器
│   │   └── WpfApplicationExtensions.cs # WPF应用扩展
│   ├── Controls/                   # 自定义控件
│   │   ├── AlbumControl.xaml/cs    # 专辑控件
│   │   ├── BackgroundControl.xaml/cs # 背景控件
│   │   ├── CenterContentControl.xaml/cs # 中心内容控件
│   │   ├── CircularSpectrumControl.xaml/cs # 环形频谱控件
│   │   ├── ControlBarControl.xaml/cs # 控制栏控件
│   │   ├── HeartControl.xaml/cs    # 收藏控件
│   │   ├── MultiLineLyricControl.xaml/cs # 多行歌词控件
│   │   ├── PlaylistControl.xaml/cs # 播放列表控件
│   │   ├── PlaylistDetailControl.xaml/cs # 播放列表详情控件
│   │   ├── PlaylistSettingControl.xaml/cs # 播放列表设置控件
│   │   ├── SettingsBarControl.xaml/cs # 设置栏控件
│   │   ├── SingerControl.xaml/cs   # 歌手控件
│   │   ├── SoundSettingsControl.xaml/cs # 声音设置控件
│   │   ├── SpectrumAnalyzerControl.xaml/cs # 频谱分析控件
│   │   ├── TitleBarControl.xaml/cs # 标题栏控件
│   │   ├── WindowSettingsControl.xaml/cs # 窗口设置控件
│   │   └── WordByWordLyricControl.xaml/cs # 逐字歌词控件
│   ├── Converters/                 # WPF 值转换器
│   │   └── StringToVisibilityConverter.cs # 字符串到可见性转换器
│   ├── Helper/                     # 辅助类
│   │   ├── Behavior/               # 行为类
│   │   ├── Converters/             # 转换器
│   │   ├── Services/               # 辅助服务
│   │   ├── Shaders/                # 着色器
│   │   ├── ButtonControl.xaml/cs   # 按钮控件
│   │   ├── CustomIcon.cs           # 自定义图标
│   │   └── HighlightTextBlock.xaml/cs # 高亮文本块
│   ├── Logging/                    # 日志模块
│   │   ├── FileLogger.cs           # 文件日志器
│   │   ├── FileLoggerExtensions.cs # 文件日志器扩展
│   │   └── FileLoggerProvider.cs   # 文件日志器提供者
│   ├── Navigation/                 # 导航模块
│   │   ├── AnimatedFrame.cs        # 动画框架
│   │   └── NavigationService.cs    # 导航服务
│   ├── Page/                       # 页面
│   │   ├── AlbumPage.xaml/cs       # 专辑页面
│   │   ├── HeartPage.xaml/cs       # 收藏页面
│   │   ├── PlayerPage.xaml/cs      # 播放器页面
│   │   ├── PlaylistDetailPage.xaml/cs # 播放列表详情页面
│   │   ├── PlaylistPage.xaml/cs    # 播放列表页面
│   │   ├── SettingsPage.xaml/cs    # 设置页面
│   │   └── SingerPage.xaml/cs      # 歌手页面
│   ├── Styles/                     # 样式
│   │   ├── ComboBoxStyles.xaml     # 组合框样式
│   │   └── MainStyles.xaml         # 主样式
│   ├── ViewModels/                 # 视图模型
│   │   ├── IViewModels/            # 视图模型接口
│   │   ├── AlbumViewModel.cs       # 专辑视图模型
│   │   ├── BackgroundViewModel.cs  # 背景视图模型
│   │   ├── CenterContentViewModel.cs # 中心内容视图模型
│   │   ├── ControlBarViewModel.cs  # 控制栏视图模型
│   │   ├── HeartViewModel.cs       # 收藏视图模型
│   │   ├── LyricsViewModel.cs       # 歌词视图模型
│   │   ├── LyricsViewModelFactory.cs # 歌词视图模型工厂
│   │   ├── MainViewModel.cs        # 主视图模型
│   │   ├── ObservableObject.cs     # MVVM 基类
│   │   ├── PlaylistDetailViewModel.cs # 播放列表详情视图模型
│   │   ├── PlaylistSettingViewModel.cs # 播放列表设置视图模型
│   │   ├── PlaylistViewModel.cs    # 播放列表视图模型
│   │   ├── SettingsBarViewModel.cs # 设置栏视图模型
│   │   ├── SettingsPageViewModel.cs # 设置页面视图模型
│   │   ├── SingerViewModel.cs      # 歌手视图模型
│   │   ├── SoundSettingsViewModel.cs # 声音设置视图模型
│   │   ├── SpectrumAnalyzerViewModel.cs # 频谱分析视图模型
│   │   ├── TitleBarViewModel.cs    # 标题栏视图模型
│   │   └── WindowSettingsViewModel.cs # 窗口设置视图模型
│   ├── VirtualizingWrapPanel/      # 虚拟化 WrapPanel
│   └── resources/                  # 资源文件
│       ├── MusicPlayer.ico         # 应用图标
│       └── MusicPlayer.png         # 应用图标
├── MusicPlayer_Install/            # 安装包项目
│   ├── MusicPlayer_Install.wapproj # 安装包配置
│   ├── Package.appxmanifest        # 应用清单
│   └── Images/                     # 安装包图片资源
├── Image/                          # 项目图片资源
├── .gitignore                      # Git 忽略文件
├── LICENSE                         # 许可证文件
└── README.md                       # 项目说明文档
```

## 🚀 快速开始

### 环境要求
- **操作系统**: Windows 10/11 (版本 19041.0 或更高)
- **开发环境**: .NET 8.0 SDK
- **IDE**: Visual Studio 2022 或 VS Code

### 构建与运行
1. **克隆项目**: `git clone https://github.com/yourusername/MusicPlayer.git`
2. **打开项目**: 使用 Visual Studio 2022 打开 `MusicPlayer.sln` 文件
3. **还原依赖**: 在 Visual Studio 中右键点击解决方案，选择 "还原 NuGet 包"
4. **构建项目**: 点击 "生成" > "生成解决方案"
5. **运行项目**: 点击 "调试" > "开始执行(不调试)" 或按 F5


## 🎯 使用指南

### 基本操作
1. **导入音乐**: 支持两种导入方式
   - 拖动文件夹到指定位置批量导入
   - 点击"导入音乐"按钮，通过文件选择器导入 
2. **播放音乐**: 双击歌曲列表中的歌曲开始播放
3. **播放控制**: 使用底部播放控制面板进行播放/暂停/切换操作
4. **搜索歌曲**: 在搜索框中输入关键词快速查找歌曲
5. **切换设置**: 点击设置按钮进入设置页面配置播放器

### 高级功能
- **音频可视化**: 播放时歌曲封面会环绕显示实时音频频谱效果，可在设置中启用/禁用
- **智能元数据**: 自动读取音乐文件的标题、艺术家、专辑信息
- **系统媒体控制**: 支持使用键盘媒体键或系统通知区域的媒体控制按钮

### 数据存储
- 播放列表信息本地存储在 LiteDB 数据库文件中
- 播放器配置、均衡器预设等设置存储在 LiteDB 数据库中
- 歌曲封面缓存保存在程序目录中的 cache 文件夹中
- 程序采用绿色便携设计，所有数据都保存在程序目录
- 数据库文件自动创建和管理，无需手动配置


## 🔧 开发说明

### 核心组件

#### 分层架构设计
- **核心层 (MusicPlayer.Core)**: 定义数据模型、服务接口、音频处理组件和数据访问层，提供独立于UI的业务逻辑
- **服务层 (MusicPlayer.Services)**: 实现业务逻辑服务，包括播放器服务、配置服务、消息处理、错误处理等
- **表现层 (MusicPlayer)**: 实现WPF界面和视图模型，处理用户交互和界面渲染
- **依赖注入**: 使用Microsoft.Extensions.DependencyInjection管理服务生命周期和依赖关系

#### 主要服务组件
- **PlayerService**: 音频播放核心服务，处理音频文件的解码和播放控制
- **PlayerStateService**: 播放状态管理，处理播放状态切换和进度跟踪
- **PlaylistService**: 播放列表核心服务，提供数据持久化和元数据读取功能
- **PlaylistDataService**: 播放列表数据服务，处理歌曲的增删改查操作
- **PlaylistCacheService**: 播放列表缓存服务，提高大量歌曲场景下的响应速度
- **PlaylistBusinessService**: 播放列表业务服务，处理播放列表的业务逻辑
- **PlaylistNavigationService**: 播放列表导航服务，处理播放列表之间的导航
- **PlaylistStateService**: 播放列表状态服务，管理播放列表的状态
- **ConfigurationService**: 配置管理服务，处理播放器配置的读写操作
- **EqualizerService**: 均衡器服务，处理音频均衡效果的调节和应用
- **NotificationService**: 系统通知服务，提供各种类型的用户通知
- **SystemMediaTransportService**: 系统媒体传输服务，集成Windows系统媒体控制
- **ErrorHandlingService**: 错误处理服务，统一处理应用程序中的异常情况
- **LifecycleManagementService**: 生命周期管理服务，管理应用程序的启动和关闭流程
- **MemoryMonitorService**: 内存监控服务，监控应用程序的内存使用情况
- **PlaybackContextService**: 播放上下文服务，管理播放上下文
- **LyricsService**: 歌词服务，处理歌词的加载和同步
- **CustomPlaylistService**: 自定义播放列表服务，处理自定义播放列表的管理
- **ServiceCoordinator**: 服务协调器，协调各个服务之间的交互

#### 消息通信机制
- **消息服务**: 基于IMessagingService实现组件间的松耦合通信
- **消息处理器**: 专门的消息处理器处理特定类型的消息，提高代码组织性
- **事件驱动**: 通过消息传递处理播放状态变更、歌曲切换等事件
- **命令模式**: 使用ICommand实现界面操作的响应式处理

#### 自定义控件系统
- **模块化设计**: 将界面拆分为TitleBar、ControlBar、Playlist、CenterContent等独立控件
- **MVVM模式**: 使用视图模型和接口实现控件的可测试性和可维护性
- **数据绑定**: 实现数据与界面的双向绑定和值转换
- **样式管理**: 通过WPF-UI和自定义样式统一管理界面外观和动画效果

#### 日志系统
- **Microsoft.Extensions.Logging集成**: 基于微软官方日志框架
- **文件日志**: 自定义文件日志提供者，支持日志文件输出
- **分级记录**: 支持不同级别的日志记录，便于调试和问题排查

### 依赖包说明
- **WPF-UI**: 提供 Fluent Design 风格的现代 WPF 控件
- **NAudio**: 强大的 .NET 音频处理库，支持多种音频格式
- **NAudio.Vorbis**: OGG/Vorbis 格式音频文件支持
- **TagLibSharp**: 音频文件元数据读取（标题、艺术家、封面等）
- **CommunityToolkit.Mvvm**: 轻量级MVVM框架，简化数据绑定和命令处理
- **Microsoft.Extensions.DependencyInjection**: 依赖注入容器，管理服务生命周期
- **Microsoft.Extensions.Hosting**: 主机应用程序基础设施，提供应用程序生命周期管理
- **Microsoft.Extensions.Logging**: 日志框架，提供灵活的日志记录功能
- **LiteDB**: 轻量级 NoSQL 数据库，用于存储播放列表和配置信息
- **Hardcodet.NotifyIcon.Wpf**: WPF系统托盘图标支持
- **CingZeoi.ChinesePinyinConverter**: 中文拼音转换库，用于歌手和歌曲的拼音排序
- **System.Runtime.Caching**: 内存缓存库，用于提高应用程序性能


## 🎉 致谢

- [WPF-UI](https://github.com/lepoco/wpfui) - 现代化 WPF UI 框架
- [NAudio](https://github.com/naudio/NAudio) - .NET 音频处理库
- [TagLib-Sharp](https://github.com/mono/taglib-sharp) - 多媒体元数据库
- [TextHighlighterTest](https://github.com/TwilightLemon/TextHighlighterTest)-混色文本控件
- [VirtualizingWrapPanel](https://github.com/sbaeumlisberger/VirtualizingWrapPanel)-网格布局控件  
 
 


## 📚 参考项目

 
- [一个功能强大、界面现代的 C# WPF 本地音乐播放器](https://github.com/ceng10086/MusicPlayer.git)
- [🎵 原音 HQ 播放器（Original Sound HQ Player）🎶](https://github.com/Johnwikix/original-sound-hq-player.git)
- [音频可视化（SpectrumVisualization）](https://github.com/Johnwikix/SpectrumVisualization.git) 
- [Lemon-App](https://github.com/TwilightLemon/Lemon-App.git)
 

## 📄 许可证

本项目采用 LICENSE 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。
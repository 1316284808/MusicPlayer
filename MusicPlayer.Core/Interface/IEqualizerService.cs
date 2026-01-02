using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 均衡器服务接口
    /// </summary>
    public interface IEqualizerService
    {
        /// <summary>
        /// 均衡器设置
        /// </summary>
        EqualizerSettings Settings { get; }
        
        /// <summary>
        /// 应用均衡器设置到音频流
        /// </summary>
        /// <param name="audioStream">原始音频流</param>
        /// <returns>处理后的音频流</returns>
        object? ApplyEqualizer(object? audioStream);
        
        /// <summary>
        /// 是否启用均衡器
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// 均衡器预设名称
        /// </summary>
        string PresetName { get; set; }
        
        /// <summary>
        /// 设置指定频段的增益
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <param name="gain">增益值 (dB)</param>
        void SetBandGain(int bandIndex, float gain);
        
        /// <summary>
        /// 获取指定频段的增益
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <returns>增益值 (dB)</returns>
        float GetBandGain(int bandIndex);
        
        /// <summary>
        /// 重置均衡器设置
        /// </summary>
        void Reset();
        
        /// <summary>
        /// 从配置加载均衡器设置
        /// </summary>
        /// <param name="config">播放器配置</param>
        void LoadFromConfiguration(PlayerConfiguration config);
        
        /// <summary>
        /// 保存均衡器设置到配置服务
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        void SaveToConfigurationService(IConfigurationService configurationService);
        
        /// <summary>
        /// 应用指定预设的增益值
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        void ApplyPreset(EqualizerPreset preset);
        
        /// <summary>
        /// 获取所有可用的均衡器预设
        /// </summary>
        /// <returns>均衡器预设列表</returns>
        List<EqualizerPreset> GetAvailablePresets();
    }
}
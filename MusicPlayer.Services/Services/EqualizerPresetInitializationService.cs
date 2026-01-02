using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using System.Diagnostics;

namespace MusicPlayer.Services.Services
{
    /// <summary>
    /// 均衡器预设初始化服务
    /// </summary>
    public class EqualizerPresetInitializationService
    {
        private readonly IEqualizerPresetRepository _presetRepository;
        private bool _isInitialized = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="presetRepository">预设仓库</param>
        public EqualizerPresetInitializationService(IEqualizerPresetRepository presetRepository)
        {
            _presetRepository = presetRepository;
        }

        /// <summary>
        /// 初始化默认预设数据
        /// </summary>
        public void InitializeDefaultPresets()
        {
            if (_isInitialized)
                return;

            try
            {
                // 确保数据库表已创建
                _presetRepository.Initialize();

                // 检查是否已有预设数据
                var existingPresets = _presetRepository.GetAll();
                if (existingPresets.Count > 0)
                {
                    Debug.WriteLine($"EqualizerPresetInitializationService: 已有 {existingPresets.Count} 个预设，跳过初始化");
                    _isInitialized = true;
                    return;
                }

                // 创建默认预设列表
                var defaultPresets = CreateDefaultPresets();
                
                // 批量添加预设
                int addedCount = _presetRepository.BatchAdd(defaultPresets);
                
                Debug.WriteLine($"EqualizerPresetInitializationService: 成功初始化 {addedCount}/{defaultPresets.Count} 个默认预设");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetInitializationService: 初始化默认预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建默认预设列表
        /// </summary>
        /// <returns>默认预设列表</returns>
        private List<EqualizerPreset> CreateDefaultPresets()
        {
            return new List<EqualizerPreset>
            {
                // 自定义
                new EqualizerPreset
                {
                    PresetName = "自定义",
                    BandGain0 = 0,
                    BandGain1 = 0,
                    BandGain2 = 0,
                    BandGain3 = 0,
                    BandGain4 = 0,
                    BandGain5 = 0,
                    BandGain6 = 0,
                    BandGain7 = 0,
                    BandGain8 = 0,
                    BandGain9 = 0
                },
                
                // 平衡
                new EqualizerPreset
                {
                    PresetName = "平衡",
                    BandGain0 = 0,
                    BandGain1 = 0,
                    BandGain2 = 0,
                    BandGain3 = 0,
                    BandGain4 = 0,
                    BandGain5 = 0,
                    BandGain6 = 0,
                    BandGain7 = 0,
                    BandGain8 = 0,
                    BandGain9 = 0
                },
                
                // 乡村
                new EqualizerPreset
                {
                    PresetName = "乡村",
                    BandGain0 = 4.5f,
                    BandGain1 = 3.5f,
                    BandGain2 = 0,
                    BandGain3 = -1,
                    BandGain4 = -1,
                    BandGain5 = 1,
                    BandGain6 = 3,
                    BandGain7 = 3.5f,
                    BandGain8 = 3.5f,
                    BandGain9 = 3.5f
                },
                
                // 流行
                new EqualizerPreset
                {
                    PresetName = "流行",
                    BandGain0 = -1,
                    BandGain1 = 2.5f,
                    BandGain2 = 4,
                    BandGain3 = 4.5f,
                    BandGain4 = 3,
                    BandGain5 = 0,
                    BandGain6 = -1,
                    BandGain7 = -1,
                    BandGain8 = 0.5f,
                    BandGain9 = 1.5f
                },
                
                // 爵士
                new EqualizerPreset
                {
                    PresetName = "爵士",
                    BandGain0 = 3.5f,
                    BandGain1 = 2.5f,
                    BandGain2 = 1,
                    BandGain3 = 2,
                    BandGain4 = 3,
                    BandGain5 = 3.5f,
                    BandGain6 = 3,
                    BandGain7 = 1.5f,
                    BandGain8 = 0,
                    BandGain9 = 0
                },
                
                // 摇滚
                new EqualizerPreset
                {
                    PresetName = "摇滚",
                    BandGain0 = 4.5f,
                    BandGain1 = 3.5f,
                    BandGain2 = 0,
                    BandGain3 = -2,
                    BandGain4 = -1,
                    BandGain5 = 2,
                    BandGain6 = 4,
                    BandGain7 = 5,
                    BandGain8 = 5,
                    BandGain9 = 4.5f
                },
                
                // 重金属
                new EqualizerPreset
                {
                    PresetName = "重金属",
                    BandGain0 = 5,
                    BandGain1 = 4.5f,
                    BandGain2 = 2,
                    BandGain3 = -2,
                    BandGain4 = -1,
                    BandGain5 = 3,
                    BandGain6 = 5,
                    BandGain7 = 6,
                    BandGain8 = 6,
                    BandGain9 = 5.5f
                },
                
                // 古典
                new EqualizerPreset
                {
                    PresetName = "古典",
                    BandGain0 = 4.5f,
                    BandGain1 = 3.5f,
                    BandGain2 = 2,
                    BandGain3 = 1,
                    BandGain4 = 0,
                    BandGain5 = 0,
                    BandGain6 = 1,
                    BandGain7 = 2,
                    BandGain8 = 3,
                    BandGain9 = 3.5f
                },
                
                // 电子
                new EqualizerPreset
                {
                    PresetName = "电子",
                    BandGain0 = 4.5f,
                    BandGain1 = 4,
                    BandGain2 = 2,
                    BandGain3 = 0,
                    BandGain4 = 0,
                    BandGain5 = 2,
                    BandGain6 = 4,
                    BandGain7 = 5,
                    BandGain8 = 5,
                    BandGain9 = 5
                },
                
                // 舞曲
                new EqualizerPreset
                {
                    PresetName = "舞曲",
                    BandGain0 = 4,
                    BandGain1 = 3.5f,
                    BandGain2 = 0,
                    BandGain3 = 0,
                    BandGain4 = 0,
                    BandGain5 = 0,
                    BandGain6 = 0,
                    BandGain7 = 4,
                    BandGain8 = 5,
                    BandGain9 = 5
                },
                
                // 嘻哈
                new EqualizerPreset
                {
                    PresetName = "嘻哈",
                    BandGain0 = 4.5f,
                    BandGain1 = 3.5f,
                    BandGain2 = 1,
                    BandGain3 = 0,
                    BandGain4 = 0,
                    BandGain5 = 1.5f,
                    BandGain6 = 3,
                    BandGain7 = 4,
                    BandGain8 = 4.5f,
                    BandGain9 = 4.5f
                }
            };
        }
    }
}
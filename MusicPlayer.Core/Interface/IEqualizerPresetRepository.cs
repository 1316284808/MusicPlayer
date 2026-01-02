using System.Collections.Generic;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 均衡器预设仓库接口
    /// </summary>
    public interface IEqualizerPresetRepository
    {
        /// <summary>
        /// 初始化数据库表
        /// </summary>
        void Initialize();

        /// <summary>
        /// 添加均衡器预设
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>添加成功后的ID</returns>
        int Add(EqualizerPreset preset);

        /// <summary>
        /// 更新均衡器预设
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>是否更新成功</returns>
        bool Update(EqualizerPreset preset);

        /// <summary>
        /// 删除均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>是否删除成功</returns>
        bool Delete(int id);

        /// <summary>
        /// 根据ID获取均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>均衡器预设</returns>
        EqualizerPreset? GetById(int id);

        /// <summary>
        /// 根据名称获取均衡器预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>均衡器预设</returns>
        EqualizerPreset? GetByName(string presetName);

        /// <summary>
        /// 获取所有均衡器预设
        /// </summary>
        /// <returns>均衡器预设列表</returns>
        List<EqualizerPreset> GetAll();

        /// <summary>
        /// 检查预设名称是否存在
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <param name="excludeId">排除的ID（用于更新时检查）</param>
        /// <returns>是否存在</returns>
        bool NameExists(string presetName, int? excludeId = null);

        /// <summary>
        /// 批量添加预设数据
        /// </summary>
        /// <param name="presets">预设列表</param>
        /// <returns>添加成功的记录数</returns>
        int BatchAdd(List<EqualizerPreset> presets);

        /// <summary>
        /// 保存均衡器预设（如果存在则更新，不存在则添加）
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>是否保存成功</returns>
        bool SavePreset(EqualizerPreset preset);
    }
}
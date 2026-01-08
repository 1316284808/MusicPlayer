using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;
using LiteDB;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 均衡器预设数据访问层
    /// </summary>
    public class EqualizerPresetsDAL : DBHelper
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databasePath">数据库路径</param>
        public EqualizerPresetsDAL(string databasePath) : base(databasePath)
        {
            if (!ExistsTable("EqualizerPresets"))
            {
                InsertDefaultPresets();
            }
        }
        
        /// <summary>
        /// 插入默认预设数据
        /// </summary>
        private void InsertDefaultPresets()
        {
            try
            {
                var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
                
                // 定义默认预设
                var defaultPresets = new List<EqualizerPreset>
                {
                    new EqualizerPreset { PresetName = "自定义", BandGain0 = 0, BandGain1 = 0, BandGain2 = 0, BandGain3 = 0, BandGain4 = 0, BandGain5 = 0, BandGain6 = 0, BandGain7 = 0, BandGain8 = 0, BandGain9 = 0 },
                    new EqualizerPreset { PresetName = "平衡", BandGain0 = 0, BandGain1 = 0, BandGain2 = 0, BandGain3 = 0, BandGain4 = 0, BandGain5 = 0, BandGain6 = 0, BandGain7 = 0, BandGain8 = 0, BandGain9 = 0 },
                    new EqualizerPreset { PresetName = "乡村", BandGain0 = 4.5f, BandGain1 = 3.5f, BandGain2 = 0, BandGain3 = -1, BandGain4 = -1, BandGain5 = 1, BandGain6 = 3, BandGain7 = 3.5f, BandGain8 = 3.5f, BandGain9 = 3.5f },
                    new EqualizerPreset { PresetName = "流行", BandGain0 = -1, BandGain1 = 2.5f, BandGain2 = 4, BandGain3 = 4.5f, BandGain4 = 3, BandGain5 = 0, BandGain6 = -1, BandGain7 = -1, BandGain8 = 0.5f, BandGain9 = 1.5f },
                    new EqualizerPreset { PresetName = "爵士", BandGain0 = 3.5f, BandGain1 = 2.5f, BandGain2 = 1, BandGain3 = 2, BandGain4 = 3, BandGain5 = 3.5f, BandGain6 = 3, BandGain7 = 1.5f, BandGain8 = 0, BandGain9 = 0 },
                    new EqualizerPreset { PresetName = "摇滚", BandGain0 = 4.5f, BandGain1 = 3.5f, BandGain2 = 0, BandGain3 = -2, BandGain4 = -1, BandGain5 = 2, BandGain6 = 4, BandGain7 = 5, BandGain8 = 5, BandGain9 = 4.5f },
                    new EqualizerPreset { PresetName = "重金属", BandGain0 = 5, BandGain1 = 4.5f, BandGain2 = 2, BandGain3 = -2, BandGain4 = -1, BandGain5 = 3, BandGain6 = 5, BandGain7 = 6, BandGain8 = 6, BandGain9 = 5.5f },
                    new EqualizerPreset { PresetName = "古典", BandGain0 = 4.5f, BandGain1 = 3.5f, BandGain2 = 2, BandGain3 = 1, BandGain4 = 0, BandGain5 = 0, BandGain6 = 1, BandGain7 = 2, BandGain8 = 3, BandGain9 = 3.5f },
                    new EqualizerPreset { PresetName = "电子", BandGain0 = 4.5f, BandGain1 = 4, BandGain2 = 2, BandGain3 = 0, BandGain4 = 0, BandGain5 = 2, BandGain6 = 4, BandGain7 = 5, BandGain8 = 5, BandGain9 = 5 },
                    new EqualizerPreset { PresetName = "舞曲", BandGain0 = 4, BandGain1 = 3.5f, BandGain2 = 0, BandGain3 = 0, BandGain4 = 0, BandGain5 = 0, BandGain6 = 0, BandGain7 = 4, BandGain8 = 5, BandGain9 = 5 },
                    new EqualizerPreset { PresetName = "嘻哈", BandGain0 = 4.5f, BandGain1 = 3.5f, BandGain2 = 1, BandGain3 = 0, BandGain4 = 0, BandGain5 = 1.5f, BandGain6 = 3, BandGain7 = 4, BandGain8 = 4.5f, BandGain9 = 4.5f }
                };
                
                // 插入默认预设
                collection.InsertBulk(defaultPresets);
                
                System.Diagnostics.Debug.WriteLine("EqualizerPresetsDAL: 成功插入默认预设数据");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerPresetsDAL: 插入默认预设数据失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 插入均衡器预设
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>插入的记录ID</returns>
        public int Insert(EqualizerPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            var result = collection.Insert(preset);
            return preset.Id;
        }

        /// <summary>
        /// 更新均衡器预设
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>是否更新成功</returns>
        public bool Update(EqualizerPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            return collection.Update(preset);
        }

        /// <summary>
        /// 删除均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>是否删除成功</returns>
        public bool Delete(int id)
        {
            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            return collection.Delete(id);
        }

        /// <summary>
        /// 根据ID获取均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>均衡器预设</returns>
        public EqualizerPreset? GetById(int id)
        {
            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            return collection.FindById(id);
        }

        /// <summary>
        /// 根据预设名称获取均衡器预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>均衡器预设</returns>
        public EqualizerPreset? GetByName(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return null;

            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            return collection.FindOne(x => x.PresetName == presetName);
        }

        /// <summary>
        /// 获取所有均衡器预设
        /// </summary>
        /// <returns>均衡器预设列表</returns>
        public List<EqualizerPreset> GetAll()
        {
            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            return collection.FindAll().OrderBy(x => x.Id).ToList();
        }

        /// <summary>
        /// 检查预设名称是否存在
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <param name="excludeId">排除的ID（用于更新时检查）</param>
        /// <returns>是否存在</returns>
        public bool NameExists(string presetName, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(presetName))
                return false;

            var collection = GetCollection<EqualizerPreset>("EqualizerPresets");
            
            if (excludeId.HasValue)
            {
                return collection.Exists(x => x.PresetName == presetName && x.Id != excludeId.Value);
            }
            else
            {
                return collection.Exists(x => x.PresetName == presetName);
            }
        }
    }
}

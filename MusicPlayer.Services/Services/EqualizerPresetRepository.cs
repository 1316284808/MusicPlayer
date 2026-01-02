using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Data;
using System.Diagnostics;

namespace MusicPlayer.Services.Services
{
    /// <summary>
    /// 均衡器预设仓库实现类
    /// </summary>
    public class EqualizerPresetRepository : IEqualizerPresetRepository
    {
        private readonly EqualizerPresetsDAL _dal;
        private bool _isInitialized = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databasePath">数据库路径</param>
        public EqualizerPresetRepository()
        {
            _dal = new EqualizerPresetsDAL(Paths.AppSettingPath);
        }

        /// <summary>
        /// 初始化数据库表
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            //try
            //{
            //    _dal.CreateTable();
            //    _isInitialized = true;
            //    Debug.WriteLine("EqualizerPresetRepository: 数据库表初始化成功");
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine($"EqualizerPresetRepository: 初始化数据库表失败: {ex.Message}");
            //    throw;
            //}
        }

        /// <summary>
        /// 添加均衡器预设
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>添加成功后的ID</returns>
        public int Add(EqualizerPreset preset)
        {
            EnsureInitialized();
            
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            try
            {
                // 检查名称是否已存在
                if (NameExists(preset.PresetName))
                {
                    Debug.WriteLine($"EqualizerPresetRepository: 预设名称 '{preset.PresetName}' 已存在");
                    return -1;
                }



                int id = _dal.Insert(preset);
                Debug.WriteLine($"EqualizerPresetRepository: 成功添加预设 '{preset.PresetName}'，ID: {id}");
                return id;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 添加预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新均衡器预设
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>是否更新成功</returns>
        public bool Update(EqualizerPreset preset)
        {
            EnsureInitialized();
            
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            try
            {
                // 检查名称是否已存在（排除当前ID）
                if (NameExists(preset.PresetName, preset.Id))
                {
                    Debug.WriteLine($"EqualizerPresetRepository: 预设名称 '{preset.PresetName}' 已存在（ID: {preset.Id}）");
                    return false;
                }



                bool success = _dal.Update(preset);
                Debug.WriteLine($"EqualizerPresetRepository: 更新预设 '{preset.PresetName}' {(success ? "成功" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 更新预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>是否删除成功</returns>
        public bool Delete(int id)
        {
            EnsureInitialized();
            
            try
            {
                bool success = _dal.Delete(id);
                Debug.WriteLine($"EqualizerPresetRepository: 删除预设 ID:{id} {(success ? "成功" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 删除预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>均衡器预设</returns>
        public EqualizerPreset? GetById(int id)
        {
            EnsureInitialized();
            
            try
            {
                var preset = _dal.GetById(id);
                Debug.WriteLine($"EqualizerPresetRepository: {(preset != null ? "成功" : "失败")}获取预设 ID:{id}");
                return preset;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 获取预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据名称获取均衡器预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>均衡器预设</returns>
        public EqualizerPreset? GetByName(string presetName)
        {
            EnsureInitialized();
            
            try
            {
                var preset = _dal.GetByName(presetName);
                Debug.WriteLine($"EqualizerPresetRepository: {(preset != null ? "成功" : "失败")}获取预设 '{presetName}'");
                return preset;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 获取预设失败: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// 获取所有均衡器预设
        /// </summary>
        /// <returns>均衡器预设列表</returns>
        public List<EqualizerPreset> GetAll()
        {
            EnsureInitialized();
            
            try
            {
                var presets = _dal.GetAll();
                Debug.WriteLine($"EqualizerPresetRepository: 成功获取 {presets.Count} 个预设");
                return presets;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 获取预设列表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 检查预设名称是否存在
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <param name="excludeId">排除的ID（用于更新时检查）</param>
        /// <returns>是否存在</returns>
        public bool NameExists(string presetName, int? excludeId = null)
        {
            EnsureInitialized();
            
            try
            {
                return _dal.NameExists(presetName, excludeId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 检查预设名称存在性失败: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// 批量添加预设数据
        /// </summary>
        /// <param name="presets">预设列表</param>
        /// <returns>添加成功的记录数</returns>
        public int BatchAdd(List<EqualizerPreset> presets)
        {
            EnsureInitialized();
            
            if (presets == null || presets.Count == 0)
                return 0;

            try
            {
                int successCount = 0;
                
                // 开始事务
                using var transaction = _dal.BeginTransaction();
                
                try
                {
                    foreach (var preset in presets)
                    {
                        try
                        {
                            // 检查名称是否已存在
                            if (!NameExists(preset.PresetName))
                            {
                                int id = _dal.Insert(preset);
                                if (id > 0)
                                {
                                    successCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"EqualizerPresetRepository: 添加预设 '{preset.PresetName}' 失败: {ex.Message}");
                        }
                    }
                    
                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"EqualizerPresetRepository: 批量添加预设完成，成功 {successCount}/{presets.Count}");
                }
                catch (Exception)
                {
                    // 回滚事务
                    transaction.Rollback();
                    throw;
                }
                
                return successCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 批量添加预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 保存均衡器预设（如果存在则更新，不存在则添加）
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        /// <returns>是否保存成功</returns>
        public bool SavePreset(EqualizerPreset preset)
        {
            EnsureInitialized();
            
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            try
            {
                // 检查是否已存在（通过名称）
                var existingByName = GetByName(preset.PresetName);
                
                if (existingByName != null)
                {
                    // 如果存在同名预设，则更新
                    preset.Id = existingByName.Id;
                    return Update(preset);
                }
                else
                {
                    // 如果不存在，则添加新预设
                    int id = Add(preset);
                    return id > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EqualizerPresetRepository: 保存预设失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确保已初始化
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _dal?.Dispose();
        }
    }
}
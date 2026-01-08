using System;
using System.IO;
using LiteDB;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// LiteDB配置数据访问层
    /// 负责配置数据的持久化存储
    /// </summary>
    public class ConfigurationDAL : DBHelper
    {
        private const string ConfigurationId = "1";
        
        public ConfigurationDAL(string databasePath) : base(databasePath)
        {

        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public PlayerConfiguration LoadConfiguration()
        {
            try
            {
                var collection = GetCollection<PlayerConfiguration>("Configuration");
                var config = collection.FindOne(x => x.LastPlayedSongId != 0);
                
                if (config != null)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationDAL: 从LiteDB加载配置 - 音量: {config.Volume}, 播放模式: {config.PlayMode}, 排序规则: {config.SortRule}, 频谱启用: {config.IsSpectrumEnabled}, 关闭行为: {config.CloseBehavior}, 过滤文本: {config.FilterText}, 主题: {config.Theme}, 均衡器启用: {config.IsEqualizerEnabled}, 均衡器预设: {config.EqualizerPresetName}, 最后播放歌曲ID: {config.LastPlayedSongId}");
                    return config;
                }

                // 如果没有配置数据，返回默认配置
                System.Diagnostics.Debug.WriteLine("ConfigurationDAL: 没有找到配置数据，使用默认配置");
                return new PlayerConfiguration();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从LiteDB加载配置失败: {ex.Message}");
                return new PlayerConfiguration();
            }
        }

        /// <summary>
        /// 保存配置  
        /// </summary>
        public void SaveConfiguration(PlayerConfiguration configuration)
        {
            try
            {
                // 确保LastSaved字段是最新的
                configuration.LastSaved = DateTime.Now;

                var collection = GetCollection<PlayerConfiguration>("Configuration");
                
                // 先删除旧配置
                collection.DeleteAll();
                
                // 插入新配置
                collection.Insert(configuration);

                System.Diagnostics.Debug.WriteLine($"ConfigurationDAL: 配置已保存到LiteDB数据库 - 音量: {configuration.Volume}, 播放模式: {configuration.PlayMode}, 排序规则: {configuration.SortRule}, 频谱启用: {configuration.IsSpectrumEnabled}, 关闭行为: {configuration.CloseBehavior}, 过滤文本: {configuration.FilterText}, 主题: {configuration.Theme}, 均衡器启用: {configuration.IsEqualizerEnabled}, 均衡器预设: {configuration.EqualizerPresetName}, 最后播放歌曲ID: {configuration.LastPlayedSongId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置到LiteDB失败: {ex.Message}");
            }
        }
    }
}
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// SQLite配置数据访问层
    /// 负责配置数据的持久化存储
    /// </summary>
    public class ConfigurationDAL : DBHelper
    {
        public ConfigurationDAL(string databasePath) : base(databasePath)
        {

        }

        /// <summary>
        /// 初始化配置表
        /// </summary>
        private void CreateConfiguration()
        {
            // 创建配置表，每个配置项作为一列
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS Configuration (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    volume REAL NOT NULL DEFAULT 0.5,
                    playMode INTEGER NOT NULL DEFAULT 1,
                    sortRule INTEGER NOT NULL DEFAULT 0,
                    lastSaved TEXT NOT NULL,
                    isSpectrumEnabled INTEGER NOT NULL DEFAULT 0,
                    closeBehavior INTEGER NOT NULL DEFAULT 0,
                    filterText TEXT NOT NULL DEFAULT '',
                    theme INTEGER NOT NULL DEFAULT 2,
                    audioengine INTEGER NOT NULL DEFAULT 0,
                    isEqualizerEnabled INTEGER NOT NULL DEFAULT 0,
                    equalizerPresetName TEXT NOT NULL DEFAULT '平衡',
                    equalizerGains TEXT NOT NULL DEFAULT '0,0,0,0,0,0,0,0,0,0',
                    lastPlayedSongId INTEGER NOT NULL DEFAULT -1
                )";

            ExecuteNonQuery(createTableSql);
        }



        /// <summary>
        /// 加载配置
        /// </summary>
        public PlayerConfiguration LoadConfiguration()
        {
            try
            {
                if (!ExistsTable("Configuration")) { CreateConfiguration(); }
                using var reader = ExecuteReader("SELECT * FROM Configuration WHERE Id = 1");

                if (reader.Read())
                {
                    var config = new PlayerConfiguration
                    {
                        Volume = reader.IsDBNull(1) ? 0.5f : Convert.ToSingle(reader["volume"]),
                        PlayMode = (PlayMode)(reader.IsDBNull(2) ? 1 : Convert.ToInt32(reader["playMode"])),
                        SortRule = (SortRule)(reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader["sortRule"])),
                        LastSaved = reader.IsDBNull(4) ? DateTime.Now : DateTime.Parse(reader["lastSaved"].ToString()),
                        IsSpectrumEnabled = reader.IsDBNull(5) ? false : Convert.ToInt32(reader["isSpectrumEnabled"]) == 1,
                        CloseBehavior = reader.IsDBNull(6) ? false : Convert.ToInt32(reader["closeBehavior"]) == 1,
                        FilterText = reader.IsDBNull(7) ? "" : reader["filterText"].ToString(),
                        Theme = (Theme)(reader.IsDBNull(8) ? 0 : Convert.ToInt32(reader["theme"])),
                        AudioEngine = (AudioEngine)(reader.IsDBNull(9) ? 0 : Convert.ToInt32(reader["AudioEngine"])),
                        IsEqualizerEnabled = reader.IsDBNull(10) ? false : Convert.ToInt32(reader["isEqualizerEnabled"]) == 1,
                        EqualizerPresetName = reader.IsDBNull(11) ? "平衡" : reader["equalizerPresetName"].ToString(),
                        LastPlayedSongId = reader.IsDBNull(13) ? -1 : Convert.ToInt32(reader["lastPlayedSongId"])
                    };

                    // 处理均衡器增益值数组（从逗号分隔的字符串加载）
                    if (!reader.IsDBNull(12))
                    {
                        var gainsString = reader["equalizerGains"].ToString();
                        if (!string.IsNullOrEmpty(gainsString))
                        {
                            var gainsArray = gainsString.Split(',');
                            if (gainsArray.Length == 10)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    if (float.TryParse(gainsArray[i], out float gain))
                                    {
                                        config.EqualizerGains[i] = gain;
                                    }
                                }
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"ConfigurationDAL: 从SQLite加载配置 - 音量: {config.Volume}, 播放模式: {config.PlayMode}, 排序规则: {config.SortRule}, 频谱启用: {config.IsSpectrumEnabled}, 关闭行为: {config.CloseBehavior}, 过滤文本: {config.FilterText}, 主题: {config.Theme}, 均衡器启用: {config.IsEqualizerEnabled}, 均衡器预设: {config.EqualizerPresetName}, 最后播放歌曲ID: {config.LastPlayedSongId}");

                    reader.Close();
                    return config;
                }

                reader.Close();

                // 如果没有配置数据，返回默认配置
                System.Diagnostics.Debug.WriteLine("ConfigurationDAL: 没有找到配置数据，使用默认配置");
                return new PlayerConfiguration();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从SQLite加载配置失败: {ex.Message}");
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

                // 将均衡器增益值数组转换为逗号分隔的字符串
                var gainsString = string.Join(",", configuration.EqualizerGains);

                // 使用INSERT OR REPLACE，如果记录存在则替换，不存在则插入
                ExecuteNonQuery(@"
                    INSERT OR REPLACE INTO Configuration 
                    (Id, volume, playMode, sortRule, lastSaved, isSpectrumEnabled, closeBehavior, filterText, theme, audioengine, isEqualizerEnabled, equalizerPresetName, equalizerGains, lastPlayedSongId)
                    VALUES (1, @volume, @playMode, @sortRule, @lastSaved, @isSpectrumEnabled, @closeBehavior, @filterText, @theme, @audioengine, @isEqualizerEnabled, @equalizerPresetName, @equalizerGains, @lastPlayedSongId)",
                    new SqliteParameter("@volume", configuration.Volume),
                    new SqliteParameter("@playMode", (int)configuration.PlayMode),
                    new SqliteParameter("@sortRule", (int)configuration.SortRule),
                    new SqliteParameter("@lastSaved", configuration.LastSaved.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")),
                    new SqliteParameter("@isSpectrumEnabled", configuration.IsSpectrumEnabled ? 1 : 0),
                    new SqliteParameter("@closeBehavior", configuration.CloseBehavior ? 1 : 0),
                    new SqliteParameter("@filterText", configuration.FilterText ?? string.Empty),
                    new SqliteParameter("@theme", (int)configuration.Theme),
                    new SqliteParameter("@audioengine", (int)configuration.AudioEngine),
                    new SqliteParameter("@isEqualizerEnabled", configuration.IsEqualizerEnabled ? 1 : 0),
                    new SqliteParameter("@equalizerPresetName", configuration.EqualizerPresetName ?? "平衡"),
                    new SqliteParameter("@equalizerGains", gainsString),
                    new SqliteParameter("@lastPlayedSongId", configuration.LastPlayedSongId));

                System.Diagnostics.Debug.WriteLine($"ConfigurationDAL: 配置已保存到SQLite数据库 - 音量: {configuration.Volume}, 播放模式: {configuration.PlayMode}, 排序规则: {configuration.SortRule}, 频谱启用: {configuration.IsSpectrumEnabled}, 关闭行为: {configuration.CloseBehavior}, 过滤文本: {configuration.FilterText}, 主题: {configuration.Theme}, 均衡器启用: {configuration.IsEqualizerEnabled}, 均衡器预设: {configuration.EqualizerPresetName}, 均衡器增益: {gainsString}, 最后播放歌曲ID: {configuration.LastPlayedSongId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置到SQLite失败: {ex.Message}");
            }
        }
    }
}
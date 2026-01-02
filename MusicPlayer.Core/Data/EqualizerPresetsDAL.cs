using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;
using Microsoft.Data.Sqlite;

namespace MusicPlayer.Core.Data
{
    /// <summary>
    /// 均衡器预设数据访问层
    /// </summary>
    public class EqualizerPresetsDAL : DBHelper
    {
        /// <summary>
        /// 创建表SQL语句
        /// </summary>
     

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databasePath">数据库路径</param>
        public EqualizerPresetsDAL(string databasePath) : base(databasePath)
        {
            if (!ExistsTable("EqualizerPresets"))
            {
                CreateEqualizerPresets();
            }
        }

        /// <summary>
        /// 创建表
        /// </summary>
        private void CreateEqualizerPresets()
        {
            // 创建配置表，每个配置项作为一列
            var createTableSql     = @"CREATE TABLE IF NOT EXISTS EqualizerPresets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PresetName TEXT NOT NULL UNIQUE,
                    BandGain0 REAL NOT NULL DEFAULT 0,
                    BandGain1 REAL NOT NULL DEFAULT 0,
                    BandGain2 REAL NOT NULL DEFAULT 0,
                    BandGain3 REAL NOT NULL DEFAULT 0,
                    BandGain4 REAL NOT NULL DEFAULT 0,
                    BandGain5 REAL NOT NULL DEFAULT 0,
                    BandGain6 REAL NOT NULL DEFAULT 0,
                    BandGain7 REAL NOT NULL DEFAULT 0,
                    BandGain8 REAL NOT NULL DEFAULT 0,
                    BandGain9 REAL NOT NULL DEFAULT 0
                )";

            ExecuteNonQuery(createTableSql);
            
            // 插入默认预设数据
            InsertDefaultPresets();
        }
        
        /// <summary>
        /// 插入默认预设数据
        /// </summary>
        private void InsertDefaultPresets()
        {
            try
            {
                // 开始事务
                using var transaction = BeginTransaction();
                
                try
                {
                    // 自定义
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('自定义', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)", 
                        transaction);
                    
                    // 平衡
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('平衡', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)", 
                        transaction);
                    
                    // 乡村
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('乡村', 4.5, 3.5, 0, -1, -1, 1, 3, 3.5, 3.5, 3.5)", 
                        transaction);
                    
                    // 流行
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('流行', -1, 2.5, 4, 4.5, 3, 0, -1, -1, 0.5, 1.5)", 
                        transaction);
                    
                    // 爵士
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('爵士', 3.5, 2.5, 1, 2, 3, 3.5, 3, 1.5, 0, 0)", 
                        transaction);
                    
                    // 摇滚
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('摇滚', 4.5, 3.5, 0, -2, -1, 2, 4, 5, 5, 4.5)", 
                        transaction);
                    
                    // 重金属
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('重金属', 5, 4.5, 2, -2, -1, 3, 5, 6, 6, 5.5)", 
                        transaction);
                    
                    // 古典
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('古典', 4.5, 3.5, 2, 1, 0, 0, 1, 2, 3, 3.5)", 
                        transaction);
                    
                    // 电子
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('电子', 4.5, 4, 2, 0, 0, 2, 4, 5, 5, 5)", 
                        transaction);
                    
                    // 舞曲
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('舞曲', 4, 3.5, 0, 0, 0, 0, 0, 4, 5, 5)", 
                        transaction);
                    
                    // 嘻哈
                    ExecuteNonQuery(@"
                        INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, BandGain5, BandGain6, BandGain7, BandGain8, BandGain9)
                        VALUES ('嘻哈', 4.5, 3.5, 1, 0, 0, 1.5, 3, 4, 4.5, 4.5)", 
                        transaction);
                    
                    // 提交事务
                    transaction.Commit();
                    System.Diagnostics.Debug.WriteLine("EqualizerPresetsDAL: 成功插入默认预设数据");
                }
                catch (Exception)
                {
                    // 回滚事务
                    transaction.Rollback();
                    throw;
                }
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

            const string sql = @"INSERT INTO EqualizerPresets 
                        (PresetName, BandGain0, BandGain1, BandGain2, BandGain3, BandGain4, 
                         BandGain5, BandGain6, BandGain7, BandGain8, BandGain9) 
                        VALUES 
                        (@PresetName, @BandGain0, @BandGain1, @BandGain2, @BandGain3, @BandGain4, 
                         @BandGain5, @BandGain6, @BandGain7, @BandGain8, @BandGain9);
                        
                        SELECT last_insert_rowid();";

            var parameters = new SqliteParameter[]
            {
                new("@PresetName", preset.PresetName),
                new("@BandGain0", preset.BandGain0),
                new("@BandGain1", preset.BandGain1),
                new("@BandGain2", preset.BandGain2),
                new("@BandGain3", preset.BandGain3),
                new("@BandGain4", preset.BandGain4),
                new("@BandGain5", preset.BandGain5),
                new("@BandGain6", preset.BandGain6),
                new("@BandGain7", preset.BandGain7),
                new("@BandGain8", preset.BandGain8),
                new("@BandGain9", preset.BandGain9)
            };

            var result = ExecuteScalar<long>(sql, parameters);
            return Convert.ToInt32(result);
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

            const string sql = @"UPDATE EqualizerPresets SET 
                        PresetName = @PresetName,
                        BandGain0 = @BandGain0,
                        BandGain1 = @BandGain1,
                        BandGain2 = @BandGain2,
                        BandGain3 = @BandGain3,
                        BandGain4 = @BandGain4,
                        BandGain5 = @BandGain5,
                        BandGain6 = @BandGain6,
                        BandGain7 = @BandGain7,
                        BandGain8 = @BandGain8,
                        BandGain9 = @BandGain9
                        WHERE Id = @Id";

            var parameters = new SqliteParameter[]
            {
                new("@Id", preset.Id),
                new("@PresetName", preset.PresetName),
                new("@BandGain0", preset.BandGain0),
                new("@BandGain1", preset.BandGain1),
                new("@BandGain2", preset.BandGain2),
                new("@BandGain3", preset.BandGain3),
                new("@BandGain4", preset.BandGain4),
                new("@BandGain5", preset.BandGain5),
                new("@BandGain6", preset.BandGain6),
                new("@BandGain7", preset.BandGain7),
                new("@BandGain8", preset.BandGain8),
                new("@BandGain9", preset.BandGain9)
            };

            int rowsAffected = ExecuteNonQuery(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// 删除均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>是否删除成功</returns>
        public bool Delete(int id)
        {
            const string sql = "DELETE FROM EqualizerPresets WHERE Id = @Id";

            var parameter = new SqliteParameter("@Id", id);
            int rowsAffected = ExecuteNonQuery(sql, parameter);
            return rowsAffected > 0;
        }

        /// <summary>
        /// 根据ID获取均衡器预设
        /// </summary>
        /// <param name="id">预设ID</param>
        /// <returns>均衡器预设</returns>
        public EqualizerPreset? GetById(int id)
        {
            const string sql = "SELECT * FROM EqualizerPresets WHERE Id = @Id";

            var parameter = new SqliteParameter("@Id", id);
            using var reader = ExecuteReader(sql, parameter);
            
            if (reader.Read())
            {
                return MapReaderToPreset(reader);
            }

            return null;
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

            const string sql = "SELECT * FROM EqualizerPresets WHERE PresetName = @PresetName";

            var parameter = new SqliteParameter("@PresetName", presetName);
            using var reader = ExecuteReader(sql, parameter);
            
            if (reader.Read())
            {
                return MapReaderToPreset(reader);
            }

            return null;
        }



        /// <summary>
        /// 获取所有均衡器预设
        /// </summary>
        /// <returns>均衡器预设列表</returns>
        public List<EqualizerPreset> GetAll()
        {
            const string sql = "SELECT * FROM EqualizerPresets ORDER BY Id";

            using var reader = ExecuteReader(sql);
            var presets = new List<EqualizerPreset>();
            
            while (reader.Read())
            {
                presets.Add(MapReaderToPreset(reader));
            }

            return presets;
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

            string sql = "SELECT COUNT(*) FROM EqualizerPresets WHERE PresetName = @PresetName";
            
            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
            }

            SqliteParameter[] parameters;
            if (excludeId.HasValue)
            {
                parameters = new SqliteParameter[]
                {
                    new("@PresetName", presetName),
                    new("@ExcludeId", excludeId.Value)
                };
            }
            else
            {
                parameters = new SqliteParameter[]
                {
                    new("@PresetName", presetName)
                };
            }

            var result = ExecuteScalar<long>(sql, parameters);
            return result > 0;
        }



        /// <summary>
        /// 将数据读取器映射到EqualizerPreset对象
        /// </summary>
        /// <param name="reader">数据读取器</param>
        /// <returns>均衡器预设对象</returns>
        private EqualizerPreset MapReaderToPreset(SqliteDataReader reader)
        {
            return new EqualizerPreset
            {
                Id = Convert.ToInt32(reader["Id"]),
                PresetName = Convert.ToString(reader["PresetName"]) ?? string.Empty,
                BandGain0 = Convert.ToSingle(reader["BandGain0"]),
                BandGain1 = Convert.ToSingle(reader["BandGain1"]),
                BandGain2 = Convert.ToSingle(reader["BandGain2"]),
                BandGain3 = Convert.ToSingle(reader["BandGain3"]),
                BandGain4 = Convert.ToSingle(reader["BandGain4"]),
                BandGain5 = Convert.ToSingle(reader["BandGain5"]),
                BandGain6 = Convert.ToSingle(reader["BandGain6"]),
                BandGain7 = Convert.ToSingle(reader["BandGain7"]),
                BandGain8 = Convert.ToSingle(reader["BandGain8"]),
                BandGain9 = Convert.ToSingle(reader["BandGain9"])
            };
        }
    }
}

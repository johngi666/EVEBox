using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EVEBox.Core.Config
{
    /// <summary>
    /// 统一配置管理（所有配置汇总到一个文件，带防抖写入）
    /// </summary>
    public class ConfigManager
    {
        private const string ConfigFile = "evesync_config.json";
        private AppConfig _config;
        private System.Timers.Timer _debounceTimer;
        private bool _pendingSave = false;
        private readonly object _saveLock = new object();

        public AppConfig Config => _config;

        public ConfigManager()
        {
            Load();
            _debounceTimer = new System.Timers.Timer(500);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += (s, e) => FlushSave();

            // 默认备份路径：桌面/EVE配置备份
            // （首次运行写入 json 的 BackupPath 字段，用户可直接改该行切换备份位置）
            if (string.IsNullOrEmpty(_config.BackupPath))
            {
                _config.BackupPath = GetBackupPath();
                Save();
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string json = File.ReadAllText(ConfigFile);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

                    // 如果 CharacterNames 有数据，同步到 CharacterCacheManager
                    if (_config.CharacterNames != null && _config.CharacterNames.Count > 0)
                    {
                        CharacterCacheManager.LoadFromDictionary(_config.CharacterNames);
                    }
                }
                else
                {
                    _config = new AppConfig();
                }
            }
            catch (Exception)
            {
                _config = new AppConfig();
            }
        }

        /// <summary>
        /// 保存配置（防抖：连续调用时只写一次磁盘）
        /// </summary>
        public void Save()
        {
            lock (_saveLock)
            {
                _pendingSave = true;
            }
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        /// <summary>
        /// 立即写入磁盘（跳过防抖）
        /// </summary>
        public void FlushSave()
        {
            lock (_saveLock)
            {
                if (!_pendingSave) return;
                _pendingSave = false;
            }

            try
            {
                var charNames = CharacterCacheManager.GetAllCachedNames();
                _config.CharacterNames = charNames ?? new Dictionary<string, string>();

                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 获取缓存的路径
        /// </summary>
        public string GetCachedPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(_config.CachedPath) && Directory.Exists(_config.CachedPath))
                    return _config.CachedPath;
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// 保存路径缓存
        /// </summary>
        public void SaveCachedPath(string folder)
        {
            _config.CachedPath = folder;
            Save();
        }

        /// <summary>
        /// 获取最后使用的服务器
        /// </summary>
        public string GetLastServer()
        {
            return _config.LastServer ?? "曙光服 (Infinity)";
        }

        /// <summary>
        /// 保存最后使用的服务器
        /// </summary>
        public void SaveLastServer(string server)
        {
            _config.LastServer = server;
            Save();
        }

        /// <summary>
        /// 获取同步设置
        /// </summary>
        public SyncSettings GetSyncSettings()
        {
            return _config.SyncSettings ?? new SyncSettings();
        }

        /// <summary>
        /// 保存同步设置
        /// </summary>
        public void SaveSyncSettings(SyncSettings settings)
        {
            _config.SyncSettings = settings;
            Save();
        }

        /// <summary>
        /// 获取所有用户备注
        /// </summary>
        public Dictionary<string, string> GetUserRemarks()
        {
            return _config.UserRemarks ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// 保存单个用户备注
        /// </summary>
        public void SaveUserRemark(string userId, string remark)
        {
            if (_config.UserRemarks == null)
                _config.UserRemarks = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(remark))
            {
                // 如果备注为空，删除该条目
                if (_config.UserRemarks.ContainsKey(userId))
                    _config.UserRemarks.Remove(userId);
            }
            else
            {
                _config.UserRemarks[userId] = remark;
            }
            Save();
        }

        /// <summary>
        /// 获取用户备注显示名（有备注显示备注，无备注显示数字ID）
        /// </summary>
        public string GetUserDisplayName(string userId)
        {
            var remarks = GetUserRemarks();
            if (remarks != null && remarks.TryGetValue(userId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                return remark;
            return userId;
        }

        /// <summary>
        /// 获取备份路径（默认桌面/EVE配置备份）
        /// 配置了 BackupPath 就用配置的（目录不存在时备份时会自动创建）
        /// </summary>
        public string GetBackupPath()
        {
            if (!string.IsNullOrEmpty(_config.BackupPath))
                return _config.BackupPath;

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return Path.Combine(desktop, "EVE配置备份");
        }

        /// <summary>
        /// 保存备份路径
        /// </summary>
        public void SaveBackupPath(string path)
        {
            _config.BackupPath = path;
            Save();
        }

        /// <summary>
        /// 获取用户备注显示名（带原ID后缀，用于同步对话框）
        /// </summary>
        public string GetUserDisplayNameWithId(string userId)
        {
            var remarks = GetUserRemarks();
            if (remarks != null && remarks.TryGetValue(userId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                return $"{remark} ({userId})";
            return userId;
        }
    }

    /// <summary>
    /// 应用配置数据模型
    /// </summary>
    public class AppConfig
    {
        public string LastServer { get; set; } = "曙光服 (Infinity)";
        public string CachedPath { get; set; }
        public string BackupPath { get; set; }
        public bool UseDarkMode { get; set; } = false;
        public Dictionary<string, string> CharacterNames { get; set; } = new Dictionary<string, string>();
        public List<ConfigScheme> ConfigSchemes { get; set; } = new List<ConfigScheme>();
        public SyncSettings SyncSettings { get; set; } = new SyncSettings();

        /// <summary>
        /// 用户备注字典 Key: 用户数字ID, Value: 备注文字
        /// </summary>
        public Dictionary<string, string> UserRemarks { get; set; } = new Dictionary<string, string>();
    }
}
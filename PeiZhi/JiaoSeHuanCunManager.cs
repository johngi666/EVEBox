using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;



namespace EVEBox.PeiZhi
{
    /// <summary>
    /// 角色名缓存管理器（数据存储在 evesync_config.json 中）
    /// </summary>
    public static class JiaoSeHuanCunManager
    {
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 从字典加载缓存（由 PeiZhiManager 调用）
        /// </summary>
        public static void LoadFromDictionary(Dictionary<string, string> dict)
        {
            lock (_lock)
            {
                _cache = dict ?? new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 获取缓存的角色名，如果不存在返回 null
        /// </summary>
        public static string GetCachedName(string characterId)
        {
            lock (_lock)
            {
                return _cache.TryGetValue(characterId, out string name) ? name : null;
            }
        }

        /// <summary>
        /// 保存角色名到缓存（由 PeiZhiManager.Save 统一写入文件）
        /// </summary>
        public static void SaveName(string characterId, string characterName)
        {
            lock (_lock)
            {
                _cache[characterId] = characterName;
            }
        }

        /// <summary>
        /// 批量保存角色名到缓存（由 PeiZhiManager.Save 统一写入文件）
        /// </summary>
        public static void SaveNames(Dictionary<string, string> names)
        {
            lock (_lock)
            {
                foreach (var kvp in names)
                {
                    _cache[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// 检查缓存中是否包含指定ID
        /// </summary>
        public static bool ContainsId(string characterId)
        {
            lock (_lock)
            {
                return _cache.ContainsKey(characterId);
            }
        }

        /// <summary>
        /// 获取所有缓存的角色名（由 PeiZhiManager.Save 调用）
        /// </summary>
        public static Dictionary<string, string> GetAllCachedNames()
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_cache);
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }
    }
}
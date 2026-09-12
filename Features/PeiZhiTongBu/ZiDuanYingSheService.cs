using EVEBox.Common.PeiZhi;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;



namespace EVEBox.Features.PeiZhiTongBu
{
    /// <summary>
    /// 字段映射服务 - 处理所有映射的查询、过滤和同步逻辑
    /// </summary>
    public class ZiDuanYingSheService
    {
        private readonly TongBuSheZhi _settings;
        private YongHuZiDuanYingShe _userMapping;
        private readonly HashSet<string> _publicChannelNames;

        public ZiDuanYingSheService(TongBuSheZhi settings)
        {
            _settings = settings ?? new TongBuSheZhi();
            _userMapping = new YongHuZiDuanYingShe();
            _publicChannelNames = new HashSet<string>();
        }

        /// <summary>
        /// 加载用户字段映射（替代旧的全局静态写入方式）
        /// </summary>
        public void LoadUserMapping(YongHuZiDuanYingShe mapping)
        {
            _userMapping = mapping ?? new YongHuZiDuanYingShe();
            RefreshPublicChannelNames();
        }

        /// <summary>
        /// 刷新公共频道名称缓存（从当前实例的映射中更新）
        /// </summary>
        public void RefreshPublicChannelNames()
        {
            _publicChannelNames.Clear();
            foreach (var name in _userMapping.ChatChannelMapping.Values)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    _publicChannelNames.Add(name);
                }
            }
        }

        #region 窗口标题处理

        /// <summary>
        /// 判断窗口标题是否应该被覆盖
        /// </summary>
        public bool ShouldOverrideWindowTitle(string key, string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            // 1. 私聊 → 永远跳过
            if (IsPrivateChat(title))
                return false;

            // 2. 提取基础名称（去掉 [N] 后缀）
            string baseName = ExtractBaseName(title);

            // 3. 本地频道 → 强制覆盖
            if (IsLocalChannel(baseName))
                return true;

            // 4. 如果聊天总开关关闭，只处理本地（已在上一步返回true）
            if (!_settings.OverrideChatConfig)
                return false;

            // 5. 群聊 → 跟随设置
            if (IsGroupChat(baseName))
                return _settings.OverrideGroupChatTitles;

            // 6. 公共频道 → 跟随设置
            if (IsPublicChannel(baseName))
                return _settings.OverridePublicChannelNames;

            // 7. 其他 → 跟随设置
            return _settings.OverrideOtherWindowTitles;
        }

        /// <summary>
        /// 判断是否为私聊
        /// </summary>
        public bool IsPrivateChat(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;
            return title.Contains("私聊(") || title.Contains("私聊（");
        }

        /// <summary>
        /// 判断是否为本地频道
        /// </summary>
        public bool IsLocalChannel(string title)
        {
            return title == "本地";
        }

        /// <summary>
        /// 判断是否为群聊
        /// </summary>
        public bool IsGroupChat(string title)
        {
            return title.StartsWith("群聊(");
        }

        /// <summary>
        /// 判断是否为公共频道
        /// </summary>
        public bool IsPublicChannel(string title)
        {
            return _publicChannelNames.Contains(title);
        }

        /// <summary>
        /// 提取基础名称，去掉 " [N]" 后缀
        /// </summary>
        public string ExtractBaseName(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            // 匹配 "xxx [数字]" 格式
            var match = Regex.Match(title, @"^(.*?)\s*\[\d+\]$");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return title;
        }

        /// <summary>
        /// 获取窗口标题的覆盖值（如果应该覆盖则返回映射值，否则返回原标题）
        /// </summary>
        public string GetWindowTitleOverride(string key, string originalTitle, Dictionary<string, string> mapping)
        {
            if (!ShouldOverrideWindowTitle(key, originalTitle))
                return originalTitle;

            if (mapping.TryGetValue(key, out string mappedValue))
                return mappedValue;

            return originalTitle;
        }

        #endregion

        #region 聊天频道处理

        /// <summary>
        /// 判断公共频道是否应该被覆盖
        /// </summary>
        public bool ShouldOverridePublicChannel(string key)
        {
            return _settings.OverridePublicChannelNames && _settings.OverrideChatConfig;
        }

        /// <summary>
        /// 获取公共频道名称的覆盖值
        /// </summary>
        public string GetPublicChannelNameOverride(string key, string originalName, Dictionary<string, string> mapping)
        {
            if (!ShouldOverridePublicChannel(key))
                return originalName;

            if (mapping.TryGetValue(key, out string mappedValue))
                return mappedValue;

            return originalName;
        }

        #endregion

        #region 其他配置处理

        /// <summary>
        /// 是否覆盖总览标签页
        /// </summary>
        public bool ShouldOverrideOverviewTabs() => _settings.OverrideOverviewTabs;

        /// <summary>
        /// 是否覆盖自定义快捷键
        /// </summary>
        public bool ShouldOverrideCustomCommands() => _settings.OverrideCustomCommands;

        /// <summary>
        /// 是否覆盖书签文件夹名
        /// </summary>
        public bool ShouldOverrideBookmarkFolders() => _settings.OverrideBookmarkFolders;

        /// <summary>
        /// 是否覆盖装配方案名
        /// </summary>
        public bool ShouldOverrideFittingNames() => _settings.OverrideFittingNames;

        #endregion

        #region 批量过滤

        /// <summary>
        /// 过滤窗口标题映射（移除私聊，根据设置决定覆盖哪些）
        /// </summary>
        public Dictionary<string, string> FilterWindowTitles(Dictionary<string, string> source, Dictionary<string, string> mapping)
        {
            var result = new Dictionary<string, string>();

            foreach (var kvp in source)
            {
                string key = kvp.Key;
                string title = kvp.Value;

                // 私聊直接跳过
                if (IsPrivateChat(title))
                    continue;

                // 提取基础名称
                string baseName = ExtractBaseName(title);

                // 本地频道 → 强制覆盖
                if (IsLocalChannel(baseName))
                {
                    result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : title;
                    continue;
                }

                // 如果聊天总开关关闭，保留原标题
                if (!_settings.OverrideChatConfig)
                {
                    result[key] = title;
                    continue;
                }

                // 群聊 → 跟随设置
                if (IsGroupChat(baseName))
                {
                    if (_settings.OverrideGroupChatTitles)
                    {
                        result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : title;
                    }
                    else
                    {
                        result[key] = title;
                    }
                    continue;
                }

                // 公共频道 → 跟随设置
                if (IsPublicChannel(baseName))
                {
                    if (_settings.OverridePublicChannelNames)
                    {
                        result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : title;
                    }
                    else
                    {
                        result[key] = title;
                    }
                    continue;
                }

                // 其他 → 跟随设置
                if (_settings.OverrideOtherWindowTitles)
                {
                    result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : title;
                }
                else
                {
                    result[key] = title;
                }
            }

            return result;
        }

        /// <summary>
        /// 过滤聊天频道映射（只保留公共频道，并根据设置决定是否覆盖）
        /// </summary>
        public Dictionary<string, string> FilterChatChannels(Dictionary<string, string> source, Dictionary<string, string> mapping)
        {
            var result = new Dictionary<string, string>();

            foreach (var kvp in source)
            {
                string key = kvp.Key;
                string channelName = kvp.Value;

                // 只处理公共频道
                if (!_publicChannelNames.Contains(channelName))
                    continue;

                if (_settings.OverridePublicChannelNames && _settings.OverrideChatConfig)
                {
                    result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : channelName;
                }
                else
                {
                    result[key] = channelName;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取过滤后的窗口标题映射（仅保留应该保留的条目）
        /// </summary>
        public Dictionary<string, string> GetFilteredWindowTitles(Dictionary<string, string> mapping)
        {
            var result = new Dictionary<string, string>();

            foreach (var kvp in mapping)
            {
                string key = kvp.Key;
                string title = kvp.Value;

                // 私聊直接跳过
                if (IsPrivateChat(title))
                    continue;

                // 提取基础名称
                string baseName = ExtractBaseName(title);

                // 本地频道 → 强制覆盖
                if (IsLocalChannel(baseName))
                {
                    result[key] = title;
                    continue;
                }

                // 如果聊天总开关关闭，跳过所有聊天相关（本地已在上面处理）
                if (!_settings.OverrideChatConfig)
                {
                    // 跳过群聊和公共频道
                    if (IsGroupChat(baseName) || IsPublicChannel(baseName))
                        continue;

                    // 其他窗口标题根据设置决定
                    if (_settings.OverrideOtherWindowTitles)
                        result[key] = title;
                    continue;
                }

                // 聊天总开关开启
                if (IsGroupChat(baseName) && _settings.OverrideGroupChatTitles)
                {
                    result[key] = title;
                }
                else if (IsPublicChannel(baseName) && _settings.OverridePublicChannelNames)
                {
                    result[key] = title;
                }
                else if (_settings.OverrideOtherWindowTitles)
                {
                    result[key] = title;
                }
            }

            return result;
        }

        #endregion

        #region 判断扩展

        /// <summary>
        /// 获取所有公共频道名称列表
        /// </summary>
        public HashSet<string> GetPublicChannelNames()
        {
            return new HashSet<string>(_publicChannelNames);
        }

        /// <summary>
        /// 获取当前设置对象
        /// </summary>
        public TongBuSheZhi GetSettings()
        {
            return _settings;
        }

        #endregion
    }
}
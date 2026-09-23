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
        private YongHuZiDuanYingShe _userMapping;
        private readonly HashSet<string> _publicChannelNames;

        public ZiDuanYingSheService()
        {
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
        /// 判断窗口标题是否应该被覆盖（除了私聊，其余一律覆盖）
        /// </summary>
        public bool ShouldOverrideWindowTitle(string key, string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            // 私聊 → 永远跳过
            if (IsPrivateChat(title))
                return false;

            // 本地频道、群聊、公共频道、其他窗口 → 覆盖
            return true;
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
            return true;
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
        public bool ShouldOverrideOverviewTabs() => true;

        /// <summary>
        /// 是否覆盖自定义快捷键
        /// </summary>
        public bool ShouldOverrideCustomCommands() => true;

        /// <summary>
        /// 是否覆盖书签文件夹名
        /// </summary>
        public bool ShouldOverrideBookmarkFolders() => true;

        /// <summary>
        /// 是否覆盖装配方案名
        /// </summary>
        public bool ShouldOverrideFittingNames() => true;

        #endregion

        #region 批量过滤

        /// <summary>
        /// 过滤窗口标题映射（移除私聊，其余按映射覆盖）
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

                // 本地频道、群聊、公共频道、其他窗口 → 一律按映射覆盖
                result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : title;
            }

            return result;
        }

        /// <summary>
        /// 过滤聊天频道映射（只保留公共频道）
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

                result[key] = mapping.TryGetValue(key, out string mappedValue) ? mappedValue : channelName;
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
                // 私聊直接跳过，其余保留
                if (IsPrivateChat(kvp.Value))
                    continue;

                result[kvp.Key] = kvp.Value;
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

        #endregion
    }
}

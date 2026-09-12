using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EVEBox.Core.Marshal
{
    /// <summary>
    /// Marshal 数据解析器
    /// 负责从 JSON 结构中提取各类映射数据（频道、窗口、总览、快捷键、书签、装配）
    /// </summary>
    public class MarshalParser
    {
        /// <summary>
        /// 从 JSON 文档中提取聊天频道映射
        /// </summary>
        public Dictionary<string, string> ExtractChatChannels(JsonDocument jsonDocument)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (jsonDocument.RootElement.TryGetProperty("ui", out JsonElement uiElement) &&
                    uiElement.TryGetProperty("bytes:chatchannels", out JsonElement channelsElement))
                {
                    foreach (JsonElement channel in channelsElement.EnumerateArray())
                    {
                        if (channel.ValueKind == JsonValueKind.Object)
                        {
                            string channelId = null;
                            string channelName = null;

                            if (channel.TryGetProperty("id", out JsonElement idElement))
                                channelId = idElement.GetString();
                            if (channel.TryGetProperty("name", out JsonElement nameElement))
                                channelName = nameElement.GetString();
                            if (string.IsNullOrEmpty(channelId) && channel.TryGetProperty("key", out JsonElement keyElement))
                                channelId = keyElement.GetString();

                            if (!string.IsNullOrEmpty(channelId) && !string.IsNullOrEmpty(channelName) &&
                                !channelName.Contains("私聊"))
                            {
                                result[channelId] = channelName;
                            }
                        }
                        else if (channel.ValueKind == JsonValueKind.Array)
                        {
                            var items = channel.EnumerateArray().ToList();
                            if (items.Count >= 2)
                            {
                                string channelId = items[0].GetString();
                                string channelName = items[1].GetString();
                                if (!string.IsNullOrEmpty(channelId) && !string.IsNullOrEmpty(channelName) &&
                                    !channelName.Contains("私聊"))
                                {
                                    result[channelId] = channelName;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 文档中提取窗口标题映射
        /// </summary>
        public Dictionary<string, string> ExtractWindowTitles(JsonDocument jsonDocument)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (jsonDocument.RootElement.TryGetProperty("ui", out JsonElement uiElement) &&
                    uiElement.TryGetProperty("bytes:tabgroups", out JsonElement tabgroupsElement))
                {
                    if (tabgroupsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty property in tabgroupsElement.EnumerateObject())
                        {
                            string key = property.Name;
                            string value = property.Value.GetString();
                            if (!string.IsNullOrEmpty(value) && !value.Contains("私聊(") && !value.Contains("私聊（"))
                            {
                                result[key] = value;
                            }
                        }
                    }
                    else if (tabgroupsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in tabgroupsElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                string key = null;
                                string value = null;
                                if (item.TryGetProperty("key", out JsonElement keyElement))
                                    key = keyElement.GetString();
                                if (item.TryGetProperty("value", out JsonElement valueElement))
                                    value = valueElement.GetString();
                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value) &&
                                    !value.Contains("私聊(") && !value.Contains("私聊（"))
                                {
                                    result[key] = value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 文档中提取总览标签页映射
        /// </summary>
        public Dictionary<string, string> ExtractOverviewTabs(JsonDocument jsonDocument)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (jsonDocument.RootElement.TryGetProperty("ui", out JsonElement uiElement) &&
                    uiElement.TryGetProperty("bytes:tabsettings_new", out JsonElement tabsElement))
                {
                    if (tabsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty property in tabsElement.EnumerateObject())
                        {
                            string key = property.Name;
                            string value = property.Value.GetString();
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                result[key] = value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 文档中提取自定义快捷键映射
        /// </summary>
        public Dictionary<string, string> ExtractCustomCommands(JsonDocument jsonDocument)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (jsonDocument.RootElement.TryGetProperty("ui", out JsonElement uiElement) &&
                    uiElement.TryGetProperty("bytes:customCmds", out JsonElement cmdsElement))
                {
                    if (cmdsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty property in cmdsElement.EnumerateObject())
                        {
                            string key = property.Name;
                            string value = property.Value.GetString();
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                result[key] = value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 文档中提取书签文件夹映射
        /// </summary>
        public Dictionary<string, string> ExtractBookmarkFolders(JsonDocument jsonDocument)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (jsonDocument.RootElement.TryGetProperty("ui", out JsonElement uiElement))
                {
                    foreach (JsonProperty property in uiElement.EnumerateObject())
                    {
                        if (property.Name.StartsWith("bytes:bookmarkSubfolderWindow_"))
                        {
                            string value = property.Value.GetString();
                            if (!string.IsNullOrEmpty(value))
                                result[property.Name] = value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 文档中提取装配方案映射
        /// </summary>
        public Dictionary<string, string> ExtractFittings(JsonDocument jsonDocument)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (jsonDocument.RootElement.TryGetProperty("ui", out JsonElement uiElement))
                {
                    foreach (JsonProperty property in uiElement.EnumerateObject())
                    {
                        if (property.Name.StartsWith("bytes:Save_ViewFitting_"))
                        {
                            string value = property.Value.GetString();
                            if (!string.IsNullOrEmpty(value))
                                result[property.Name] = value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        /// <summary>
        /// 批量提取所有映射数据
        /// </summary>
        public MarshalData ExtractAll(JsonDocument jsonDocument)
        {
            return new MarshalData
            {
                WindowTitles = ExtractWindowTitles(jsonDocument),
                ChatChannels = ExtractChatChannels(jsonDocument),
                OverviewTabs = ExtractOverviewTabs(jsonDocument),
                CustomCommands = ExtractCustomCommands(jsonDocument),
                BookmarkFolders = ExtractBookmarkFolders(jsonDocument),
                Fittings = ExtractFittings(jsonDocument)
            };
        }

        /// <summary>
        /// 从 JSON 字符串中提取所有映射数据
        /// </summary>
        public MarshalData ExtractAllFromString(string jsonString)
        {
            using var document = JsonDocument.Parse(jsonString);
            return ExtractAll(document);
        }
    }

    /// <summary>
    /// Marshal 数据容器
    /// </summary>
    public class MarshalData
    {
        public Dictionary<string, string> WindowTitles { get; set; } = new();
        public Dictionary<string, string> ChatChannels { get; set; } = new();
        public Dictionary<string, string> OverviewTabs { get; set; } = new();
        public Dictionary<string, string> CustomCommands { get; set; } = new();
        public Dictionary<string, string> BookmarkFolders { get; set; } = new();
        public Dictionary<string, string> Fittings { get; set; } = new();
    }
}
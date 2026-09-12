using System.Collections.Generic;
using System.Linq;



namespace EVEBox.Features.PeiZhiTongBu
{
    /// <summary>
    /// 用户文件字段映射（实例化，避免全局静态状态）
    /// </summary>
    public class YongHuZiDuanYingShe
    {
        public Dictionary<string, string> WindowTitleMapping { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ChatChannelMapping { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> OverviewTabMapping { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> CustomCommandMapping { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> BookmarkFolderMapping { get; private set; } = new Dictionary<string, string>();

        public void BuildWindowTitleMapping(Dictionary<string, string> windowData)
        {
            WindowTitleMapping = FilterPrivateChat(windowData);
        }

        public void BuildChatChannelMapping(Dictionary<string, string> channelData)
        {
            ChatChannelMapping = FilterPrivateChat(channelData);
        }

        public void BuildOverviewTabMapping(Dictionary<string, string> overviewData)
        {
            OverviewTabMapping = overviewData ?? new Dictionary<string, string>();
        }

        public void BuildCustomCommandMapping(Dictionary<string, string> commandData)
        {
            CustomCommandMapping = commandData ?? new Dictionary<string, string>();
        }

        public void BuildBookmarkFolderMapping(Dictionary<string, string> bookmarkData)
        {
            BookmarkFolderMapping = bookmarkData ?? new Dictionary<string, string>();
        }

        public void BuildAll(
            Dictionary<string, string> windowData,
            Dictionary<string, string> channelData,
            Dictionary<string, string> overviewData,
            Dictionary<string, string> commandData,
            Dictionary<string, string> bookmarkData)
        {
            BuildWindowTitleMapping(windowData);
            BuildChatChannelMapping(channelData);
            BuildOverviewTabMapping(overviewData);
            BuildCustomCommandMapping(commandData);
            BuildBookmarkFolderMapping(bookmarkData);
        }

        public void Clear()
        {
            WindowTitleMapping = new Dictionary<string, string>();
            ChatChannelMapping = new Dictionary<string, string>();
            OverviewTabMapping = new Dictionary<string, string>();
            CustomCommandMapping = new Dictionary<string, string>();
            BookmarkFolderMapping = new Dictionary<string, string>();
        }

        public string GetWindowTitle(string key)
        {
            return WindowTitleMapping.TryGetValue(key, out string value) ? value : key;
        }

        public string GetChatChannelName(string key)
        {
            return ChatChannelMapping.TryGetValue(key, out string value) ? value : key;
        }

        public string GetOverviewTabName(string key)
        {
            return OverviewTabMapping.TryGetValue(key, out string value) ? value : key;
        }

        public string GetCustomCommand(string key)
        {
            return CustomCommandMapping.TryGetValue(key, out string value) ? value : key;
        }

        public string GetBookmarkFolderName(string key)
        {
            return BookmarkFolderMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static bool IsPrivateChatTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;
            return title.Contains("私聊(") || title.Contains("私聊（");
        }

        public static bool IsLocalChannelTitle(string title)
        {
            return title == "本地";
        }

        public static bool IsGroupChatTitle(string title)
        {
            return title.StartsWith("群聊(");
        }

        public bool IsPublicChannelTitle(string title)
        {
            return ChatChannelMapping.ContainsValue(title);
        }

        private static Dictionary<string, string> FilterPrivateChat(Dictionary<string, string> data)
        {
            if (data == null)
                return new Dictionary<string, string>();

            return data
                .Where(kvp => !kvp.Value.Contains("私聊(") && !kvp.Value.Contains("私聊（"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}

using System.Collections.Generic;

namespace EVEBox.Core.Config
{
    /// <summary>
    /// 同步设置数据模型（由 ConfigManager 保存/加载）
    /// </summary>
    public class SyncSettings
    {
        // ========== 覆盖开关（兼容旧版） ==========
        public bool OverrideChatConfig { get; set; } = true;
        public bool OverridePublicChannelNames { get; set; } = true;
        public bool OverrideGroupChatTitles { get; set; } = true;
        public bool OverrideOtherWindowTitles { get; set; } = true;
        public bool OverrideOverviewTabs { get; set; } = true;
        public bool OverrideCustomCommands { get; set; } = true;
        public bool OverrideBookmarkFolders { get; set; } = true;
        public bool OverrideFittingNames { get; set; } = true;
        public bool OverrideLocalChannelTitles => true;

        // ========== 新增：保存用户勾选的设置项名称列表 ==========
        public List<string> SelectedPartialSettings { get; set; } = new List<string>();

        /// <summary>
        /// 重置为默认（全部勾选）
        /// </summary>
        public void ResetToDefault()
        {
            OverrideChatConfig = true;
            OverridePublicChannelNames = true;
            OverrideGroupChatTitles = true;
            OverrideOtherWindowTitles = true;
            OverrideOverviewTabs = true;
            OverrideCustomCommands = true;
            OverrideBookmarkFolders = true;
            OverrideFittingNames = true;

            var allSettings = Core.Mapping.SettingMapping.GetAll();
            SelectedPartialSettings = new List<string>();
            foreach (var item in allSettings)
            {
                SelectedPartialSettings.Add(item.DisplayName);
            }
        }
    }
}

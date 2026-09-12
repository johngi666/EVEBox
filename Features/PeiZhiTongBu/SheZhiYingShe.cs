using System.Collections.Generic;



namespace EVEBox.Features.PeiZhiTongBu
{
    public class SheZhiXiang
    {
        public string DisplayName { get; set; } = string.Empty;
        public string JsonPath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool DefaultSelected { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
    }

    public static class SheZhiYingShe
    {
        public static List<SheZhiXiang> GetAll()
        {
            return new List<SheZhiXiang>
            {
                new SheZhiXiang
                {
                    DisplayName = "聊天频道列表",
                    JsonPath = "ui.bytes:chatchannels",
                    Category = "聊天",
                    DefaultSelected = true,
                    Description = "当前加入的所有聊天频道",
                },
                new SheZhiXiang
                {
                    DisplayName = "本地频道位置",
                    JsonPath = "windows.bytes:windowSizesAndPositions_1.bytes:chatchannel_local",
                    Category = "聊天",
                    DefaultSelected = true,
                    Description = "本地频道窗口的位置和大小",
                },
                new SheZhiXiang
                {
                    DisplayName = "聊天窗口折叠状态",
                    JsonPath = "windows.bytes:collapsedWindows",
                    Category = "聊天",
                    DefaultSelected = false,
                    Description = "所有聊天窗口的折叠状态",
                },
                new SheZhiXiang
                {
                    DisplayName = "聊天窗口锁定状态",
                    JsonPath = "windows.bytes:lockedWindows",
                    Category = "聊天",
                    DefaultSelected = false,
                    Description = "所有聊天窗口的锁定状态",
                },
                new SheZhiXiang
                {
                    DisplayName = "所有窗口位置和大小",
                    JsonPath = "windows.bytes:windowSizesAndPositions_1",
                    Category = "窗口",
                    DefaultSelected = true,
                    Description = "所有窗口的位置和大小",
                },
                new SheZhiXiang
                {
                    DisplayName = "窗口透明度",
                    JsonPath = "windows.bytes:windowTransparency",
                    Category = "窗口",
                    DefaultSelected = false,
                    Description = "窗口透明度 (1.0=不透明)",
                },
                new SheZhiXiang
                {
                    DisplayName = "星图视图设置",
                    JsonPath = "ui.bytes:mapview2_",
                    Category = "UI",
                    DefaultSelected = false,
                    Description = "星图的视图颜色、显示层、布局等",
                },
                new SheZhiXiang
                {
                    DisplayName = "默认环绕距离",
                    JsonPath = "ui.bytes:defaultTypeOrbitDist",
                    Category = "UI",
                    DefaultSelected = false,
                    Description = "不同舰船类型的默认环绕距离",
                },
                new SheZhiXiang
                {
                    DisplayName = "星图面板停靠设置",
                    JsonPath = "dockPanels.bytes:primary_map_panel",
                    Category = "停靠面板",
                    DefaultSelected = false,
                    Description = "主星图面板的停靠设置",
                },
                new SheZhiXiang
                {
                    DisplayName = "技能计划面板停靠",
                    JsonPath = "dockPanels.bytes:SkillPlanner",
                    Category = "停靠面板",
                    DefaultSelected = false,
                    Description = "技能计划窗口的停靠设置",
                },
                new SheZhiXiang
                {
                    DisplayName = "通知淡出时间",
                    JsonPath = "notifications.bytes:notificationSettingsFadeTime",
                    Category = "通知",
                    DefaultSelected = false,
                    Description = "通知淡出时间（秒）",
                },
                new SheZhiXiang
                {
                    DisplayName = "自动装填设置",
                    JsonPath = "autoreload",
                    Category = "自动操作",
                    DefaultSelected = false,
                    Description = "物品自动装填设置（弹药、晶体等）",
                },
                new SheZhiXiang
                {
                    DisplayName = "自动重复使用设置",
                    JsonPath = "autorepeat",
                    Category = "自动操作",
                    DefaultSelected = false,
                    Description = "物品自动重复使用设置（发射器等）",
                },
            };
        }

        public static Dictionary<string, List<SheZhiXiang>> GetByCategory()
        {
            var result = new Dictionary<string, List<SheZhiXiang>>();
            foreach (var item in GetAll())
            {
                if (!result.ContainsKey(item.Category))
                    result[item.Category] = new List<SheZhiXiang>();
                result[item.Category].Add(item);
            }
            return result;
        }

        public static List<string> GetAllCategories()
        {
            var categories = new HashSet<string>();
            foreach (var item in GetAll())
            {
                categories.Add(item.Category);
            }
            return new List<string>(categories);
        }
    }
}
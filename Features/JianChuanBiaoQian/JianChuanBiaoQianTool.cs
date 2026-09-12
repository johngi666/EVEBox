using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace EVEBox.Features.JianChuanBiaoQian
{
    /// <summary>
    /// 工具类：舰船标签显示开关
    /// 操作 EVE 配置文件夹下的 prefs.ini 中的 bracketsAlwaysShowShipText 键。
    /// 后续新增的工具功能（工具标签页）也统一放在本命名空间下。
    /// </summary>
    public static class JianChuanBiaoQianTool
    {
        /// <summary>
        /// prefs.ini 中控制舰船名称标签显示的键名
        /// </summary>
        public const string BracketsAlwaysShowShipTextKey = "bracketsAlwaysShowShipText";

        /// <summary>
        /// 检查 prefs.ini 中是否已开启舰船标签显示（值 =1）
        /// </summary>
        public static bool IsEnabled(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return false;

            string iniPath = GetPrefsPath(folder);
            if (!System.IO.File.Exists(iniPath))
                return false;

            foreach (string line in System.IO.File.ReadAllLines(iniPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith(BracketsAlwaysShowShipTextKey, StringComparison.OrdinalIgnoreCase))
                {
                    int eq = trimmed.IndexOf('=');
                    return eq >= 0 && trimmed.Substring(eq + 1).Trim() == "1";
                }
            }
            return false;
        }

        /// <summary>
        /// 设置舰船标签显示开关（true=开启1 / false=关闭0）
        /// 已存在该键则更新值，不存在则在文件最顶部插入（不重复添加）
        /// </summary>
        public static void SetEnabled(string folder, bool enabled)
        {
            if (string.IsNullOrEmpty(folder))
                return;

            string iniPath = GetPrefsPath(folder);
            string value = enabled ? "1" : "0";
            string newLine = BracketsAlwaysShowShipTextKey + "=" + value;

            if (!System.IO.File.Exists(iniPath))
            {
                // 文件不存在：创建并写入
                System.IO.File.WriteAllText(iniPath, newLine + Environment.NewLine);
                return;
            }

            var lines = System.IO.File.ReadAllLines(iniPath).ToList();
            int index = lines.FindIndex(l =>
                l.TrimStart().StartsWith(BracketsAlwaysShowShipTextKey, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                lines[index] = newLine;
            }
            else
            {
                // 不存在：在文件最上面一排插入
                lines.Insert(0, newLine);
            }

            System.IO.File.WriteAllLines(iniPath, lines);
        }

        private static string GetPrefsPath(string folder)
        {
            return Path.Combine(folder, "prefs.ini");
        }
    }
}

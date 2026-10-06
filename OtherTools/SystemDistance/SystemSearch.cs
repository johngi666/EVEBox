using System;
using System.Collections.Generic;
using System.Linq;

namespace EVEBox.OtherTools.SystemDistance
{
    /// <summary>
    /// 星系候选搜索（纯逻辑，不依赖界面）：
    /// 按「名称 / 拼音首字母」模糊匹配并给出相关度排序。
    ///
    /// 抽成独立类是为了能写单元测试——拼音首字母匹配曾经整体失效
    /// （边界表把 GB2312 码位当 Unicode 比较），用户输入 jt 找不到「吉他」。
    /// </summary>
    public static class SystemSearch
    {
        /// <summary>模糊搜索：返回按相关度降序的候选星系。</summary>
        public static List<SolarSystem> Search(
            IReadOnlyList<SolarSystem> systems, string input, int limit = 15)
        {
            if (systems == null || systems.Count == 0)
                return new List<SolarSystem>();

            string q = (input ?? string.Empty).Trim().ToLowerInvariant();
            if (q.Length == 0) return new List<SolarSystem>();

            var scored = new List<(SolarSystem System, int Score)>();
            foreach (var s in systems)
            {
                int sc = Score(s, q);
                if (sc > 0) scored.Add((s, sc));
            }

            return scored
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.System.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.System)
                .Take(limit)
                .ToList();
        }

        /// <summary>相关度评分：完全匹配 &gt; 名称前缀 &gt; 拼音全等 &gt; 拼音前缀 &gt; 名称包含 &gt; 拼音包含。</summary>
        public static int Score(SolarSystem s, string q)
        {
            if (s == null) return 0;

            string name = (s.Name ?? string.Empty).ToLowerInvariant();
            string py = (s.PinyinInitial ?? string.Empty).ToLowerInvariant();

            if (name == q) return 100;
            if (name.StartsWith(q, StringComparison.Ordinal)) return 90;
            if (py == q) return 85;
            if (py.StartsWith(q, StringComparison.Ordinal)) return 80;
            if (name.Contains(q, StringComparison.Ordinal)) return 60;
            if (py.Contains(q, StringComparison.Ordinal)) return 50;
            return 0;
        }
    }
}

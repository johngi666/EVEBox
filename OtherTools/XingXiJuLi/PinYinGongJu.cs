using System;
using System.Text;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// 拼音首字母工具：把中文转成拼音首字母（用于曙光服中文星系名的模糊搜索）。
    /// 采用 GB2312 一级汉字按拼音分组的 Unicode 区间表，无需外部依赖。
    /// </summary>
    public static class PinYinGongJu
    {
        // 拼音首字母对应的汉字起始码点（按拼音排序，共 23 个，无 I/U/V）
        private static readonly string[] _shouZiMu =
            { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "W", "X", "Y", "Z" };

        private static readonly int[] _bianJie =
        {
            0x963F, 0x82AD, 0x64B6, 0x54D2, 0x86FE, 0x53D1, 0x5656, 0x54C8,
            0x51FB, 0x5580, 0x62C9, 0x5988, 0x62FF, 0x54E6, 0x6015, 0x6B3E,
            0x7136, 0x6492, 0x5854, 0x6318, 0x897F, 0x538B, 0x5480
        };

        /// <summary>
        /// 取单个汉字的拼音首字母（A-Z）；非汉字字符原样返回（小写）。
        /// </summary>
        public static char QuShouZiMu(char c)
        {
            int code = c;
            if (code < 0x4E00 || code > 0x9FA5)
            {
                return char.ToLowerInvariant(c);
            }

            for (int i = _bianJie.Length - 1; i >= 0; i--)
            {
                if (code >= _bianJie[i])
                {
                    return char.ToLowerInvariant(_shouZiMu[i][0]);
                }
            }
            return c;
        }

        /// <summary>
        /// 取字符串的拼音首字母（中文→首字母，英文/数字→原样小写）。
        /// 例如 "坦欧" → "to"，"Jita" → "jita"。
        /// </summary>
        public static string QuShouZiMu(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                sb.Append(QuShouZiMu(c));
            }
            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EVEBox.OtherTools.SystemDistance
{
    /// <summary>
    /// 拼音首字母工具：把中文转成拼音首字母（用于曙光服中文星系名的模糊搜索）。
    ///
    /// 原理：GB2312 一级字库（3755 个常用字）就是**按拼音排序**的，
    /// 所以把汉字换算成它在 GB2312 里的顺序号，再对照各拼音字母的分界序号即可。
    /// 无需外部词库，也不依赖任何运行时数据文件。
    ///
    /// 历史坑：早期版本直接拿 Unicode 码点去比「GB2312 的分界码位」，
    /// 两套编码错位，导致几乎所有汉字都算错（吉他 → "y他"、索巴色基 → "zzzz"）。
    /// 现在统一走 GB2312 顺序号，并有 PinyinHelperTests 覆盖常用星系名。
    /// </summary>
    public static class PinyinHelper
    {
        /// <summary>拼音首字母，按 GB2312 一级字库中的先后顺序排列（无 I / U / V）</summary>
        private static readonly char[] _ziMu =
            { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'W', 'X', 'Y', 'Z' };

        /// <summary>
        /// 每个拼音字母在 GB2312 一级字库中的起始序号（0 起，共 3755 字）。
        /// 序号 = (GB 高字节 - 0xB0) * 94 + (GB 低字节 - 0xA1)。
        /// 对应各段的起始字依次为：
        /// 阿 芭 擦 搭 蛾 发 噶 哈 击 喀 垃 妈 拿 哦 啪 期 然 撒 塌 挖 昔 压 匝
        /// </summary>
        private static readonly int[] _qiShiXuHao =
        {
            1, 36, 220, 453, 637, 659, 784, 939, 1120, 1415, 1515, 1763,
            1914, 1995, 2003, 2125, 2282, 2341, 2627, 2783, 2903, 3126, 3432
        };

        /// <summary>GB2312 一级字库容量</summary>
        private const int YiJiZiShu = 3755;

        /// <summary>
        /// GB2312 一级字库之外、但星系名里确实用到的字 → 拼音首字母。
        ///
        /// 一级字库只收 3755 个常用字，以下 13 个字不在其中（曙光服 1975 个中文星系名里
        /// 有 12 个以它们开头），不补的话这些名字的拼音检索会失效。
        /// 取的是常见读音：多音字（伽 jiā/qié/gā、忒 tè/tuī、缪 miù/móu/miào）
        /// 按星系名里的习惯读法选定。
        /// </summary>
        private static readonly Dictionary<char, char> _shengPiZi = new Dictionary<char, char>
        {
            ['伽'] = 'j', ['姗'] = 's', ['娅'] = 'y', ['忒'] = 't', ['斐'] = 'f',
            ['滕'] = 't', ['珥'] = 'e', ['缪'] = 'm', ['讷'] = 'n', ['迦'] = 'j',
            ['逖'] = 't', ['铎'] = 'd', ['黛'] = 'd',
        };

        private static readonly Encoding _gb2312 = ZhiZaoGb2312();

        private static Encoding ZhiZaoGb2312()
        {
            try
            {
                // .NET Core 起非 Unicode 编码需要显式注册提供程序
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding("GB2312");
            }
            catch
            {
                // 极端环境下拿不到 GB2312 时退化为 null，QuShouZiMu 会走兜底分支
                return null;
            }
        }

        /// <summary>
        /// 取单个汉字的拼音首字母（小写 a-z）；无法判定的字符原样返回（小写）。
        /// </summary>
        public static char QuShouZiMu(char c)
        {
            if (IsHanZi(c))
            {
                // GB2312 一级字库覆盖不到的字，查补充表
                if (_shengPiZi.TryGetValue(c, out char buChong))
                    return buChong;

                if (_gb2312 != null)
                {
                    byte[] bytes;
                    try
                    {
                        bytes = _gb2312.GetBytes(new[] { c });
                    }
                    catch
                    {
                        bytes = null;
                    }

                    // GB2312 用两个字节表示一个汉字；单字节说明不在 GB2312 里
                    if (bytes != null && bytes.Length == 2)
                    {
                        int xuHao = (bytes[0] - 0xB0) * 94 + (bytes[1] - 0xA1);
                        if (xuHao >= 0 && xuHao < YiJiZiShu)
                        {
                            // 从后往前找第一个不超过它的分界
                            for (int i = _qiShiXuHao.Length - 1; i >= 0; i--)
                            {
                                if (xuHao >= _qiShiXuHao[i])
                                    return char.ToLowerInvariant(_ziMu[i]);
                            }
                        }
                    }
                }
            }

            return char.ToLowerInvariant(c);
        }

        /// <summary>
        /// 取字符串的拼音首字母（中文→首字母，英文/数字→原样小写）。
        /// 例如 "坦欧" → "to"、"吉他" → "jt"、"Jita" → "jita"。
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

        private static bool IsHanZi(char c) => c >= 0x4E00 && c <= 0x9FA5;
    }
}

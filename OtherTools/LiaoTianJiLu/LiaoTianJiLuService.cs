using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EVEBox.OtherTools.LiaoTianJiLu
{
    /// <summary>一次汇总导出的结果</summary>
    public class HuiZongJieGuo
    {
        /// <summary>生成的 txt 完整路径</summary>
        public string ShuChuLuJing { get; set; } = "";

        /// <summary>合并进来的日志文件数</summary>
        public int WenJianShu { get; set; }

        /// <summary>消息条数</summary>
        public int TiaoShu { get; set; }

        /// <summary>正文合计字节数</summary>
        public long ZiJieShu { get; set; }
    }

    /// <summary>
    /// 聊天记录服务，三步：
    /// ① 扫描：只解析文件名建索引（角色 → 频道 → 文件），不打开任何文件；
    /// ② 角色名：每个角色只读一个文件的文件头 Listener 字段，离线、不需要选服务器；
    /// ③ 提取：按时间顺序把各文件的正文（跳过文件头）拼成一条时间线，输出 txt。
    ///
    /// 两个实测结论（决定了这里的实现方式）：
    /// · 文件名里的时间是 UTC，文件头和正文里的时间是本机时间（中国区相差 8 小时），
    ///   所以按时间段筛选必须先把文件名时间换算成本机时间，否则会整段错前一天的 8 小时；
    /// · 国服客户端写的是 UTF-16LE + BOM，而且每条消息行前面还带一个 U+FEFF，
    ///   因此汇总输出统一按 UTF-16LE 写（与源文件一致），记事本才能正常显示。
    /// </summary>
    public class LiaoTianJiLuService
    {
        /// <summary>文件名：频道名_YYYYMMDD_HHMMSS_角色ID.txt（频道名可能带下划线/括号/空格，故从右往左锚定）</summary>
        private static readonly Regex WenJianMingZhengZe = new Regex(
            @"^(?<pin>.+)_(?<ri>\d{8})_(?<shi>\d{6})_(?<id>\d+)$", RegexOptions.Compiled);

        /// <summary>正文里的消息行：[ 2026.09.22 20:31:01 ] 说话人 &gt; 内容</summary>
        private static readonly Regex XiaoXiHangZhengZe = new Regex(
            @"^\[ \d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2} \]", RegexOptions.Compiled);

        /// <summary>UTF-16LE 的 CRLF，用于字节级拼接</summary>
        private static readonly byte[] HuanHang = { 0x0D, 0x00, 0x0A, 0x00 };

        /// <summary>日志目录（Documents\EVE\logs\Chatlogs）</summary>
        public string RizhiMuLu { get; }

        public LiaoTianJiLuService(string riZhiMuLu)
        {
            RizhiMuLu = riZhiMuLu;
        }

        public bool MuLuCunZai => Directory.Exists(RizhiMuLu);

        #region 扫描建索引

        /// <summary>
        /// 扫描目录：只读文件名，建「角色 → 频道 → 文件」索引；
        /// 角色名额外读每个角色一个文件的文件头（每个角色只读一次，不是每条记录查一次）。
        /// </summary>
        public List<LiaoTianJiaoSe> SaoMiao(Action<LiaoTianJiaoSe> jinDu = null)
        {
            var jiaoSeBiao = new Dictionary<string, LiaoTianJiaoSe>();
            if (!MuLuCunZai) return new List<LiaoTianJiaoSe>();

            foreach (string luJing in Directory.GetFiles(RizhiMuLu, "*.txt"))
            {
                var pi = WenJianMingZhengZe.Match(Path.GetFileNameWithoutExtension(luJing));
                if (!pi.Success) continue;      // 命名不符合规则的忽略

                string jiaoSeId = pi.Groups["id"].Value;
                string pinDao = pi.Groups["pin"].Value;

                var wenJian = new LiaoTianWenJian
                {
                    LuJing = luJing,
                    PinDao = pinDao,
                    JiaoSeId = jiaoSeId,
                    UtcShiJian = JieXiShiJian(pi.Groups["ri"].Value, pi.Groups["shi"].Value),
                    DaXiao = ChangDu(luJing),
                };
                wenJian.BenDiShiJian = ZhuanBenDi(wenJian.UtcShiJian);

                if (!jiaoSeBiao.TryGetValue(jiaoSeId, out var jiaoSe))
                {
                    jiaoSe = new LiaoTianJiaoSe { Id = jiaoSeId, MingCheng = jiaoSeId };
                    jiaoSeBiao[jiaoSeId] = jiaoSe;
                }

                if (!jiaoSe.PinDaoBiao.TryGetValue(pinDao, out var lieBiao))
                    jiaoSe.PinDaoBiao[pinDao] = lieBiao = new List<LiaoTianWenJian>();
                lieBiao.Add(wenJian);
            }

            foreach (var jiaoSe in jiaoSeBiao.Values)
            {
                jiaoSe.MingCheng = ZhaoJiaoSeMing(jiaoSe) ?? jiaoSe.Id;
                jinDu?.Invoke(jiaoSe);
            }

            foreach (var jiaoSe in jiaoSeBiao.Values)
                foreach (var lieBiao in jiaoSe.PinDaoBiao.Values)
                    lieBiao.Sort((a, b) => a.UtcShiJian.CompareTo(b.UtcShiJian));

            return jiaoSeBiao.Values
                             .OrderBy(x => x.MingCheng, StringComparer.CurrentCulture)
                             .ThenBy(x => x.Id, StringComparer.Ordinal)
                             .ToList();
        }

        /// <summary>同一个角色各文件的 Listener 一致，读最早的几个里任意一个能读到的即可</summary>
        private static string ZhaoJiaoSeMing(LiaoTianJiaoSe jiaoSe)
        {
            foreach (var wenJian in jiaoSe.SuoYouWenJian().OrderBy(x => x.UtcShiJian).Take(3))
            {
                string ming = DuJiaoSeMing(wenJian.LuJing);
                if (!string.IsNullOrEmpty(ming)) return ming;
            }
            return null;
        }

        /// <summary>读文件头的 Listener 字段（内容就是角色名），离线、不需要选服务器</summary>
        public static string DuJiaoSeMing(string luJing)
        {
            try
            {
                // StreamReader 按 BOM 自动识别编码，源文件是 UTF-16LE
                using (var du = new StreamReader(luJing, Encoding.UTF8, true))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        string hang = du.ReadLine();
                        if (hang == null) break;

                        // 头部行有时也会被写上零宽字符，先去掉再判断
                        string jingHua = hang.TrimStart('\uFEFF').Trim();
                        int maoHao = jingHua.IndexOf(':');
                        if (maoHao <= 0) continue;
                        if (jingHua.Substring(0, maoHao).Trim() != "Listener") continue;

                        return jingHua.Substring(maoHao + 1).Trim().TrimStart('\uFEFF').Trim();
                    }
                }
            }
            catch (Exception)
            {
                // 读不了就当没读到，调用方会用角色 ID 顶上
            }
            return null;
        }

        private static long ChangDu(string luJing)
        {
            try { return new FileInfo(luJing).Length; }
            catch (Exception) { return 0; }
        }

        private static DateTime JieXiShiJian(string ri, string shi)
        {
            try
            {
                return new DateTime(
                    int.Parse(ri.Substring(0, 4)), int.Parse(ri.Substring(4, 2)), int.Parse(ri.Substring(6, 2)),
                    int.Parse(shi.Substring(0, 2)), int.Parse(shi.Substring(2, 2)), int.Parse(shi.Substring(4, 2)),
                    DateTimeKind.Utc);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>文件名时间是 UTC，正文时间是本机时间，这里换算成同一套坐标系</summary>
        public static DateTime ZhuanBenDi(DateTime utc)
        {
            if (utc == DateTime.MinValue) return DateTime.MinValue;
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Local);
        }

        #endregion

        #region 按时间段筛选

        /// <summary>按本机日期筛选（含首尾整天），结果按时间升序</summary>
        public static List<LiaoTianWenJian> GuoLu(List<LiaoTianWenJian> quanBu, DateTime qi, DateTime zhi)
        {
            if (quanBu == null) return new List<LiaoTianWenJian>();

            DateTime qiTian = qi.Date;
            DateTime zhiTian = zhi.Date.AddDays(1);     // 含结束日整天
            return quanBu.Where(x => x.BenDiShiJian >= qiTian && x.BenDiShiJian < zhiTian)
                         .OrderBy(x => x.UtcShiJian)
                         .ToList();
        }

        #endregion

        #region 汇总导出

        /// <summary>
        /// 汇总导出：按时间顺序把各文件的正文（不含文件头）拼成一条时间线，
        /// 输出 UTF-16LE + BOM 的 txt，记事本可直接打开。
        /// </summary>
        public HuiZongJieGuo HuiZong(string shuChuLuJing, LiaoTianJiaoSe jiaoSe, string pinDao,
                                     DateTime benDiQi, DateTime benDiZhi, List<LiaoTianWenJian> wenJianBiao)
        {
            int tiaoShu = 0;
            long ziJieShu = 0;
            int heBingShu = 0;

            using (var zhengWen = new MemoryStream())
            {
                foreach (var wenJian in wenJianBiao)
                {
                    byte[] ziJie = QuZhengWen(wenJian.LuJing, out int tiao);
                    if (ziJie.Length == 0) continue;      // 空会话（进了频道没说话）不占位置

                    heBingShu++;
                    tiaoShu += tiao;
                    ziJieShu += ziJie.Length;

                    zhengWen.Write(ziJie, 0, ziJie.Length);
                    zhengWen.Write(HuanHang, 0, HuanHang.Length);   // 会话之间空一行
                }

                using (var shuChu = new FileStream(shuChuLuJing, FileMode.CreateNew, FileAccess.Write))
                {
                    byte[] biaoTou = Encoding.Unicode.GetBytes(
                        JianBiaoTou(jiaoSe, pinDao, benDiQi, benDiZhi, heBingShu, tiaoShu, ziJieShu));

                    shuChu.Write(new byte[] { 0xFF, 0xFE }, 0, 2);      // UTF-16LE BOM
                    shuChu.Write(biaoTou, 0, biaoTou.Length);

                    zhengWen.Position = 0;
                    zhengWen.CopyTo(shuChu);
                }
            }

            return new HuiZongJieGuo
            {
                ShuChuLuJing = shuChuLuJing,
                WenJianShu = heBingShu,
                TiaoShu = tiaoShu,
                ZiJieShu = ziJieShu,
            };
        }

        private static string JianBiaoTou(LiaoTianJiaoSe jiaoSe, string pinDao, DateTime qi, DateTime zhi,
                                          int wenJianShu, int tiaoShu, long ziJieShu)
        {
            string xian = new string('=', 60);
            var wenBen = new StringBuilder();
            wenBen.Append(xian).Append("\r\n");
            wenBen.Append("  EVE BOX 聊天记录汇总\r\n");
            wenBen.Append($"  角色：{jiaoSe.MingCheng}（{jiaoSe.Id}）\r\n");
            wenBen.Append($"  频道：{pinDao}\r\n");
            wenBen.Append($"  时间段：{qi:yyyy.MM.dd} ~ {zhi:yyyy.MM.dd}（本机时间，含结束日整天）\r\n");
            wenBen.Append($"  文件数：{wenJianShu}　　消息条数：{tiaoShu}\r\n");
            wenBen.Append($"  合计 {ziJieShu / 1024.0 / 1024.0:0.##} MB　　导出时间：{DateTime.Now:yyyy.MM.dd HH:mm:ss}\r\n");
            wenBen.Append(xian).Append("\r\n\r\n");
            return wenBen.ToString();
        }

        /// <summary>取文件正文（跳过文件头），统一输出 UTF-16LE 字节；tiaoShu 为消息条数</summary>
        public static byte[] QuZhengWen(string luJing, out int tiaoShu)
        {
            byte[] yuan = File.ReadAllBytes(luJing);

            // 国服客户端：UTF-16LE + BOM，直接按字节切，最快而且完全保真
            if (yuan.Length >= 2 && yuan[0] == 0xFF && yuan[1] == 0xFE)
            {
                int qiDian = ZhaoZhengWenQiDian(yuan);
                tiaoShu = ShuXiaoXiHang(yuan, qiDian);

                // 找不到正文起点：整篇没有消息行的算空会话（没内容可并）；
                // 有消息行却定位失败的，整个文件保留，宁多勿丢
                if (qiDian <= 0)
                    return tiaoShu == 0 ? Array.Empty<byte>() : yuan;

                var jieGuo = new byte[yuan.Length - qiDian];
                Buffer.BlockCopy(yuan, qiDian, jieGuo, 0, jieGuo.Length);
                return jieGuo;
            }

            // 其他编码（UTF-8 等）：解码后定位正文，再统一转成 UTF-16LE，避免输出里混编码
            string wenBen;
            using (var yuanLiu = new MemoryStream(yuan))
            using (var du = new StreamReader(yuanLiu, Encoding.UTF8, true))
                wenBen = du.ReadToEnd();

            int wei = ZhaoZhengWenWei(wenBen);
            string zhengWen = wei <= 0 ? wenBen : wenBen.Substring(wei);
            tiaoShu = ZhengWenTiaoShu(zhengWen);
            return tiaoShu == 0 ? Array.Empty<byte>() : Encoding.Unicode.GetBytes(zhengWen);
        }

        /// <summary>找第一条消息行的字节偏移；找不到返回 0（由调用方判断是空会话还是整篇保留）</summary>
        private static int ZhaoZhengWenQiDian(byte[] shuJu)
        {
            for (int i = 2; i + 20 <= shuJu.Length; i += 2)
                if (ShiXiaoXiHang(shuJu, i)) return i;
            return 0;
        }

        /// <summary>从 i 开始是不是一条消息行：'[' + ' ' + 四位数字</summary>
        private static bool ShiXiaoXiHang(byte[] shuJu, int i)
        {
            if (shuJu[i] != 0x5B || shuJu[i + 1] != 0x00) return false;       // '['
            if (shuJu[i + 2] != 0x20 || shuJu[i + 3] != 0x00) return false;   // ' '
            for (int j = 0; j < 4; j++)
            {
                if (shuJu[i + 4 + j * 2 + 1] != 0x00) return false;
                byte ziJie = shuJu[i + 4 + j * 2];
                if (ziJie < 0x30 || ziJie > 0x39) return false;               // 0-9
            }
            return true;
        }

        private static int ShuXiaoXiHang(byte[] shuJu, int qiDian)
        {
            int tiao = 0;
            for (int i = qiDian; i + 20 <= shuJu.Length; i += 2)
                if (ShiXiaoXiHang(shuJu, i)) tiao++;
            return tiao;
        }

        private static int ZhaoZhengWenWei(string wenBen)
        {
            int wei = 0;
            foreach (string hang in wenBen.Replace("\r\n", "\n").Split('\n'))
            {
                if (XiaoXiHangZhengZe.IsMatch(hang.TrimStart('\uFEFF'))) return wei;
                wei += hang.Length + 1;
            }
            return 0;
        }

        private static int ZhengWenTiaoShu(string zhengWen)
        {
            return zhengWen.Replace("\r\n", "\n").Split('\n')
                           .Count(x => XiaoXiHangZhengZe.IsMatch(x.TrimStart('\uFEFF')));
        }

        #endregion

        #region 输出与打开

        /// <summary>
        /// 桌面上取一个不重名的输出路径：角色名_起止日期.txt（同名的自动加 (2)(3)，绝不覆盖已有文件）
        /// </summary>
        public static string ZhiShuChuLuJing(string jiaoSeMing, DateTime qi, DateTime zhi)
        {
            string zhuoMian = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shiDuan = qi.Date == zhi.Date
                ? qi.ToString("yyyyMMdd")
                : qi.ToString("yyyyMMdd") + "-" + zhi.ToString("yyyyMMdd");
            string jiChu = AnQuanWenJianMing($"{jiaoSeMing}_{shiDuan}");

            string luJing = Path.Combine(zhuoMian, jiChu + ".txt");
            for (int i = 2; File.Exists(luJing); i++)
                luJing = Path.Combine(zhuoMian, $"{jiChu}({i}).txt");
            return luJing;
        }

        /// <summary>把文件名里不能用的字符换掉（频道名/角色名可能带括号等）</summary>
        public static string AnQuanWenJianMing(string ming)
        {
            if (string.IsNullOrEmpty(ming)) return "聊天记录";
            foreach (char ziFu in Path.GetInvalidFileNameChars())
                ming = ming.Replace(ziFu, '_');
            ming = ming.Trim();
            return ming.Length == 0 ? "聊天记录" : ming;
        }

        /// <summary>用默认程序打开文件（txt 就是记事本）</summary>
        public static void DaKaiWenJian(string luJing)
        {
            if (string.IsNullOrEmpty(luJing) || !File.Exists(luJing)) return;
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(luJing) { UseShellExecute = true });
        }

        /// <summary>在资源管理器中打开目录</summary>
        public static void DaKaiMuLu(string muLu)
        {
            if (string.IsNullOrEmpty(muLu) || !Directory.Exists(muLu)) return;
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(muLu) { UseShellExecute = true });
        }

        #endregion
    }
}

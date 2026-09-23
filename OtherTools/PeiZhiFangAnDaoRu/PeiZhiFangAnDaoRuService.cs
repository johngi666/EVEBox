using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EVEBox.OtherTools.PeiZhiFangAnDaoRu
{
    /// <summary>一次配置方案导入的结果统计</summary>
    public class DaoRuJieGuo
    {
        /// <summary>已覆盖的目标文件数</summary>
        public int TiHuanShu { get; set; }

        /// <summary>目标里原本没有该类型文件、新增进去的文件数</summary>
        public int XinZengShu { get; set; }

        /// <summary>被占用或写入失败而跳过的文件数</summary>
        public int TiaoGuoShu { get; set; }

        /// <summary>是否有文件真正落到了目标目录</summary>
        public bool ChengGong => TiHuanShu + XinZengShu > 0;
    }

    /// <summary>
    /// 配置方案导入服务：把一套配置方案包（一个文件夹 = 一个包，里面分别放一个 char 文件和一个 user 文件）
    /// 克隆替换到所选服务器的 settings_Default。
    /// 覆盖逻辑与「配置同步」的完整同步一致——以包里的 char / user 文件为准，覆盖目标目录下全部同类型文件，
    /// 使该服务器下的所有角色共用这套配置。
    /// </summary>
    public class PeiZhiFangAnDaoRuService
    {
        private const string CharZhengZe = @"^core_char_\d+\.dat$";
        private const string UserZhengZe = @"^core_user_\d+\.dat$";

        /// <summary>内置方案包目录（编译时从本模块的 MoBan 复制到输出目录的 templates\PeiZhiFangAnDaoRu）</summary>
        public string NeiZhiFangAnMuLu { get; }

        public PeiZhiFangAnDaoRuService(string neiZhiFangAnMuLu)
        {
            NeiZhiFangAnMuLu = neiZhiFangAnMuLu;
            // 目录先建好，用户可以直接往里放自己收集的方案包
            Directory.CreateDirectory(NeiZhiFangAnMuLu);
        }

        /// <summary>内置方案包列表：内置目录下每个子文件夹 = 一个方案包</summary>
        public List<FangAnXiang> LieChuNeiZhiFangAn()
        {
            var list = new List<FangAnXiang>();
            if (!Directory.Exists(NeiZhiFangAnMuLu)) return list;

            foreach (var dir in Directory.GetDirectories(NeiZhiFangAnMuLu)
                                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(new FangAnXiang
                {
                    MingCheng = Path.GetFileName(dir),
                    LuJing = dir,
                    ZhaiYao = ZhaiYao(dir)
                });
            }
            return list;
        }

        /// <summary>方案内容摘要；不是有效方案（没有 char / user 文件）时返回空串</summary>
        public static string ZhaiYao(string fangAnLuJing)
        {
            if (string.IsNullOrEmpty(fangAnLuJing) || !Directory.Exists(fangAnLuJing)) return "";

            int charShu = JiShu(fangAnLuJing, CharZhengZe);
            int userShu = JiShu(fangAnLuJing, UserZhengZe);
            if (charShu == 0 && userShu == 0) return "";

            var buFen = new List<string>();
            if (charShu > 0) buFen.Add($"char 文件 {charShu} 个");
            if (userShu > 0) buFen.Add($"user 文件 {userShu} 个");
            return string.Join(" / ", buFen);
        }

        /// <summary>该文件夹里是否有可用的 char / user 文件</summary>
        public static bool ShiYouXiaoFangAn(string fangAnLuJing) => ZhaiYao(fangAnLuJing).Length > 0;

        /// <summary>
        /// 克隆替换：把方案里的 char / user 文件覆盖到目标 settings_Default。
        /// </summary>
        public DaoRuJieGuo YingYong(string fangAnLuJing, string muBiaoLuJing, Action<string> log)
        {
            if (string.IsNullOrEmpty(fangAnLuJing) || !Directory.Exists(fangAnLuJing))
                throw new DirectoryNotFoundException($"方案文件夹不存在: {fangAnLuJing}");
            if (string.IsNullOrEmpty(muBiaoLuJing) || !Directory.Exists(muBiaoLuJing))
                throw new DirectoryNotFoundException($"目标文件夹不存在: {muBiaoLuJing}");
            if (TongYiLuJing(fangAnLuJing, muBiaoLuJing))
                throw new InvalidOperationException("方案文件夹与目标文件夹是同一个，无需导入。");

            var jieGuo = new DaoRuJieGuo();
            log?.Invoke($"方案包：{Path.GetFileName(fangAnLuJing.TrimEnd(Path.DirectorySeparatorChar))}");
            log?.Invoke($"目标：{muBiaoLuJing}");

            ChongFu(fangAnLuJing, muBiaoLuJing, UserZhengZe, "用户文件", jieGuo, log);
            ChongFu(fangAnLuJing, muBiaoLuJing, CharZhengZe, "角色文件", jieGuo, log);

            log?.Invoke($"覆盖 {jieGuo.TiHuanShu} 个，新增 {jieGuo.XinZengShu} 个，跳过 {jieGuo.TiaoGuoShu} 个");
            return jieGuo;
        }

        /// <summary>用方案包里的 char / user 文件覆盖目标目录下全部同类型文件</summary>
        private static void ChongFu(string fangAnLuJing, string muBiaoLuJing, string zhengZe, string leiXing,
                                    DaoRuJieGuo jieGuo, Action<string> log)
        {
            var baoNeiWenJian = Directory.GetFiles(fangAnLuJing)
                                         .Where(f => Regex.IsMatch(Path.GetFileName(f), zhengZe))
                                         .ToList();
            if (baoNeiWenJian.Count == 0)
            {
                log?.Invoke($"{leiXing}：方案包里没有，跳过");
                return;
            }

            // 方案包约定是一个包里放一个 char + 一个 user；万一是多个，取最新的那个
            string laiYuan = baoNeiWenJian.OrderByDescending(File.GetLastWriteTime).First();
            if (baoNeiWenJian.Count > 1)
                log?.Invoke($"{leiXing}：方案包里有 {baoNeiWenJian.Count} 个，只取最新的 {Path.GetFileName(laiYuan)}");

            var muBiaoWenJian = Directory.GetFiles(muBiaoLuJing)
                                         .Where(f => Regex.IsMatch(Path.GetFileName(f), zhengZe))
                                         .ToList();

            // 目标里还没有这类文件：直接放一份进去，否则方案内容无处可落
            if (muBiaoWenJian.Count == 0)
            {
                string xinWenJian = Path.Combine(muBiaoLuJing, Path.GetFileName(laiYuan));
                try
                {
                    File.Copy(laiYuan, xinWenJian, true);
                    jieGuo.XinZengShu++;
                    log?.Invoke($"{leiXing}：目标里没有，已新增 {Path.GetFileName(xinWenJian)}");
                }
                catch (Exception ex)
                {
                    jieGuo.TiaoGuoShu++;
                    log?.Invoke($"{leiXing}：新增失败 - {ex.Message}");
                }
                return;
            }

            log?.Invoke($"{leiXing}：以方案包中的 {Path.GetFileName(laiYuan)} 为准，覆盖 {muBiaoWenJian.Count} 个");
            foreach (var muBiao in muBiaoWenJian)
            {
                if (IsFileLocked(muBiao))
                {
                    jieGuo.TiaoGuoShu++;
                    log?.Invoke($"  跳过（被占用）：{Path.GetFileName(muBiao)}");
                    continue;
                }

                try
                {
                    File.Copy(laiYuan, muBiao, true);
                    jieGuo.TiHuanShu++;
                    log?.Invoke($"  已覆盖：{Path.GetFileName(muBiao)}");
                }
                catch (Exception ex)
                {
                    jieGuo.TiaoGuoShu++;
                    log?.Invoke($"  覆盖失败：{Path.GetFileName(muBiao)} - {ex.Message}");
                }
            }
        }

        private static int JiShu(string muLu, string zhengZe)
        {
            if (!Directory.Exists(muLu)) return 0;
            return Directory.GetFiles(muLu).Count(f => Regex.IsMatch(Path.GetFileName(f), zhengZe));
        }

        /// <summary>文件被其他进程占用（EVE 运行中会锁定配置文件）</summary>
        public static bool IsFileLocked(string wenJianLuJing)
        {
            try
            {
                using (File.Open(wenJianLuJing, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        private static bool TongYiLuJing(string a, string b)
        {
            return string.Equals(
                Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>在资源管理器中打开目录</summary>
        public static void DaKaiMuLu(string muLu)
        {
            if (string.IsNullOrEmpty(muLu) || !Directory.Exists(muLu)) return;
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(muLu) { UseShellExecute = true });
        }
    }
}

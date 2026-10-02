using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EVEBox.OtherTools.TemplateImport
{
    /// <summary>
    /// 内置模板资源：各模块 MoBan 目录下的模板文件在编译时被嵌进 exe，
    /// 程序启动时按需释放到 Documents\EVE\templates\&lt;模块&gt;\ 下，之后各处仍按原样从磁盘读取。
    /// 放在用户文档下而不是程序目录：程序解压到哪、放临时目录还是 Program Files，模板都照常可用。
    /// 这样发布包只需要一个 exe，自动更新（只替换主程序）也不会丢模板。
    /// </summary>
    public static class BuiltInTemplateResource
    {
        /// <summary>资源名前缀，与 EVEBox.csproj 里 EmbeddedResource 的 LogicalName 对应</summary>
        private const string ZiYuanQianZhui = "EVEBoxTemplates/";

        /// <summary>
        /// 内置模板释放根目录：Documents\EVE\templates
        /// </summary>
        public static string MoBanGenMuLu => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "templates");

        /// <summary>某个模块的内置模板目录：Documents\EVE\templates\&lt;模块&gt;</summary>
        public static string MoBanMuLu(string moKuaiMing) => Path.Combine(MoBanGenMuLu, moKuaiMing);

        /// <summary>资源名里的目录分隔符可能是 \ 或 /，统一按 / 处理</summary>
        private static string GuiFan(string ziYuanMing) => ziYuanMing.Replace('\\', '/');

        /// <summary>列出所有内置模板资源：K=资源全名，V=相对 templates\ 的路径（用 / 分隔）</summary>
        public static List<KeyValuePair<string, string>> LieChuZiYuan()
        {
            var jieGuo = new List<KeyValuePair<string, string>>();
            foreach (string quanMing in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                string guiFanLiang = GuiFan(quanMing);
                int wei = guiFanLiang.IndexOf(ZiYuanQianZhui, StringComparison.Ordinal);
                if (wei < 0) continue;

                string xiangDui = guiFanLiang.Substring(wei + ZiYuanQianZhui.Length);
                if (xiangDui.Length == 0) continue;

                jieGuo.Add(new KeyValuePair<string, string>(quanMing, xiangDui));
            }
            return jieGuo;
        }

        /// <summary>
        /// 旧版本（v6.13 及之前）把内置模板释放在程序目录的 templates 下，现在换到文档目录，
        /// 这里把程序目录那份残留清掉。
        /// 只有目录里全是内置模板原件、没有用户自己放进去的东西时才动，而且送回收站。
        /// </summary>
        public static bool QingLiJiuMuLu(Action<string, string, string> log = null)
            => QingLiJiuMuLu(Path.Combine(AppContext.BaseDirectory, "templates"), log);

        /// <summary>
        /// 清理指定的旧模板目录（jiuMuLu 一般为 程序目录\templates）
        /// </summary>
        public static bool QingLiJiuMuLu(string jiuMuLu, Action<string, string, string> log = null)
        {
            try
            {
                if (!Directory.Exists(jiuMuLu))
                    return false;

                // 防一手：新旧目录不可能同一个，真遇上也别动
                if (string.Equals(Path.GetFullPath(jiuMuLu), Path.GetFullPath(MoBanGenMuLu),
                        StringComparison.OrdinalIgnoreCase))
                    return false;

                string yuWai = ZhaoChuFeiNeiZhiWenJian(jiuMuLu);
                if (yuWai != null)
                {
                    log?.Invoke("内置模板", "旧目录保留（里面有非内置文件）", yuWai);
                    return false;
                }

                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    jiuMuLu,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                log?.Invoke("内置模板", "已把程序目录下的旧 templates 送进回收站", jiuMuLu);
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke("内置模板", "清理旧模板目录失败", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 内置模块目录的旧中文名 → 新英文名。
        ///
        /// 模板模块目录在英文化后改了名（如 ZhongCaiMoBan → PlantingTemplate）。
        /// 老用户的 文档\EVE\templates 下会留着旧目录，与新目录并排出现重复模板，
        /// 所以启动时把旧目录清掉——但只清「内容全是内置原件」的，
        /// 用户自己往里加的文件会保留整个目录（与程序目录旧 templates 的处理一致）。
        /// </summary>
        private static readonly Dictionary<string, string> JiuMoKuaiMing = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PeiZhiFangAnDaoRu"] = "ConfigSchemeImport",
            ["ZhongCaiMoBan"] = "PlantingTemplate",
            ["ZhuangPeiFangAn"] = "FittingPlan",
            ["ZongLanMoBan"] = "OverviewTemplate",
        };

        /// <summary>
        /// 清理 文档\EVE\templates 下遗留的旧中文模块目录（英文化前释放的）。
        /// 只清理内容全是内置原件、没有用户自加文件的目录；一律送回收站。
        /// 返回清理掉的目录数。
        /// </summary>
        public static int QingLiJiuMoKuaiMuLu(Action<string, string, string> log = null)
            => QingLiJiuMoKuaiMuLu(MoBanGenMuLu, log);

        /// <summary>
        /// 清理指定根目录下遗留的旧中文模块目录（测试用；生产走 MoBanGenMuLu 那个重载）。
        /// </summary>
        public static int QingLiJiuMoKuaiMuLu(string genMuLu, Action<string, string, string> log = null)
        {
            int qingLiShu = 0;

            foreach (var pai in JiuMoKuaiMing)
            {
                string jiuMuLu = Path.Combine(genMuLu, pai.Key);
                string xinMuLu = Path.Combine(genMuLu, pai.Value);

                try
                {
                    if (!Directory.Exists(jiuMuLu)) continue;

                    // 防一手：新旧同名时绝不动
                    if (string.Equals(Path.GetFullPath(jiuMuLu), Path.GetFullPath(xinMuLu),
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    string yuWai = ZhaoChuFeiNeiZhiWenJian(jiuMuLu, pai.Value);
                    if (yuWai != null)
                    {
                        log?.Invoke("内置模板", "旧模板目录保留（里面有非内置文件）", yuWai);
                        continue;
                    }

                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                        jiuMuLu,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                    qingLiShu++;
                    log?.Invoke("内置模板", "已把改名前的旧模板目录送进回收站", jiuMuLu);
                }
                catch (Exception ex)
                {
                    log?.Invoke("内置模板", "清理旧模板目录失败", $"{jiuMuLu}: {ex.Message}");
                }
            }

            return qingLiShu;
        }

        /// <summary>
        /// 找出旧目录里不属于内置模板的文件（按「模块内的相对路径」比对，与模块名叫什么无关）；
        /// 全是内置模板则返回 null。
        /// </summary>
        /// <param name="jiuMuLu">要检查的目录（可能是旧中文模块目录）</param>
        /// <param name="moKuaiMing">该目录对应的模块名（用于取内置文件清单）</param>
        public static string ZhaoChuFeiNeiZhiWenJian(string jiuMuLu, string moKuaiMing = null)
        {
            var huiBian = Assembly.GetExecutingAssembly();
            var neiZhi = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            string qianZhui = string.IsNullOrEmpty(moKuaiMing) ? null : moKuaiMing + "/";

            foreach (var pai in LieChuZiYuan())
            {
                using var ziYuanLiu = huiBian.GetManifestResourceStream(pai.Key);
                if (ziYuanLiu == null) continue;

                // 指定模块时，只取该模块的资源，并剥掉模块名，
                // 这样无论是旧中文目录还是新英文目录都能按同样的相对路径比对
                if (qianZhui != null)
                {
                    if (!pai.Value.StartsWith(qianZhui, StringComparison.OrdinalIgnoreCase)) continue;
                    neiZhi[pai.Value.Substring(qianZhui.Length)] = ziYuanLiu.Length;
                }
                else
                {
                    neiZhi[pai.Value] = ziYuanLiu.Length;
                }
            }

            foreach (string wenJian in Directory.GetFiles(jiuMuLu, "*", SearchOption.AllDirectories))
            {
                string xiangDui = Path.GetRelativePath(jiuMuLu, wenJian).Replace('\\', '/');
                if (!neiZhi.TryGetValue(xiangDui, out long changDu) ||
                    new FileInfo(wenJian).Length != changDu)
                {
                    return wenJian;
                }
            }

            return null;
        }

        /// <summary>
        /// 确保内置模板在 muBiaoGenMuLu（一般传 MoBanGenMuLu，即 文档\EVE\templates）下齐全：
        /// 文件不存在就写出，存在但大小和内置的不一致就当缺失/损坏，重新写出。
        /// 内容一致的文件不动（不会每次启动都重写）。返回实际写出的文件数。
        /// </summary>
        public static int QueBaoDaoChu(string muBiaoGenMuLu, Action<string, string, string> log = null)
        {
            int xieChuShu = 0;
            var huiBian = Assembly.GetExecutingAssembly();

            foreach (var pai in LieChuZiYuan())
            {
                string muBiaoLuJing = Path.Combine(
                    muBiaoGenMuLu, pai.Value.Replace('/', Path.DirectorySeparatorChar));

                try
                {
                    using var ziYuanLiu = huiBian.GetManifestResourceStream(pai.Key);
                    if (ziYuanLiu == null) continue;

                    // 已存在且大小一致，认为完好，不覆盖（保留用户放在这里的东西）
                    if (File.Exists(muBiaoLuJing) && new FileInfo(muBiaoLuJing).Length == ziYuanLiu.Length)
                        continue;

                    string muLu = Path.GetDirectoryName(muBiaoLuJing);
                    if (!string.IsNullOrEmpty(muLu)) Directory.CreateDirectory(muLu);

                    using (var shuChuLiu = File.Create(muBiaoLuJing))
                        ziYuanLiu.CopyTo(shuChuLiu);

                    xieChuShu++;
                }
                catch (Exception ex)
                {
                    log?.Invoke("内置模板", $"释放失败：{pai.Value}", ex.Message);
                }
            }

            if (xieChuShu > 0)
                log?.Invoke("内置模板", $"已释放内置模板 {xieChuShu} 个", muBiaoGenMuLu);

            return xieChuShu;
        }
    }
}

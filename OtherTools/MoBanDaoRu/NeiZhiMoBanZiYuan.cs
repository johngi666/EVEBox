using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EVEBox.OtherTools.MoBanDaoRu
{
    /// <summary>
    /// 内置模板资源：各模块 MoBan 目录下的模板文件在编译时被嵌进 exe，
    /// 程序启动时按需释放到程序目录的 templates\&lt;模块&gt;\ 下，之后各处仍按原样从磁盘读取。
    /// 这样发布包只需要一个 exe，自动更新（只替换主程序）也不会丢模板。
    /// </summary>
    public static class NeiZhiMoBanZiYuan
    {
        /// <summary>资源名前缀，与 EVEBox.csproj 里 EmbeddedResource 的 LogicalName 对应</summary>
        private const string ZiYuanQianZhui = "EVEBoxTemplates/";

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
        /// 确保内置模板在 muBiaoGenMuLu（一般传 程序目录\templates）下齐全：
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

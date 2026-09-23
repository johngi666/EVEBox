using EVEBox.App;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EVEBox.Features.GengXin
{
    /// <summary>
    /// 主程序文件名修正
    /// 旧版自动更新是「下载新 exe 后覆盖当前 exe 路径」，所以程序更名后文件名还是旧产品名
    /// （EVE配置管理工具.exe）。启动时若发现文件名仍是旧产品名，就改名为标准名并重启，
    /// 同时把桌面/开始菜单里指向旧路径的快捷方式改指新路径。
    /// </summary>
    public static class ChengXuGaiMingService
    {
        /// <summary>改名失败的标记文件：存在时不再自动重试，避免反复重启</summary>
        public const string ShiBaiBiaoJiWenJian = ".evebox_rename_failed";

        /// <summary>历史产品名：只有这些名字才自动纠正，用户自己改的名字不动</summary>
        private static readonly string[] JiuChanPinMing = { "EVE配置管理工具.exe", "EVESyncTool.exe" };

        private const int ZuiDaChongShiCiShu = 20;

        /// <summary>
        /// 安排了改名重启时返回 true，调用方应立即退出进程。
        /// </summary>
        public static bool GaiMingBingChongQi()
        {
            try
            {
                string dangQian = Application.ExecutablePath;
                string muLu = Path.GetDirectoryName(dangQian);
                if (string.IsNullOrEmpty(muLu))
                    return false;

                string biaoZhun = Path.Combine(muLu, YingYongXinXi.ExeFileName);
                string shiBaiBiaoJi = Path.Combine(muLu, ShiBaiBiaoJiWenJian);

                if (!XuYaoGaiMing(dangQian, biaoZhun, shiBaiBiaoJi))
                    return false;

                XiuZhengKuaiJieFangShi(dangQian, biaoZhun);

                string jiaoBen = Path.Combine(muLu, "rename_exe.cmd");
                File.WriteAllText(jiaoBen, ShengChengGaiMingJiaoBen(dangQian, biaoZhun, shiBaiBiaoJi));

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{jiaoBen}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 是否需要改名：当前名是旧产品名、标准名没被占用、且没有留下失败标记
        /// </summary>
        public static bool XuYaoGaiMing(string dangQianLuJing, string biaoZhunLuJing, string shiBaiBiaoJiLuJing)
        {
            if (string.IsNullOrWhiteSpace(dangQianLuJing) || string.IsNullOrWhiteSpace(biaoZhunLuJing))
                return false;

            if (string.Equals(dangQianLuJing, biaoZhunLuJing, StringComparison.OrdinalIgnoreCase))
                return false;

            string dangQianMing = Path.GetFileName(dangQianLuJing);
            if (!Array.Exists(JiuChanPinMing, n => string.Equals(n, dangQianMing, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (File.Exists(biaoZhunLuJing))
                return false;   // 标准名已被占用，交给用户自己取舍

            if (File.Exists(shiBaiBiaoJiLuJing))
                return false;

            return true;
        }

        /// <summary>
        /// 生成改名脚本内容
        /// 脚本逻辑：等待本进程退出 → 把 exe 改成标准名 → 重启 → 删除自身；
        /// 反复改不动时写失败标记并启动原文件，避免把用户卡在无法启动的状态。
        /// </summary>
        public static string ShengChengGaiMingJiaoBen(
            string dangQianLuJing, string biaoZhunLuJing, string shiBaiBiaoJiLuJing)
        {
            string dangQianMing = Path.GetFileName(dangQianLuJing);
            string biaoZhunMing = Path.GetFileName(biaoZhunLuJing);

            return
                "@echo off\r\n" +
                "chcp 65001 >nul\r\n" +
                "set /a n=0\r\n" +
                ":wait\r\n" +
                $"taskkill /f /im \"{dangQianMing}\" >nul 2>&1\r\n" +
                "timeout /t 1 /nobreak >nul\r\n" +
                "set /a n+=1\r\n" +
                $"if %n% gtr {ZuiDaChongShiCiShu} goto giveup\r\n" +
                $"ren \"{dangQianLuJing}\" \"{biaoZhunMing}\" >nul 2>&1\r\n" +
                $"if exist \"{dangQianLuJing}\" goto wait\r\n" +
                $"start \"\" \"{biaoZhunLuJing}\"\r\n" +
                "del /f \"%~f0\"\r\n" +
                "exit /b\r\n" +
                ":giveup\r\n" +
                $"echo x > \"{shiBaiBiaoJiLuJing}\"\r\n" +
                $"start \"\" \"{dangQianLuJing}\"\r\n" +
                "del /f \"%~f0\"\r\n";
        }

        /// <summary>
        /// 把桌面/开始菜单/任务栏固定项里指向旧路径的快捷方式改指新路径
        /// </summary>
        private static void XiuZhengKuaiJieFangShi(string jiuLuJing, string xinLuJing)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return;
                dynamic shell = Activator.CreateInstance(shellType);

                foreach (string lnk in LieChuKuaiJieFangShi())
                {
                    try
                    {
                        dynamic kuaijie = shell.CreateShortcut(lnk);
                        string muBiao = kuaijie.TargetPath as string;
                        if (string.IsNullOrWhiteSpace(muBiao))
                            continue;
                        if (!string.Equals(muBiao, jiuLuJing, StringComparison.OrdinalIgnoreCase))
                            continue;

                        kuaijie.TargetPath = xinLuJing;

                        string tuBiao = kuaijie.IconLocation as string;
                        if (!string.IsNullOrWhiteSpace(tuBiao) &&
                            tuBiao.StartsWith(jiuLuJing, StringComparison.OrdinalIgnoreCase))
                        {
                            kuaijie.IconLocation = xinLuJing + tuBiao.Substring(jiuLuJing.Length);
                        }

                        kuaijie.Save();
                    }
                    catch
                    {
                        // 单个快捷方式失败不影响其它
                    }
                }
            }
            catch
            {
                // 没有 WScript.Shell 就只改名，不动快捷方式
            }
        }

        private static IEnumerable<string> LieChuKuaiJieFangShi()
        {
            string[] muLu =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar")
            };

            foreach (string m in muLu)
            {
                if (string.IsNullOrEmpty(m) || !Directory.Exists(m))
                    continue;

                string[] wenJian = null;
                try { wenJian = Directory.GetFiles(m, "*.lnk", SearchOption.AllDirectories); }
                catch { }

                if (wenJian == null)
                    continue;

                foreach (string f in wenJian)
                    yield return f;
            }
        }
    }
}

using EVESyncTool.Dialogs.Common;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace EVESyncTool.Core.Services
{
    /// <summary>
    /// EVE 客户端进程守卫：在执行修改文件的操作前检测客户端是否运行。
    /// 有客户端运行时弹窗询问是否一键关闭；选择"是"关闭所有客户端并延迟1秒后继续，
    /// 选择"否"则取消当前操作（返回 false）。
    /// </summary>
    public static class EveClientGuard
    {
        /// <summary>
        /// EVE 客户端主进程名（exefile.exe，进程名匹配不区分大小写）
        /// </summary>
        private const string ClientProcessName = "exefile";

        /// <summary>
        /// 是否有 EVE 客户端正在运行
        /// </summary>
        public static bool IsClientRunning()
        {
            return Process.GetProcessesByName(ClientProcessName).Length > 0;
        }

        /// <summary>
        /// 检测 EVE 客户端并弹窗处理。
        /// 返回 true 表示可继续执行操作；返回 false 表示用户选择不关闭客户端，应取消当前操作。
        /// </summary>
        public static bool EnsureNoClient()
        {
            if (!IsClientRunning()) return true;

            var result = CustomMessageBox.Show(
                "检测到 EVE 客户端正在运行！\n\n" +
                "当前操作会修改配置文件，客户端运行中可能导致文件被占用或写入失败。\n\n" +
                "是否一键关闭所有 EVE 客户端进程？\n" +
                "（选择\"否\"将取消当前操作）",
                "EVE客户端检测",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return false;

            int count = KillAllClients();

            // ★★★ 延迟1秒：等待客户端进程完全退出、文件锁释放 ★★★
            Thread.Sleep(1000);
            return true;
        }

        /// <summary>
        /// 关闭所有 EVE 客户端进程（清杀全部），返回关闭的数量
        /// </summary>
        public static int KillAllClients()
        {
            int count = 0;
            foreach (var process in Process.GetProcessesByName(ClientProcessName))
            {
                try
                {
                    process.Kill();
                    count++;
                }
                catch
                {
                    // 进程已退出或无权限，忽略
                }
            }
            return count;
        }
    }
}

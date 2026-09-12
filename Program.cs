using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EVEBox
{
    static class Program
    {
        private const string MutexName = "EVEBox_Instance_Mutex";
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [STAThread]
        static void Main()
        {
            // 检查是否已有实例在运行
            bool createdNew;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // 已有实例在运行，激活它
                    ActivateExistingWindow();
                    return;
                }

                // 首次启动，正常运行
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }

        /// <summary>
        /// 激活已存在的窗口
        /// </summary>
        private static void ActivateExistingWindow()
        {
            try
            {
                // 通过窗口标题查找
                IntPtr hWnd = FindWindow(null, "EVE BOX");
                if (hWnd != IntPtr.Zero)
                {
                    // 如果窗口最小化，先还原
                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }
                    // 置前
                    SetForegroundWindow(hWnd);
                }
                else
                {
                    // 备用方案：通过进程名查找
                    Process[] processes = Process.GetProcessesByName("EVE BOX");
                    if (processes.Length > 0 && processes[0].MainWindowHandle != IntPtr.Zero)
                    {
                        IntPtr handle = processes[0].MainWindowHandle;
                        if (IsIconic(handle))
                        {
                            ShowWindow(handle, SW_RESTORE);
                        }
                        SetForegroundWindow(handle);
                    }
                }
            }
            catch (Exception)
            {
                // 静默处理激活失败
            }
        }
    }
}
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace EVESyncTool.Core.UI
{
    /// <summary>
    /// 明暗主题管理器
    /// </summary>
    public static class ThemeManager
    {
        // ===== Win32 滚动条暗色支持（Windows 11+） =====
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETREDRAW = 0x000B;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static bool IsDarkMode { get; private set; } = false;

        // 亮色主题
        public static readonly Color Light_Bg = Color.FromArgb(240, 248, 255);
        public static readonly Color Light_Panel = Color.White;
        public static readonly Color Light_TitleBar = Color.FromArgb(70, 130, 180);
        public static readonly Color Light_Text = Color.Black;
        public static readonly Color Light_Accent = Color.FromArgb(70, 130, 180);
        public static readonly Color Light_Separator = Color.FromArgb(200, 200, 200);
        public static readonly Color Light_GridBg = Color.White;
        public static readonly Color Light_GridHeader = Color.FromArgb(240, 248, 255);
        public static readonly Color Light_SelectionBg = Color.FromArgb(70, 130, 180);
        public static readonly Color Light_SelectionFg = Color.White;
        public static readonly Color Light_TitleBtn = Color.White;

        // 暗色主题
        public static readonly Color Dark_Bg = Color.FromArgb(30, 30, 30);
        public static readonly Color Dark_Panel = Color.FromArgb(45, 45, 48);
        public static readonly Color Dark_TitleBar = Color.FromArgb(37, 37, 40);
        public static readonly Color Dark_Text = Color.FromArgb(220, 220, 220);
        public static readonly Color Dark_Accent = Color.FromArgb(100, 160, 210);
        public static readonly Color Dark_Separator = Color.FromArgb(70, 70, 74);
        public static readonly Color Dark_GridBg = Color.FromArgb(37, 37, 40);
        public static readonly Color Dark_GridHeader = Color.FromArgb(55, 55, 60);
        public static readonly Color Dark_SelectionBg = Color.FromArgb(62, 95, 130);
        public static readonly Color Dark_SelectionFg = Color.FromArgb(220, 220, 220);
        public static readonly Color Dark_TitleBtn = Color.FromArgb(200, 200, 200);

        public static Color Bg => IsDarkMode ? Dark_Bg : Light_Bg;
        public static Color Panel => IsDarkMode ? Dark_Panel : Light_Panel;
        public static Color TitleBar => IsDarkMode ? Dark_TitleBar : Light_TitleBar;
        public static Color Text => IsDarkMode ? Dark_Text : Light_Text;
        public static Color Accent => IsDarkMode ? Dark_Accent : Light_Accent;
        public static Color Separator => IsDarkMode ? Dark_Separator : Light_Separator;
        public static Color GridBg => IsDarkMode ? Dark_GridBg : Light_GridBg;
        public static Color GridHeader => IsDarkMode ? Dark_GridHeader : Light_GridHeader;
        public static Color SelectionBg => IsDarkMode ? Dark_SelectionBg : Light_SelectionBg;
        public static Color SelectionFg => IsDarkMode ? Dark_SelectionFg : Light_SelectionFg;
        public static Color TitleBtn => IsDarkMode ? Dark_TitleBtn : Light_TitleBtn;

        public static event Action<bool> ThemeChanged;

        public static void SetDarkMode(bool dark)
        {
            if (IsDarkMode == dark) return;
            IsDarkMode = dark;
            ThemeChanged?.Invoke(dark);
        }

        public static void Toggle()
        {
            SetDarkMode(!IsDarkMode);
        }

        /// <summary>
        /// 将当前主题应用到整个窗体（主窗体与所有弹窗通用）
        /// 整个应用过程在“禁用重绘”块内完成，全部颜色改完后一次性重绘，避免逐控件变色造成的闪烁
        /// </summary>
        public static void ApplyToForm(Form form)
        {
            if (form == null) return;
            BeginThemeUpdate(form);
            try
            {
                ApplyCore(form);
            }
            finally
            {
                EndThemeUpdate(form);
            }
        }

        /// <summary>
        /// 开始批量主题更新：禁用窗体重绘并挂起布局（配合 ApplyCore/EndThemeUpdate 使用）
        /// </summary>
        public static void BeginThemeUpdate(Form form)
        {
            if (form == null) return;
            SendMessage(form.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            form.SuspendLayout();
        }

        /// <summary>
        /// 结束批量主题更新：恢复重绘并整窗一次性刷新
        /// </summary>
        public static void EndThemeUpdate(Form form)
        {
            if (form == null) return;
            form.ResumeLayout(true);
            SendMessage(form.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            form.Invalidate(true);
            form.Update();
        }

        /// <summary>
        /// 仅应用颜色（不管理重绘状态），供 BeginThemeUpdate/EndThemeUpdate 包裹使用
        /// </summary>
        public static void ApplyCore(Form form)
        {
            if (form == null) return;
            form.BackColor = Bg;
            ApplyToControlTree(form);
            ApplyScrollBarTheme(form);
        }

        /// <summary>
        /// 将滚动条切换为暗色（Windows 11+ 生效，Win10 自动忽略）
        /// </summary>
        private static void ApplyScrollBarTheme(Control root)
        {
            try
            {
                if (root.Handle == IntPtr.Zero)
                    root.CreateControl();

                EnumChildWindows(root.Handle, (hWnd, lParam) =>
                {
                    var sb = new StringBuilder(256);
                    GetClassName(hWnd, sb, sb.Capacity);
                    if (sb.ToString() == "SCROLLBAR")
                    {
                        // 夜间模式用暗色滚动条主题，日间模式恢复默认
                        SetWindowTheme(hWnd, IsDarkMode ? "DarkMode_Explorer" : null, null);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
                // 非 Windows 或权限问题忽略
            }
        }

        private static void ApplyToControlTree(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // 标题栏（Dock=Top 且高度<=40 的面板）
                if (ctrl is Panel panel && panel.Dock == DockStyle.Top && panel.Height <= 40)
                {
                    panel.BackColor = TitleBar;
                    foreach (Control child in panel.Controls)
                    {
                        if (child is Label label) label.ForeColor = TitleBtn;
                        else if (child is Button btn)
                        {
                            btn.ForeColor = TitleBtn;
                            btn.BackColor = Color.Transparent;
                        }
                    }
                    continue;
                }

                if (ctrl is DataGridView dgv)
                {
                    dgv.BackgroundColor = GridBg;
                    dgv.DefaultCellStyle.BackColor = GridBg;
                    dgv.DefaultCellStyle.ForeColor = Text;
                    dgv.DefaultCellStyle.SelectionBackColor = SelectionBg;
                    dgv.DefaultCellStyle.SelectionForeColor = SelectionFg;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = Text;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeader;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.GridColor = Separator;
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        col.DefaultCellStyle.BackColor = GridBg;
                        col.DefaultCellStyle.ForeColor = Text;
                        col.DefaultCellStyle.SelectionBackColor = SelectionBg;
                        col.DefaultCellStyle.SelectionForeColor = SelectionFg;
                    }
                }
                else if (ctrl is RichTextBox rtb)
                {
                    rtb.BackColor = GridBg;
                    rtb.ForeColor = Text;
                }
                else if (ctrl is TextBox tb)
                {
                    tb.BackColor = GridBg;
                    tb.ForeColor = Text;
                }
                else if (ctrl is Label label)
                {
                    // 蓝色强调文字 → Accent（识别亮/暗两种主题色，双向切换都能恢复）
                    if (label.ForeColor == Color.FromArgb(70, 130, 180) || label.ForeColor == Dark_Accent)
                        label.ForeColor = Accent;
                    else
                        label.ForeColor = Text;
                }
                else if (ctrl is CheckBox cb)
                {
                    cb.ForeColor = Text;
                }
                else if (ctrl is ComboBox cmb)
                {
                    // 下拉框背景 + 文字
                    cmb.BackColor = GridBg;
                    cmb.ForeColor = Text;
                }
                else if (ctrl is ProgressBar pb)
                {
                    // 轨道色 + 填充色
                    pb.BackColor = GridBg;
                    pb.ForeColor = Accent;
                }
                else if (ctrl is Button btn)
                {
                    // 蓝色主按钮 → Accent（识别亮/暗两种主题色）；透明/彩色按钮（绿/橙/红/灰）保持
                    if (btn.BackColor == Color.FromArgb(70, 130, 180) || btn.BackColor == Dark_Accent)
                        btn.BackColor = Accent;
                }
                else if (ctrl is Panel p)
                {
                    // 分割线（高≤2且宽远大于高）
                    if (p.Height <= 2 && p.Width > p.Height * 10)
                        p.BackColor = Separator;
                    // 非透明面板统一用 Panel 色（无论之前是亮色白底还是暗色深底，双向切换都能恢复）
                    else if (p.BackColor != Color.Transparent)
                        p.BackColor = Panel;
                }

                if (ctrl.HasChildren)
                    ApplyToControlTree(ctrl);
            }
        }
    }
}

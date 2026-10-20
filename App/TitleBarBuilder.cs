using EVEBox.App;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;



namespace EVEBox.App
{
    /// <summary>
    /// 标题栏构建器
    /// </summary>
    public class TitleBarBuilder
    {
        private readonly Form _owner;
        private readonly Panel _titleBar;
        private readonly Button _btnPrivacy;

        public TitleBarBuilder(Form owner)
        {
            _owner = owner;

            _titleBar = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(70, 130, 180),
                Margin = new Padding(0)
            };

            // 标题（版本号只在「更新」页显示，这里只放名称）
            Label titleLabel = new Label
            {
                Text = "EVE BOX",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 5)
            };
            titleLabel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(_owner.Handle, 0xA1, 0x2, 0);
                }
            };

            // 关闭按钮
            Button btnClose = new Button
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(_owner.Width - 40, 0),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => _owner.Close();

            // 最小化按钮
            Button btnMinimize = new Button
            {
                Text = "─",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(_owner.Width - 80, 0),
                Cursor = Cursors.Hand
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.Click += (s, e) => _owner.WindowState = FormWindowState.Minimized;

            // 隐私模式开关
            _btnPrivacy = new Button
            {
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(KuanDu, 25),
                Location = new Point(_owner.Width - KuanDu - 88, 5),
                Cursor = Cursors.Hand
            };
            _btnPrivacy.FlatAppearance.BorderSize = 0;
            _btnPrivacy.Click += (s, e) => PrivacyToggled?.Invoke();
            SetPrivacyState(false);

            _titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(_owner.Handle, 0xA1, 0x2, 0);
                }
            };

            _titleBar.Controls.Add(titleLabel);
            _titleBar.Controls.Add(btnClose);
            _titleBar.Controls.Add(btnMinimize);
            _titleBar.Controls.Add(_btnPrivacy);

            _owner.Resize += (s, e) =>
            {
                btnClose.Location = new Point(_owner.Width - 40, 0);
                btnMinimize.Location = new Point(_owner.Width - 80, 0);
                _btnPrivacy.Location = new Point(_owner.Width - KuanDu - 88, 5);
            };
        }

        /// <summary>隐私按钮宽度：要放得下「🔒 隐私模式：开/关」这行字</summary>
        private const int KuanDu = 124;

        /// <summary>隐私模式按钮被点击（由 MainForm 接管状态）</summary>
        public event Action PrivacyToggled;

        /// <summary>
        /// 按当前状态刷新按钮外观：直接显示「隐私模式：开/关」，开启时用醒目底色，
        /// 这样不用点开也知道当前处于哪种状态。
        /// </summary>
        public void SetPrivacyState(bool enabled)
        {
            _btnPrivacy.Text = enabled ? "🔓 隐私模式：开" : "🔒 隐私模式：关";
            _btnPrivacy.BackColor = enabled ? Color.FromArgb(255, 196, 0) : Color.Transparent;
            _btnPrivacy.ForeColor = enabled ? Color.FromArgb(60, 40, 0) : ThemeManager.TitleBtn;
            _btnPrivacy.Font = new Font("Microsoft YaHei", 9, enabled ? FontStyle.Bold : FontStyle.Regular);
        }

        public Panel Build()
        {
            return _titleBar;
        }

        /// <summary>
        /// 应用明暗主题到标题栏
        /// </summary>
        public void ApplyTheme(bool isDark)
        {
            _titleBar.BackColor = ThemeManager.TitleBar;

            // 遍历标题栏内所有子控件
            foreach (Control ctrl in _titleBar.Controls)
            {
                if (ctrl is Label label)
                {
                    label.ForeColor = ThemeManager.TitleBtn;
                }
                else if (ctrl is Button btn)
                {
                    btn.ForeColor = ThemeManager.TitleBtn;
                    btn.BackColor = Color.Transparent;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}
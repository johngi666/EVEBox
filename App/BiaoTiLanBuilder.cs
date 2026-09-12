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
    public class BiaoTiLanBuilder
    {
        private readonly Form _owner;
        private readonly Panel _titleBar;

        public BiaoTiLanBuilder(Form owner)
        {
            _owner = owner;

            _titleBar = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(70, 130, 180),
                Margin = new Padding(0)
            };

            // 标题（带版本号）
            Label titleLabel = new Label
            {
                Text = $"EVE BOX {YingYongXinXi.Version}",
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

            _owner.Resize += (s, e) =>
            {
                btnClose.Location = new Point(_owner.Width - 40, 0);
                btnMinimize.Location = new Point(_owner.Width - 80, 0);
            };
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
            _titleBar.BackColor = ZhuTiManager.TitleBar;

            // 遍历标题栏内所有子控件
            foreach (Control ctrl in _titleBar.Controls)
            {
                if (ctrl is Label label)
                {
                    label.ForeColor = ZhuTiManager.TitleBtn;
                }
                else if (ctrl is Button btn)
                {
                    btn.ForeColor = ZhuTiManager.TitleBtn;
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
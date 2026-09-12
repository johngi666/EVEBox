using EVEBox.GongYong;
using System;
using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.GengXin
{
    /// <summary>
    /// 更新下载进度弹窗
    /// </summary>
    public class XiaZaiJinDuDialog : JiChuDialog
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _lblPercent;
        private readonly Label _lblStatus;
        private readonly Button _btnCancel;
        private bool _isCancelled = false;

        public bool IsCancelled => _isCancelled;

        public XiaZaiJinDuDialog(string version)
        {
            this.Text = "下载更新";
            this.Size = new Size(420, 190);
            this.BackColor = Color.White;

            Label lblTitle = new Label
            {
                Text = $"正在下载 {version} ...",
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Location = new Point(20, 50),
                AutoSize = true
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 85),
                Size = new Size(370, 22),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            _lblPercent = new Label
            {
                Text = "0%",
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Location = new Point(360, 87),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblStatus = new Label
            {
                Text = "连接服务器...",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                Location = new Point(20, 115),
                AutoSize = true
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(310, 145),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(150, 150, 150),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) =>
            {
                _isCancelled = true;
                _btnCancel.Enabled = false;
                _btnCancel.Text = "取消中...";
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(_progressBar);
            this.Controls.Add(_lblPercent);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_btnCancel);
        }

        /// <summary>
        /// 更新进度（可跨线程调用）
        /// </summary>
        public void UpdateProgress(int percent, string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(percent, status)));
                return;
            }

            _progressBar.Value = Math.Max(0, Math.Min(100, percent));
            _lblPercent.Text = $"{percent}%";
            if (!string.IsNullOrEmpty(status))
                _lblStatus.Text = status;
            Application.DoEvents();
        }

        /// <summary>
        /// 标记失败
        /// </summary>
        public void SetFailed(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetFailed(message)));
                return;
            }

            _lblStatus.Text = $"❌ {message}";
            _lblStatus.ForeColor = Color.Red;
            _btnCancel.Text = "关闭";
            Application.DoEvents();
        }
    }
}

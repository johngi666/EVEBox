using EVEBox.Dialogs.Common;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVEBox.Dialogs.Progress
{
    public partial class SearchProgressForm : BaseDialog
    {
        private ProgressBar progressBar;
        private Label lblStatus;
        private bool _isCancelled = false;

        public bool IsCancelled => _isCancelled;

        public SearchProgressForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "深度搜索";
            this.Size = new Size(400, 150);
            this.BackColor = Color.White;
            this.TopMost = false;
            this.ShowInTaskbar = false;

            // 状态标签
            lblStatus = new Label
            {
                Text = "正在搜索设置文件夹...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(51, 51, 51),
                AutoSize = false,
                Size = new Size(360, 25),
                Location = new Point(20, 55),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 进度条
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Location = new Point(20, 85),
                Size = new Size(360, 20)
            };

            // 取消按钮
            Button btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(310, 115),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { _isCancelled = true; this.Close(); };

            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnCancel);

            this.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(this.Width - 90, this.Height - 45);
                progressBar.Size = new Size(this.Width - 40, 20);
                lblStatus.Size = new Size(this.Width - 40, 25);
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 确保在 Owner 设置后重新计算位置
            if (Owner != null)
            {
                this.Location = new Point(
                    Owner.Location.X + (Owner.Width - this.Width) / 2,
                    Owner.Location.Y + (Owner.Height - this.Height) / 2
                );
            }
        }

        public void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => lblStatus.Text = status));
            }
            else
            {
                lblStatus.Text = status;
            }
            Application.DoEvents();
        }

        public void CloseForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.Close()));
            }
            else
            {
                this.Close();
            }
        }

        protected override void OnCloseClicked()
        {
            _isCancelled = true;
            this.Close();
        }

    }
}
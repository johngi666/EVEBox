using EVEBox.Dialogs.Common;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVEBox.Dialogs.Progress
{
    /// <summary>
    /// 同步进度对话框
    /// </summary>
    public partial class SyncProgressDialog : BaseDialog
    {
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblDetail;
        private Button btnCancel;
        private Button btnClose;

        private CancellationTokenSource _cts;
        private bool _isCompleted = false;
        private bool _isCancelled = false;
        private int _totalSteps;
        private int _currentStep;

        public bool IsCancelled => _isCancelled;
        public bool IsCompleted => _isCompleted;

        public SyncProgressDialog(string title = "同步进度", int totalSteps = 100)
        {
            _totalSteps = totalSteps;
            _cts = new CancellationTokenSource();
            InitializeComponent(title);
        }

        private void InitializeComponent(string title)
        {
            this.Text = title;
            this.Size = new Size(450, 180);
            this.BackColor = Color.White;
            this.TopMost = false;
            this.ShowInTaskbar = false;

            // 状态标签
            lblStatus = new Label
            {
                Text = "准备开始...",
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 51),
                AutoSize = false,
                Size = new Size(410, 25),
                Location = new Point(20, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 详情标签
            lblDetail = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(410, 20),
                Location = new Point(20, 75),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 进度条
            progressBar = new ProgressBar
            {
                Location = new Point(20, 100),
                Size = new Size(410, 22),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            // 底部按钮
            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(350, 135),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                var result = CustomMessageBox.Show(
                    "确定要取消同步吗？",
                    "确认取消",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    _cts?.Cancel();
                    _isCancelled = true;
                    btnCancel.Enabled = false;
                    btnCancel.Text = "取消中...";
                }
            };

            btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(80, 30),
                Location = new Point(350, 135),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblStatus);
            this.Controls.Add(lblDetail);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnClose);

            this.Resize += (s, e) =>
            {
                progressBar.Size = new Size(this.Width - 40, 22);
                lblStatus.Size = new Size(this.Width - 40, 25);
                lblDetail.Size = new Size(this.Width - 40, 20);
                btnCancel.Location = new Point(this.Width - 100, this.Height - 45);
                btnClose.Location = new Point(this.Width - 100, this.Height - 45);
            };
        }

        /// <summary>
        /// 获取取消令牌
        /// </summary>
        public CancellationToken GetCancellationToken()
        {
            return _cts.Token;
        }

        protected override void OnCloseClicked()
        {
            if (!_isCompleted)
            {
                var result = CustomMessageBox.Show(
                    "同步正在进行中，确定要取消吗？",
                    "确认取消",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    _cts?.Cancel();
                    _isCancelled = true;
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public void UpdateProgress(int percent, string status, string detail = "")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(percent, status, detail)));
                return;
            }

            progressBar.Value = Math.Min(100, Math.Max(0, percent));
            lblStatus.Text = status;
            lblDetail.Text = detail;
            Application.DoEvents();
        }

        /// <summary>
        /// 更新进度（自动计算百分比）
        /// </summary>
        public void UpdateProgressStep(string status, string detail = "")
        {
            _currentStep++;
            int percent = _totalSteps > 0 ? (_currentStep * 100 / _totalSteps) : 0;
            UpdateProgress(percent, status, detail);
        }

        /// <summary>
        /// 标记为完成
        /// </summary>
        public void SetCompleted(string message = "同步完成！")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetCompleted(message)));
                return;
            }

            _isCompleted = true;
            progressBar.Value = 100;
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.Green;
            btnCancel.Visible = false;
            btnClose.Visible = true;
            Application.DoEvents();
        }

        /// <summary>
        /// 标记为失败
        /// </summary>
        public void SetFailed(string errorMessage)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetFailed(errorMessage)));
                return;
            }

            _isCompleted = true;
            progressBar.Value = 0;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = $"❌ {errorMessage}";
            lblStatus.ForeColor = Color.Red;
            btnCancel.Visible = false;
            btnClose.Visible = true;
            Application.DoEvents();
        }

        /// <summary>
        /// 显示取消状态
        /// </summary>
        public void SetCancelled()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SetCancelled));
                return;
            }

            _isCancelled = true;
            _isCompleted = true;
            lblStatus.Text = "❌ 同步已取消";
            lblStatus.ForeColor = Color.Orange;
            progressBar.Value = 0;
            btnCancel.Visible = false;
            btnClose.Visible = true;
            Application.DoEvents();
        }

        /// <summary>
        /// 设置总步数
        /// </summary>
        public void SetTotalSteps(int total)
        {
            _totalSteps = total;
            _currentStep = 0;
        }


        /// <summary>
        /// 创建并运行同步任务（便捷方法）
        /// </summary>
        public static async Task<bool> RunAsync(
            IWin32Window owner,
            string title,
            Func<IProgress<(int Percent, string Status, string Detail)>, CancellationToken, Task> work,
            Action onCancelled = null)
        {
            using var dialog = new SyncProgressDialog(title);
            dialog.Show(owner);
            Application.DoEvents();

            try
            {
                var progress = new Progress<(int Percent, string Status, string Detail)>(p =>
                    dialog.UpdateProgress(p.Percent, p.Status, p.Detail));

                await work(progress, dialog.GetCancellationToken());

                if (dialog.IsCancelled)
                {
                    dialog.SetCancelled();
                    onCancelled?.Invoke();
                    return false;
                }

                dialog.SetCompleted();
                return true;
            }
            catch (OperationCanceledException)
            {
                dialog.SetCancelled();
                onCancelled?.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                dialog.SetFailed(ex.Message);
                return false;
            }
            finally
            {
                // 等待用户点击关闭
                while (!dialog.IsDisposed && !dialog.btnClose.Visible)
                {
                    await Task.Delay(100);
                }
                // 对话框保持打开，让用户查看结果后手动关闭
            }
        }
    }
}
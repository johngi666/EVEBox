using System;
using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.GongYong
{
    public partial class SouSuoShiBaiDialog : JiChuDialog
    {
        public enum YongHuXuanZe
        {
            SwitchServer,   // 切换服务器
            DeepSearch,     // 深度查找
            ManualSelect,   // 手动设置
            Cancel          // 取消
        }

        private YongHuXuanZe _result = YongHuXuanZe.Cancel;
        private string _serverName;

        public SouSuoShiBaiDialog(string serverName)
        {
            _serverName = serverName;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "提示";
            this.Size = new Size(380, 220);
            this.BackColor = Color.White;
            this.TopMost = false;
            this.ShowInTaskbar = false;

            // 提示消息
            Label lblMessage = new Label
            {
                Text = $"未找到【{_serverName}】\n的可用设置文件夹",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(51, 51, 51),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(340, 50),
                Location = new Point(20, 55)
            };

            // 按钮面板
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(30, 125),
                Size = new Size(320, 40),
                WrapContents = false
            };

            Button btnSwitchServer = CreateButton("切换服务器", Color.FromArgb(70, 130, 180));
            btnSwitchServer.Click += (s, e) => { _result = YongHuXuanZe.SwitchServer; this.Close(); };

            Button btnDeepSearch = CreateButton("深度查找", Color.FromArgb(70, 130, 180));
            btnDeepSearch.Click += (s, e) => { _result = YongHuXuanZe.DeepSearch; this.Close(); };

            Button btnManualSelect = CreateButton("手动设置", Color.FromArgb(70, 130, 180));
            btnManualSelect.Click += (s, e) => { _result = YongHuXuanZe.ManualSelect; this.Close(); };

            Button btnCancel = CreateButton("取消", Color.FromArgb(100, 100, 100));
            btnCancel.Click += (s, e) => { _result = YongHuXuanZe.Cancel; this.Close(); };

            buttonPanel.Controls.Add(btnSwitchServer);
            buttonPanel.Controls.Add(btnDeepSearch);
            buttonPanel.Controls.Add(btnManualSelect);
            buttonPanel.Controls.Add(btnCancel);

            this.Controls.Add(lblMessage);
            this.Controls.Add(buttonPanel);

            this.Resize += (s, e) =>
            {
                buttonPanel.Location = new Point(30, this.Height - 85);
            };
        }

        private Button CreateButton(string text, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(77, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public YongHuXuanZe ShowDialogAndGetResult()
        {
            base.ShowDialog();
            return _result;
        }

        protected override void OnCloseClicked()
        {
            _result = YongHuXuanZe.Cancel;
            this.Close();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}
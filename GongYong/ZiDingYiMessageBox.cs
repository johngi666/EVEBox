using System;
using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.GongYong
{
    public class ZiDingYiMessageBox : JiChuDialog
    {
        private Label lblMessage;
        private Button btnOK;
        private Button btnYes;
        private Button btnNo;
        private DialogResult result = DialogResult.None;

        public ZiDingYiMessageBox(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            InitializeComponent();
            this.Text = title;
            lblMessage.Text = message;
            SetupButtons(buttons);
            SetupIcon(icon);
            AdjustHeight();
        }

        private void InitializeComponent()
        {
            this.MinimumSize = new Size(300, 150);
            this.MaximumSize = new Size(500, 500);
            this.BackColor = Color.White;

            // 消息内容
            lblMessage = new Label
            {
                Location = new Point(20, 55),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.FromArgb(51, 51, 51),
                MaximumSize = new Size(360, 0)
            };

            // 按钮容器
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(0, 100),
                Size = new Size(this.Width - 20, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            this.Controls.Add(lblMessage);
            this.Controls.Add(buttonPanel);

            this.Resize += (s, e) =>
            {
                buttonPanel.Location = new Point(10, this.Height - 45);
            };
        }

        private void AdjustHeight()
        {
            // 计算消息内容所需高度
            int messageHeight = lblMessage.GetPreferredSize(new Size(360, 0)).Height;

            // 计算窗口总高度 = 标题栏(35) + 消息内边距(55+10) + 按钮区域(45) + 消息内容高度
            int newHeight = 35 + messageHeight + 30 + 45;

            // 限制最小和最大高度
            newHeight = Math.Max(150, Math.Min(450, newHeight));

            this.Height = newHeight;

            // 更新按钮面板位置
            FlowLayoutPanel buttonPanel = this.Controls[2] as FlowLayoutPanel;
            if (buttonPanel != null)
            {
                buttonPanel.Location = new Point(10, this.Height - 45);
            }
        }

        private void SetupButtons(MessageBoxButtons buttons)
        {
            FlowLayoutPanel buttonPanel = this.Controls[2] as FlowLayoutPanel;
            buttonPanel.Controls.Clear();

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    btnOK = CreateButton("确定", DialogResult.OK);
                    buttonPanel.Controls.Add(btnOK);
                    break;
                case MessageBoxButtons.OKCancel:
                    btnOK = CreateButton("确定", DialogResult.OK);
                    Button btnCancel = CreateButton("取消", DialogResult.Cancel);
                    buttonPanel.Controls.Add(btnOK);
                    buttonPanel.Controls.Add(btnCancel);
                    break;
                case MessageBoxButtons.YesNo:
                    btnYes = CreateButton("是", DialogResult.Yes);
                    btnNo = CreateButton("否", DialogResult.No);
                    buttonPanel.Controls.Add(btnYes);
                    buttonPanel.Controls.Add(btnNo);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    btnYes = CreateButton("是", DialogResult.Yes);
                    btnNo = CreateButton("否", DialogResult.No);
                    Button btnCancel2 = CreateButton("取消", DialogResult.Cancel);
                    buttonPanel.Controls.Add(btnYes);
                    buttonPanel.Controls.Add(btnNo);
                    buttonPanel.Controls.Add(btnCancel2);
                    break;
            }
        }

        private Button CreateButton(string text, DialogResult dialogResult)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => { result = dialogResult; this.Close(); };
            return btn;
        }

        private void SetupIcon(MessageBoxIcon icon)
        {
            // 可选：添加图标显示
            switch (icon)
            {
                case MessageBoxIcon.Information:
                    break;
                case MessageBoxIcon.Warning:
                    break;
                case MessageBoxIcon.Error:
                    break;
                case MessageBoxIcon.Question:
                    break;
            }
        }

        public new DialogResult ShowDialog()
        {
            base.ShowDialog();
            return result;
        }

        protected override void OnCloseClicked()
        {
            result = DialogResult.Cancel;
            this.Close();
        }

        public static DialogResult Show(string message, string title = "提示", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            using (var msgBox = new ZiDingYiMessageBox(message, title, buttons, icon))
            {
                return msgBox.ShowDialog();
            }
        }
    }
}
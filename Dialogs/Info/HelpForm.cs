using EVEBox.Dialogs.Common;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVEBox.Dialogs.Info
{
    public partial class HelpForm : BaseDialog
    {
        public HelpForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "使用说明";
            this.Size = new Size(490, 588);
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.White;
            this.TopMost = false;
            this.ShowInTaskbar = false;

            // 内容区域
            RichTextBox rtb = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(470, 370),
                ReadOnly = true,
                Font = new Font("Microsoft YaHei", 9),
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.None
            };
            rtb.Text = HelpText.Content;   // 引用 HelpText.cs 中的内容

            Button btnCloseBottom = new Button
            {
                Text = "关闭",
                Size = new Size(80, 30),
                Location = new Point(400, 420),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseBottom.FlatAppearance.BorderSize = 0;
            btnCloseBottom.Click += (s, e) => this.Close();

            this.Controls.Add(rtb);
            this.Controls.Add(btnCloseBottom);

            this.Resize += (s, e) =>
            {
                rtb.Size = new Size(this.Width - 20, this.Height - 100);
                btnCloseBottom.Location = new Point(this.Width - 90, this.Height - 45);
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            if (Owner != null)
            {
                this.Location = new Point(Owner.Location.X - this.Width, Owner.Location.Y);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (Owner != null)
            {
                Owner.LocationChanged += Owner_LocationChanged;
            }
        }

        private void Owner_LocationChanged(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (Owner != null)
            {
                Owner.LocationChanged -= Owner_LocationChanged;
            }
            base.OnFormClosed(e);
        }

    }
}
using EVEBox.App;
using EVEBox.GongYong;
using System;
using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.GengXin
{
    /// <summary>
    /// 新版本通知弹窗
    /// 点击「立即更新」返回 DialogResult.OK，由调用方执行下载安装
    /// </summary>
    public class GengXinDialog : JiChuDialog
    {
        public GengXinDialog(string newVersion, string releaseNotes, string downloadUrl)
        {
            this.Text = "发现新版本";
            this.Size = new Size(420, 320);
            this.BackColor = Color.White;

            Label lblIcon = new Label
            {
                Text = "🎉",
                Font = new Font("Segoe UI", 32),
                Location = new Point(20, 55),
                AutoSize = true
            };

            Label lblTitle = new Label
            {
                Text = $"发现新版本：{newVersion}",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Location = new Point(70, 60),
                AutoSize = true
            };

            Label lblCurrent = new Label
            {
                Text = $"当前版本：{YingYongXinXi.Version}",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                Location = new Point(70, 88),
                AutoSize = true
            };

            TextBox txtNotes = new TextBox
            {
                Text = string.IsNullOrWhiteSpace(releaseNotes) ? "暂无更新说明" : releaseNotes,
                Font = new Font("Microsoft YaHei", 9),
                Location = new Point(20, 120),
                Size = new Size(370, 100),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };

            Button btnDownload = new Button
            {
                Text = "立即更新",
                Size = new Size(110, 35),
                Location = new Point(150, 235),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += (s, e) =>
            {
                DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnLater = new Button
            {
                Text = "稍后提醒",
                Size = new Size(110, 35),
                Location = new Point(270, 235),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 180, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Cursor = Cursors.Hand
            };
            btnLater.FlatAppearance.BorderSize = 0;
            btnLater.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(lblIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblCurrent);
            this.Controls.Add(txtNotes);
            this.Controls.Add(btnDownload);
            this.Controls.Add(btnLater);
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVESyncTool.Core.UI
{
    /// <summary>
    /// 左侧面板构建器
    /// </summary>
    public class LeftPanelBuilder
    {
        private readonly Panel _panel;
        private readonly ComboBox _cmbServer;
        private readonly Button _btnOpenFolder;
        private readonly Button _btnLoadDefault;
        private readonly Button _btnSelectFolder;
        private readonly Button _btnVersionManage;
        private readonly Button _btnBackup;
        private readonly Button _btnDeleteAllBackups;
        private readonly Button _btnSync;
        private readonly Button _btnShipTag;

        // 服务器状态标签（供外部访问）
        private readonly Label _lblInfinityStatus;
        private readonly Label _lblSerenityStatus;
        private readonly Label _lblTranquilityStatus;

        public ComboBox CmbServer => _cmbServer;
        public Button BtnOpenFolder => _btnOpenFolder;
        public Button BtnLoadDefault => _btnLoadDefault;
        public Button BtnSelectFolder => _btnSelectFolder;
        public Button BtnVersionManage => _btnVersionManage;
        public Button BtnBackup => _btnBackup;
        public Button BtnDeleteAllBackups => _btnDeleteAllBackups;
        public Button BtnSync => _btnSync;
        public Button BtnShipTag => _btnShipTag;
        public Label LblInfinityStatus => _lblInfinityStatus;
        public Label LblSerenityStatus => _lblSerenityStatus;
        public Label LblTranquilityStatus => _lblTranquilityStatus;

        public LeftPanelBuilder()
        {
            _panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 10),
                BackColor = Color.White
            };

            int y = 40;

            // ===== 服务器切换 =====
            Label lblServerTitle = new Label
            {
                Text = "服务器切换",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(15, y)
            };
            _panel.Controls.Add(lblServerTitle);

            _cmbServer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, y + 30),
                Size = new Size(190, 30),
                Font = new Font("Microsoft YaHei", 10),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbServer.Items.AddRange(new object[] { "曙光服 (Infinity)", "晨曦服 (Serenity)", "国际服 (Tranquility)" });
            _panel.Controls.Add(_cmbServer);

            y += 75;

            Panel separator1 = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(190, 1),
                BackColor = Color.FromArgb(200, 200, 200)
            };
            _panel.Controls.Add(separator1);

            y += 10;

            // ===== 配置文件夹 =====
            _btnOpenFolder = new Button
            {
                Text = "未识别到可用配置",
                Location = new Point(15, y),
                Size = new Size(190, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 248, 255),
                ForeColor = Color.FromArgb(70, 130, 180),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(70, 130, 180);
            _panel.Controls.Add(_btnOpenFolder);

            y += 40;

            _btnLoadDefault = new Button
            {
                Text = "📂 默认配置路径",
                Location = new Point(15, y),
                Size = new Size(190, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnLoadDefault.FlatAppearance.BorderSize = 0;
            _panel.Controls.Add(_btnLoadDefault);

            y += 40;

            _btnSelectFolder = new Button
            {
                Text = "📁 手动选择文件夹",
                Location = new Point(15, y),
                Size = new Size(190, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSelectFolder.FlatAppearance.BorderSize = 0;
            _panel.Controls.Add(_btnSelectFolder);

            y += 45;

            Panel separator2 = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(190, 1),
                BackColor = Color.FromArgb(200, 200, 200)
            };
            _panel.Controls.Add(separator2);

            y += 10;

            // ===== 配置方案管理 =====
            _btnVersionManage = new Button
            {
                Text = "📂 配置方案管理",
                Location = new Point(15, y),
                Size = new Size(190, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnVersionManage.FlatAppearance.BorderSize = 0;
            _panel.Controls.Add(_btnVersionManage);

            y += 40;

            _btnBackup = CreateStyledButton("💾 备份当前配置", 15, y, 190, Color.FromArgb(50, 205, 50));
            _panel.Controls.Add(_btnBackup);

            y += 40;

            _btnDeleteAllBackups = CreateStyledButton("🗑️ 删除所有备份", 15, y, 190, Color.FromArgb(255, 69, 0));
            _panel.Controls.Add(_btnDeleteAllBackups);

            y += 40;

            _btnSync = CreateStyledButton("▶️ 快捷覆盖", 15, y, 190, Color.FromArgb(70, 130, 180));
            _panel.Controls.Add(_btnSync);

            y += 50;

            Panel separator3 = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(190, 1),
                BackColor = Color.FromArgb(200, 200, 200)
            };
            _panel.Controls.Add(separator3);

            // 分割线与标题间距（收紧10px后再上移5px）；标题与人数行保持宽松间距
            y += 5;

            // ===== 服务器状态 =====
            Label lblServerStatusTitle = new Label
            {
                Text = "📡 服务器状态",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(15, y)
            };
            _panel.Controls.Add(lblServerStatusTitle);

            // 标题与人数显示锚定（人数区上移5px，标题不动）
            y += 23;

            _lblInfinityStatus = new Label
            {
                Text = "曙光服: 查询中...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(_lblInfinityStatus);

            y += 20;

            _lblSerenityStatus = new Label
            {
                Text = "晨曦服: 查询中...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(_lblSerenityStatus);

            y += 20;

            _lblTranquilityStatus = new Label
            {
                Text = "国际服: 查询中...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(_lblTranquilityStatus);

            // 按钮前间距（+5px 补偿人数上移，保证按钮位置不变）
            y += 24;

            // ===== 全局舰船标签显示开关 =====
            _btnShipTag = CreateStyledButton("🛰️ 全局舰船标签显示:关闭", 15, y, 190, Color.FromArgb(70, 130, 180));
            _panel.Controls.Add(_btnShipTag);

            y += 45;

            Panel separator4 = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(190, 1),
                BackColor = Color.FromArgb(200, 200, 200)
            };
            _panel.Controls.Add(separator4);
        }

        private Button CreateStyledButton(string text, int x, int y, int width, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public Panel Build()
        {
            return _panel;
        }
    }
}
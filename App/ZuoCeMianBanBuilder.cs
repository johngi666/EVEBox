using EVEBox.App;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.App
{
    /// <summary>
    /// 左侧面板：标签导航 + 底部固定服务器状态区
    /// 业务内容全部由右侧标签内容区承载，切换标签时触发 TabSelected 事件
    /// </summary>
    public class ZuoCeMianBanBuilder
    {
        // 标签常量（供 ZhuChuangTi 引用）
        public const int TabSync = 0;      // 配置同步
        public const int TabBackup = 1;    // 备份管理
        public const int TabScheme = 2;    // 配置方案
        public const int TabTools = 3;     // 工具
        public const int TabHelp = 4;      // 使用说明
        public const int TabLog = 5;       // 操作日志
        public const int TabUpdate = 6;    // 更新

        public static readonly string[] TabNames =
        {
            "配置同步", "备份管理", "配置方案管理", "其他工具", "使用说明", "操作日志", "更新"
        };

        private readonly Panel _panel;
        private readonly List<Button> _tabButtons = new List<Button>();
        private int _selectedTab = 0;

        // 服务器状态标签（供外部访问）
        private readonly Label _lblInfinityStatus;
        private readonly Label _lblSerenityStatus;
        private readonly Label _lblTranquilityStatus;

        // 主题切换按钮（原位于标题栏，现移入左栏服务器状态分隔线上方）
        private readonly Button _btnTheme;

        // 舰船标签开关按钮（位于主题按钮上方）
        private readonly Button _btnShipTag;

        /// <summary>
        /// 标签切换事件（参数为标签索引）
        /// </summary>
        public event Action<int> TabSelected;

        public Label LblInfinityStatus => _lblInfinityStatus;
        public Label LblSerenityStatus => _lblSerenityStatus;
        public Label LblTranquilityStatus => _lblTranquilityStatus;

        public Button BtnTheme => _btnTheme;
        public Button BtnShipTag => _btnShipTag;

        public ZuoCeMianBanBuilder()
        {
            _panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 10),
                BackColor = Color.White
            };

            // ===== 标签导航（7 个，居中显示） =====
            int y = 20;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                Button btn = new Button
                {
                    Text = TabNames[i],
                    Location = new Point(20, y),
                    Size = new Size(140, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(70, 130, 180),
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(70, 130, 180);
                btn.Click += (s, e) =>
                {
                    SelectTab(index);
                    TabSelected?.Invoke(index);
                };
                _tabButtons.Add(btn);
                _panel.Controls.Add(btn);

                y += 46;
            }

            // ===== 舰船标签开关按钮（位于主题按钮上方） =====
            _btnShipTag = new Button
            {
                Text = "舰船标签:关闭",
                Location = new Point(20, y),
                Size = new Size(140, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnShipTag.FlatAppearance.BorderSize = 0;
            _panel.Controls.Add(_btnShipTag);

            // ===== 主题切换按钮（位于服务器状态上方分隔线的上方） =====
            _btnTheme = new Button
            {
                Text = "🌙夜间模式",
                Location = new Point(20, y + 44),
                Size = new Size(140, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnTheme.FlatAppearance.BorderSize = 0;
            _panel.Controls.Add(_btnTheme);

            Panel separator1 = new Panel
            {
                Location = new Point(20, y+90),
                Size = new Size(140, 2),
                BackColor = Color.FromArgb(200, 200, 200)
            };
            _panel.Controls.Add(separator1);

            y += 98;

            // ===== 底部固定：服务器状态 =====
            Label lblServerStatusTitle = new Label
            {
                Text = "服务器状态",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(lblServerStatusTitle);

            y += 28;

            _lblInfinityStatus = new Label
            {
                Text = "曙光服: 查询中...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(_lblInfinityStatus);

            y += 22;

            _lblSerenityStatus = new Label
            {
                Text = "晨曦服: 查询中...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(_lblSerenityStatus);

            y += 22;

            _lblTranquilityStatus = new Label
            {
                Text = "国际服: 查询中...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, y)
            };
            _panel.Controls.Add(_lblTranquilityStatus);

            // ===== 在线人数下方的分隔线（与标签区同款） =====
            y += 24;

            Panel separator2 = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(140, 2),
                BackColor = Color.FromArgb(200, 200, 200)
            };
            _panel.Controls.Add(separator2);

            // 默认选中"配置同步"
            SelectTab(TabSync);
        }

        /// <summary>
        /// 切换选中标签并刷新高亮样式
        /// </summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= _tabButtons.Count) return;
            _selectedTab = index;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].BackColor = i == index ? Color.FromArgb(70, 130, 180) : Color.White;
                _tabButtons[i].ForeColor = i == index ? Color.White : Color.FromArgb(70, 130, 180);
            }
        }

        public Panel Build()
        {
            return _panel;
        }
    }
}

using EVEBox.Common.GongYong;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;



namespace EVEBox.Features.PeiZhiTongBu
{
    public class SheZhiXuanZeDialog : JiChuDialog
    {
        // ===== 私有字段（所有名称与 TongBuDialog 不同） =====
        private readonly string _srcName;
        private readonly string _dstFolder;
        private readonly List<string> _savedSettings;

        private Dictionary<string, CheckBox> _settingCheckboxes = new Dictionary<string, CheckBox>();

        private Button _okButton;
        private Button _cancelButton;
        private Button _checkAllButton;
        private Button _uncheckAllButton;

        private FlowLayoutPanel _settingsPanel;
        private Label _infoLabel;
        private Label _targetInfoLabel;
        private Label _hintLabel;
        private Panel _scrollView;

        private ToolTip _fieldTooltip = new ToolTip();

        public List<string> PickedSettings { get; private set; } = new List<string>();

        // ===== 构造函数 =====
        public SheZhiXuanZeDialog(string sourceName, string targetFolder)
            : this(sourceName, targetFolder, null)
        {
        }

        public SheZhiXuanZeDialog(
            string sourceName,
            string targetFolder,
            List<string> savedSelections)
        {
            _srcName = sourceName;
            _dstFolder = targetFolder;
            _savedSettings = savedSelections ?? new List<string>();

            BuildForm();
            LoadSettingCategories();
        }

        // ===== 界面构建 =====
        private void BuildForm()
        {
            this.Text = "选择要覆盖的设置";
            this.Size = new Size(500, 580);
            this.MinimumSize = new Size(450, 480);
            this.BackColor = Color.White;

            // ---- 信息标签 ----
            _infoLabel = new Label
            {
                Text = $"来源: {_srcName} → 目标: {_dstFolder}",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(15, 48)
            };

            _targetInfoLabel = new Label
            {
                Text = "选择要覆盖的设置项",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 72)
            };

            // ---- 全选/全不选 ----
            _checkAllButton = new Button
            {
                Text = "全选",
                Size = new Size(70, 28),
                Location = new Point(15, 98),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _checkAllButton.FlatAppearance.BorderSize = 0;
            _checkAllButton.Click += (s, e) => ToggleAllCheckboxes(true);

            _uncheckAllButton = new Button
            {
                Text = "全不选",
                Size = new Size(70, 28),
                Location = new Point(90, 98),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _uncheckAllButton.FlatAppearance.BorderSize = 0;
            _uncheckAllButton.Click += (s, e) => ToggleAllCheckboxes(false);

            // ---- 滚动容器 ----
            _scrollView = new Panel
            {
                Location = new Point(15, 135),
                Size = new Size(this.Width - 30, this.Height - 205),
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            // ---- 设置面板 ----
            _settingsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Width = _scrollView.Width - 20,
                BackColor = Color.Transparent,
                Padding = new Padding(10),
                MinimumSize = new Size(10, 10)
            };
            _scrollView.Controls.Add(_settingsPanel);

            _scrollView.Resize += (s, e) =>
            {
                _settingsPanel.Width = _scrollView.Width - 20;
            };

            // ---- 底部按钮 ----
            _okButton = new Button
            {
                Text = "执行同步",
                Size = new Size(80, 35),
                Location = new Point(this.Width - 195, this.Height - 62),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 205, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _okButton.FlatAppearance.BorderSize = 0;
            _okButton.Click += ConfirmButton_Click;

            _cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(80, 35),
                Location = new Point(this.Width - 100, this.Height - 62),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; this.Close(); };

            _hintLabel = new Label
            {
                Text = "提示: 只勾选需要覆盖的设置项，未勾选的将保持不变",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, this.Height - 55)
            };

            // ---- 内容面板 ----
            Panel contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            contentArea.Controls.Add(_infoLabel);
            contentArea.Controls.Add(_targetInfoLabel);
            contentArea.Controls.Add(_checkAllButton);
            contentArea.Controls.Add(_uncheckAllButton);
            contentArea.Controls.Add(_scrollView);
            contentArea.Controls.Add(_okButton);
            contentArea.Controls.Add(_cancelButton);
            contentArea.Controls.Add(_hintLabel);

            this.Controls.Add(contentArea);

            // ---- 窗口自适应 ----
            this.Resize += (s, e) =>
            {
                _scrollView.Size = new Size(this.Width - 30, this.Height - 205);
                _okButton.Location = new Point(this.Width - 195, this.Height - 62);
                _cancelButton.Location = new Point(this.Width - 100, this.Height - 62);
                _hintLabel.Location = new Point(15, this.Height - 55);
                _settingsPanel.Width = _scrollView.Width - 20;
            };
        }

        // ===== 加载设置分类 =====
        private void LoadSettingCategories()
        {
            var categories = SheZhiYingShe.GetAllCategories();
            _settingCheckboxes.Clear();
            _settingsPanel.Controls.Clear();
            _settingsPanel.Height = 10;

            int totalHeight = 10;

            foreach (var category in categories)
            {
                Label categoryLabel = new Label
                {
                    Text = $"── {category} ──",
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 130, 180),
                    AutoSize = true,
                    Margin = new Padding(0, 8, 0, 4)
                };
                _settingsPanel.Controls.Add(categoryLabel);
                totalHeight += 30;

                var mappings = SheZhiYingShe.GetAll().Where(m => m.Category == category).ToList();
                foreach (var mapping in mappings)
                {
                    CheckBox cb = new CheckBox
                    {
                        Text = mapping.DisplayName,
                        Font = new Font("Microsoft YaHei", 9),
                        AutoSize = true,
                        Checked = _savedSettings.Contains(mapping.DisplayName),
                        Margin = new Padding(20, 2, 0, 2),
                        Tag = mapping
                    };
                    if (!string.IsNullOrEmpty(mapping.Description))
                    {
                        cb.MouseHover += (s, e) => _fieldTooltip.SetToolTip(cb, mapping.Description);
                    }
                    _settingsPanel.Controls.Add(cb);
                    _settingCheckboxes[mapping.DisplayName] = cb;
                    totalHeight += 26;
                }
            }

            _settingsPanel.Height = Math.Max(totalHeight + 20, _scrollView.Height - 10);
        }

        // ===== 全选/全不选 =====
        private void ToggleAllCheckboxes(bool isChecked)
        {
            foreach (var cb in _settingCheckboxes.Values)
            {
                cb.Checked = isChecked;
            }
        }

        // ===== 确认按钮 =====
        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            PickedSettings.Clear();

            foreach (var kvp in _settingCheckboxes)
            {
                if (kvp.Value.Checked)
                {
                    PickedSettings.Add(kvp.Key);
                }
            }

            if (PickedSettings.Count == 0)
            {
                ZiDingYiMessageBox.Show("请至少选择一个要覆盖的设置项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void OnCloseClicked()
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
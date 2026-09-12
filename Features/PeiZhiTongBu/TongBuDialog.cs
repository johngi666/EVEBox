using EVEBox.Common.GongYong;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;



namespace EVEBox.Features.PeiZhiTongBu
{
    public class TongBuDialog : JiChuDialog
    {
        private readonly string _sourceName;
        private readonly string _targetFolder;
        private readonly List<WenJianMuBiaoXiang> _targets;
        private readonly Dictionary<string, string> _userRemarks;  // ← 新增：用户备注字典
        private Dictionary<string, CheckBox> _checkBoxes = new Dictionary<string, CheckBox>();
        private Button btnConfirm;
        private Button btnCancel;
        private Button btnSelectAll;
        private FlowLayoutPanel categoryPanel;
        private Label lblInfo;
        private Label lblTargetInfo;
        private Panel scrollContainer;

        public List<string> SelectedTargets { get; private set; } = new List<string>();

        // ===== 原有构造函数 =====
        public TongBuDialog(string sourceName, string targetFolder)
            : this(sourceName, targetFolder, null, null)
        {
        }

        public TongBuDialog(string sourceName, string targetFolder, List<WenJianMuBiaoXiang> targets)
            : this(sourceName, targetFolder, targets, null)
        {
        }

        // ===== 新增构造函数（带备注字典） =====
        public TongBuDialog(string sourceName, string targetFolder, List<WenJianMuBiaoXiang> targets, Dictionary<string, string> userRemarks)
        {
            _sourceName = sourceName;
            _targetFolder = targetFolder;
            _targets = targets ?? new List<WenJianMuBiaoXiang>();
            _userRemarks = userRemarks ?? new Dictionary<string, string>();

            InitializeComponent();
            if (_targets.Count > 0)
            {
                LoadTargets();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "选择同步目标";
            this.Size = new Size(400, 480);
            this.MinimumSize = new Size(400, 400);
            this.BackColor = Color.White;

            // ===== 信息标签 =====
            lblInfo = new Label
            {
                Text = $"来源: {_sourceName}",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(15, 48)
            };

            lblTargetInfo = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 72)
            };

            // ===== 全选按钮 =====
            btnSelectAll = new Button
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
            btnSelectAll.FlatAppearance.BorderSize = 0;
            btnSelectAll.Click += (s, e) => ToggleAllCheckBoxes();

            // ===== 滚动容器 =====
            scrollContainer = new Panel
            {
                Location = new Point(15, 135),
                Size = new Size(this.Width - 30, this.Height - 205),
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            categoryPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Width = scrollContainer.Width - 20,
                BackColor = Color.Transparent,
                Padding = new Padding(10),
                MinimumSize = new Size(10, 10)
            };
            scrollContainer.Controls.Add(categoryPanel);

            scrollContainer.Resize += (s, e) =>
            {
                categoryPanel.Width = scrollContainer.Width - 20;
            };

            // ===== 底部按钮 =====
            btnConfirm = new Button
            {
                Text = "确认同步",
                Size = new Size(80, 35),
                Location = new Point(this.Width - 195, this.Height - 55),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 205, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 35),
                Location = new Point(this.Width - 100, this.Height - 55),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; this.Close(); };

            // ===== 内容面板 =====
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            contentPanel.Controls.Add(lblInfo);
            contentPanel.Controls.Add(lblTargetInfo);
            contentPanel.Controls.Add(btnSelectAll);
            contentPanel.Controls.Add(scrollContainer);
            contentPanel.Controls.Add(btnConfirm);
            contentPanel.Controls.Add(btnCancel);

            this.Controls.Add(contentPanel);

            // ===== 窗口自适应 =====
            this.Resize += (s, e) =>
            {
                scrollContainer.Size = new Size(this.Width - 30, this.Height - 205);
                btnConfirm.Location = new Point(this.Width - 195, this.Height - 55);
                btnCancel.Location = new Point(this.Width - 100, this.Height - 55);
                categoryPanel.Width = scrollContainer.Width - 20;
            };
        }

        private void LoadTargets()
        {
            lblTargetInfo.Text = $"共 {_targets.Count} 个目标文件可供选择";

            categoryPanel.Controls.Clear();
            categoryPanel.Height = 10;

            int totalHeight = 10;

            foreach (var target in _targets)
            {
                // ===== 获取显示名称（备注优先） =====
                string displayText;
                if (!string.IsNullOrEmpty(target.UserId) && _userRemarks != null && _userRemarks.TryGetValue(target.UserId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                {
                    // 有备注：显示 "备注 (数字ID)"
                    displayText = $"{remark} ({target.UserId})";
                }
                else if (!string.IsNullOrEmpty(target.DisplayName))
                {
                    // 无备注但有DisplayName
                    displayText = target.DisplayName;
                }
                else
                {
                    // 兜底
                    displayText = Path.GetFileName(target.FileName);
                }

                CheckBox cb = new CheckBox
                {
                    Text = displayText,
                    Font = new Font("Microsoft YaHei", 9),
                    AutoSize = true,
                    Checked = true,
                    Margin = new Padding(20, 2, 0, 2),
                    Tag = target.FileName
                };
                categoryPanel.Controls.Add(cb);
                _checkBoxes[target.FileName] = cb;
                totalHeight += 26;
            }

            categoryPanel.Height = Math.Max(totalHeight + 20, scrollContainer.Height - 10);
        }

        private void ToggleAllCheckBoxes()
        {
            bool allChecked = true;
            foreach (var cb in _checkBoxes.Values)
            {
                if (!cb.Checked)
                {
                    allChecked = false;
                    break;
                }
            }

            bool newState = !allChecked;
            foreach (var cb in _checkBoxes.Values)
            {
                cb.Checked = newState;
            }

            btnSelectAll.Text = newState ? "全不选" : "全选";
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            SelectedTargets.Clear();

            foreach (var kvp in _checkBoxes)
            {
                if (kvp.Value.Checked)
                {
                    SelectedTargets.Add(kvp.Key);
                }
            }

            if (SelectedTargets.Count == 0)
            {
                ZiDingYiMessageBox.Show("请至少选择一个要同步的目标文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    public class WenJianMuBiaoXiang
    {
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public string UserId { get; set; }  // ← 新增：用户数字ID，用于匹配备注

        public WenJianMuBiaoXiang(string fileName, string displayName)
        {
            FileName = fileName;
            DisplayName = displayName;
            UserId = null;  // 默认为空
        }

        public WenJianMuBiaoXiang(string fileName, string displayName, string userId)
        {
            FileName = fileName;
            DisplayName = displayName;
            UserId = userId;
        }
    }
}
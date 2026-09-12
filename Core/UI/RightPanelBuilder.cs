using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EVEBox.Core.UI
{
    /// <summary>
    /// 右侧面板构建器（用户文件、备份管理、角色文件）
    /// 按标签页拆分为 partial：本文件为公共结构 + 构造函数，其余见各分部文件
    /// </summary>
    public partial class RightPanelBuilder
    {
        private readonly Panel _syncPanel;
        private readonly Panel _backupPanel;

        // 用户文件列表
        private DataGridView _dgvUserFiles;
        private Label _lblUserTitle;

        // 角色文件列表
        private DataGridView _dgvCharFiles;
        private Label _lblCharTitle;

        // 备份管理列表
        private DataGridView _dgvBackups;
        private Label _lblBackupTitle;

        // 用户备注相关
        private ToolTip _userToolTip = new ToolTip();
        private string _hoveredUserId = null;

        // 列头（供外部访问）
        public ColumnHeader ColUserId { get; private set; }
        public ColumnHeader ColUserTime { get; private set; }
        public ColumnHeader ColUserBackup { get; private set; }
        public ColumnHeader ColUserSync { get; private set; }

        public ColumnHeader ColCharName { get; private set; }
        public ColumnHeader ColCharId { get; private set; }
        public ColumnHeader ColCharTime { get; private set; }
        public ColumnHeader ColCharBackup { get; private set; }
        public ColumnHeader ColCharSync { get; private set; }

        public ColumnHeader ColBackupName { get; private set; }
        public ColumnHeader ColBackupTime { get; private set; }
        public ColumnHeader ColBackupShow { get; private set; }
        public ColumnHeader ColBackupRestore { get; private set; }
        public ColumnHeader ColBackupDelete { get; private set; }

        public DataGridView DgvUserFiles => _dgvUserFiles;
        public DataGridView DgvCharFiles => _dgvCharFiles;
        public DataGridView DgvBackups => _dgvBackups;
        public Label LblUserTitle => _lblUserTitle;
        public Label LblCharTitle => _lblCharTitle;
        public Label LblBackupTitle => _lblBackupTitle;

        // 用户备注相关事件
        public event EventHandler<UserRemarkEditEventArgs> UserRemarkEdited;

        public RightPanelBuilder()
        {
            // ===== 配置同步面板：用户文件（左） | 角色文件（右） =====
            Panel userPanel = CreateUserFilePanel();
            Panel charPanel = CreateCharFilePanel();

            _syncPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 10, 10, 10),
                BackColor = Color.White
            };

            TableLayoutPanel syncContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            syncContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            syncContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            syncContainer.Controls.Add(userPanel, 0, 0);
            syncContainer.Controls.Add(charPanel, 1, 0);

            _syncPanel.Controls.Add(syncContainer);

            // ===== 备份管理面板：备份表格 =====
            Panel backupPanel = CreateBackupPanel();

            _backupPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 10, 10, 10),
                BackColor = Color.White
            };
            _backupPanel.Controls.Add(backupPanel);

            // ★★★ 表格启用双缓冲：消除标签切换/数据刷新时的重绘闪烁动画 ★★★
            EnableDoubleBuffered(_dgvUserFiles);
            EnableDoubleBuffered(_dgvCharFiles);
            EnableDoubleBuffered(_dgvBackups);
        }

        /// <summary>
        /// 通过反射启用 DataGridView 双缓冲（DoubleBuffered 为受保护属性）
        /// </summary>
        private static void EnableDoubleBuffered(DataGridView grid)
        {
            if (grid == null) return;
            var prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(grid, true, null);
        }

        public Panel BuildSyncPanel()
        {
            return _syncPanel;
        }

        public Panel BuildBackupPanel()
        {
            return _backupPanel;
        }

        public Panel Build()
        {
            return _syncPanel;
        }
    }

    /// <summary>
    /// 用户备注编辑事件参数
    /// </summary>
    public class UserRemarkEditEventArgs : EventArgs
    {
        public string UserId { get; }
        public string Remark { get; }

        public UserRemarkEditEventArgs(string userId, string remark)
        {
            UserId = userId;
            Remark = remark;
        }
    }
}

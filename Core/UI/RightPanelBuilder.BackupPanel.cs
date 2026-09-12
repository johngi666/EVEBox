using System.Drawing;
using System.Windows.Forms;

namespace EVEBox.Core.UI
{
    /// <summary>
    /// 右侧面板构建器 - 备份管理面板（备份管理页）
    /// </summary>
    public partial class RightPanelBuilder
    {
        private Panel CreateBackupPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = Color.White
            };

            _lblBackupTitle = new Label
            {
                Text = "备份管理 (0个备份)",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _dgvBackups = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.Fixed3D,
                BackgroundColor = Color.FromArgb(248, 248, 248),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = new Font("Microsoft YaHei", 9),
                ScrollBars = ScrollBars.Vertical,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(240, 248, 255),
                    ForeColor = Color.Black,
                    SelectionBackColor = Color.FromArgb(240, 248, 255),
                    SelectionForeColor = Color.Black
                }
            };

            DataGridViewTextBoxColumn colBackupName = new DataGridViewTextBoxColumn
            {
                HeaderText = "备份名",
                Width = 240,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ReadOnly = true
            };
            DataGridViewTextBoxColumn colBackupTime = new DataGridViewTextBoxColumn
            {
                HeaderText = "时间",
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ReadOnly = true
            };
            DataGridViewButtonColumn colBackupShow = new DataGridViewButtonColumn
            {
                HeaderText = "显示",
                Width = 100,
                Text = "📂",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };
            DataGridViewButtonColumn colBackupRestore = new DataGridViewButtonColumn
            {
                HeaderText = "还原",
                Width = 100,
                Text = "↩️",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };
            DataGridViewButtonColumn colBackupDelete = new DataGridViewButtonColumn
            {
                HeaderText = "删除",
                Width = 100,
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };

            _dgvBackups.Columns.Add(colBackupName);
            _dgvBackups.Columns.Add(colBackupTime);
            _dgvBackups.Columns.Add(colBackupShow);
            _dgvBackups.Columns.Add(colBackupRestore);
            _dgvBackups.Columns.Add(colBackupDelete);

            // 标题移到顶部操作条居中（两按钮之间），表格直接填满下方
            panel.Controls.Add(_dgvBackups);

            return panel;
        }
    }
}

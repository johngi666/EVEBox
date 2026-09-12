using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.App
{
    /// <summary>
    /// 右侧面板构建器 - 角色配置文件面板（配置同步页右侧）
    /// </summary>
    public partial class YouMianBanBuilder
    {
        private Panel CreateCharFilePanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = Color.White
            };

            _lblCharTitle = new Label
            {
                Text = "角色配置文件 (0个文件)",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _dgvCharFiles = new DataGridView
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

            DataGridViewTextBoxColumn colCharName = new DataGridViewTextBoxColumn
            {
                HeaderText = "角色名",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ReadOnly = true
            };
            DataGridViewTextBoxColumn colCharId = new DataGridViewTextBoxColumn
            {
                HeaderText = "角色ID",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ReadOnly = true
            };
            DataGridViewTextBoxColumn colCharTime = new DataGridViewTextBoxColumn
            {
                HeaderText = "修改时间",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ReadOnly = true
            };
            DataGridViewButtonColumn colCharBackup = new DataGridViewButtonColumn
            {
                HeaderText = "备份",
                Width = 50,
                Text = "💾",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };
            DataGridViewButtonColumn colCharSync = new DataGridViewButtonColumn
            {
                HeaderText = "同步",
                Width = 50,
                Text = "📂",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };

            _dgvCharFiles.Columns.Add(colCharName);
            _dgvCharFiles.Columns.Add(colCharId);
            _dgvCharFiles.Columns.Add(colCharTime);
            _dgvCharFiles.Columns.Add(colCharBackup);
            _dgvCharFiles.Columns.Add(colCharSync);

            // ★★★ Dock 逆序：先放 Fill 表格，后放 Top 标题 ★★★
            panel.Controls.Add(_dgvCharFiles);
            panel.Controls.Add(_lblCharTitle);

            return panel;
        }
    }
}

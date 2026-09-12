using EVEBox.PeiZhiTongBu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;



namespace EVEBox.JieMian
{
    /// <summary>
    /// 右侧面板构建器 - 用户配置文件面板（配置同步页左侧）
    /// </summary>
    public partial class YouMianBanBuilder
    {
        private Panel CreateUserFilePanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = Color.White
            };

            _lblUserTitle = new Label
            {
                Text = "用户配置文件 (0个文件)",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _dgvUserFiles = new DataGridView
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
                ReadOnly = false,
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

            // ===== 用户ID列（可编辑） =====
            DataGridViewTextBoxColumn colUserId = new DataGridViewTextBoxColumn
            {
                HeaderText = "用户ID",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.White
                },
                ReadOnly = false
            };

            DataGridViewTextBoxColumn colUserTime = new DataGridViewTextBoxColumn
            {
                HeaderText = "修改时间",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ReadOnly = true
            };
            DataGridViewButtonColumn colUserBackup = new DataGridViewButtonColumn
            {
                HeaderText = "备份",
                Width = 67,
                Text = "💾",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };
            DataGridViewButtonColumn colUserSync = new DataGridViewButtonColumn
            {
                HeaderText = "同步",
                Width = 68,
                Text = "📂",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = true
            };

            _dgvUserFiles.Columns.Add(colUserId);
            _dgvUserFiles.Columns.Add(colUserTime);
            _dgvUserFiles.Columns.Add(colUserBackup);
            _dgvUserFiles.Columns.Add(colUserSync);

            // ===== 用户ID列双击编辑事件 =====
            _dgvUserFiles.CellDoubleClick += OnUserCellDoubleClick;
            _dgvUserFiles.CellEndEdit += OnUserCellEndEdit;

            // ===== 鼠标悬停显示原ID =====
            _dgvUserFiles.CellMouseEnter += OnUserCellMouseEnter;
            _dgvUserFiles.CellMouseLeave += OnUserCellMouseLeave;
            _dgvUserFiles.MouseLeave += OnUserFilesMouseLeave;

            // ★★★ Dock 逆序：先放 Fill 表格，后放 Top 标题（否则表格盖住标题）★★★
            panel.Controls.Add(_dgvUserFiles);
            panel.Controls.Add(_lblUserTitle);

            return panel;
        }

        // ===== 用户备注相关事件处理 =====

        private void OnUserCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

            var grid = sender as DataGridView;
            if (grid == null) return;

            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            grid.BeginEdit(true);
        }

        private void OnUserCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

            var grid = sender as DataGridView;
            if (grid == null) return;

            var row = grid.Rows[e.RowIndex];
            if (row.Tag == null) return;

            string userId = (row.Tag as YongHuWenJianXiang)?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            string newRemark = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString()?.Trim() ?? "";

            UserRemarkEdited?.Invoke(this, new YongHuBeiZhuBianJiCanShu(userId, newRemark));
        }

        // ===== 鼠标悬停显示原ID（修复版） =====

        private void OnUserCellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

            var grid = sender as DataGridView;
            if (grid == null) return;

            var row = grid.Rows[e.RowIndex];
            if (row.Tag == null) return;

            string userId = (row.Tag as YongHuWenJianXiang)?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            string displayText = row.Cells[e.ColumnIndex].Value?.ToString() ?? userId;

            if (displayText != userId)
            {
                Point mousePos = grid.PointToClient(Cursor.Position);
                _userToolTip.Show($"原ID: {userId}", grid, mousePos.X + 15, mousePos.Y - 20, 3000);
                _hoveredUserId = userId;
            }
            else
            {
                _userToolTip.Hide(grid);
                _hoveredUserId = null;
            }
        }

        private void OnUserCellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            _userToolTip.Hide(_dgvUserFiles);
            _hoveredUserId = null;
        }

        private void OnUserFilesMouseLeave(object sender, EventArgs e)
        {
            _userToolTip.Hide(_dgvUserFiles);
            _hoveredUserId = null;
        }

        // ===== 外部调用方法 =====

        public void UpdateUserRemarkDisplay(string userId, string remark)
        {
            foreach (DataGridViewRow row in _dgvUserFiles.Rows)
            {
                var item = row.Tag as YongHuWenJianXiang;
                if (item != null && item.UserId == userId)
                {
                    row.Cells[0].Value = string.IsNullOrWhiteSpace(remark) ? userId : remark;
                    break;
                }
            }
        }

        public void RefreshUserRemarks(Dictionary<string, string> remarks)
        {
            foreach (DataGridViewRow row in _dgvUserFiles.Rows)
            {
                var item = row.Tag as YongHuWenJianXiang;
                if (item != null)
                {
                    string userId = item.UserId;
                    if (remarks != null && remarks.TryGetValue(userId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                    {
                        row.Cells[0].Value = remark;
                    }
                    else
                    {
                        row.Cells[0].Value = userId;
                    }
                }
            }
        }
    }
}

using EVEBox.Features.BeiFen;
using System;
using System.Collections.Generic;
using System.Windows.Forms;



namespace EVEBox.Features.PeiZhiTongBu
{
    public class BiaoGeHandler
    {
        private readonly BeiFenService _backupService;
        private readonly TongBuService _syncService;
        private readonly Func<string> _getCurrentFolder;
        private readonly Action<YongHuWenJianXiang> _showUserSyncDialog;
        private readonly Action<JiaoSeWenJianXiang> _showCharSyncDialog;

        public BiaoGeHandler(
            BeiFenService backupService,
            TongBuService syncService,
            Func<string> getCurrentFolder,
            Action<YongHuWenJianXiang> showUserSyncDialog,
            Action<JiaoSeWenJianXiang> showCharSyncDialog)
        {
            _backupService = backupService;
            _syncService = syncService;
            _getCurrentFolder = getCurrentFolder;
            _showUserSyncDialog = showUserSyncDialog;
            _showCharSyncDialog = showCharSyncDialog;
        }

        public void OnUserFileCellClick(DataGridView grid, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 2) return;

            // ★★★ 从行Tag取数据项（列表排序后行号≠列表下标，不能用索引取）★★★
            var row = grid?.Rows[e.RowIndex];
            var item = row?.Tag as YongHuWenJianXiang;
            if (item == null) return;

            if (e.ColumnIndex == 2)
            {
                _backupService.BackupSingleFile(item.FilePath);
            }
            else if (e.ColumnIndex == 3)
            {
                _showUserSyncDialog?.Invoke(item);
            }
        }

        public void OnCharFileCellClick(DataGridView grid, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 3) return;

            // ★★★ 从行Tag取数据项（列表排序后行号≠列表下标，不能用索引取）★★★
            var row = grid?.Rows[e.RowIndex];
            var item = row?.Tag as JiaoSeWenJianXiang;
            if (item == null) return;

            if (e.ColumnIndex == 3)
            {
                _backupService.BackupSingleFile(item.FilePath);
            }
            else if (e.ColumnIndex == 4)
            {
                _showCharSyncDialog?.Invoke(item);
            }
        }

        public void OnBackupCellClick(DataGridView grid, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 2) return;

            // ★★★ 从行Tag取数据项（列表排序后行号≠列表下标，不能用索引取）★★★
            var row = grid?.Rows[e.RowIndex];
            var item = row?.Tag as BeiFenXiang;
            if (item == null) return;

            if (e.ColumnIndex == 2)
            {
                _backupService.ShowBackupInExplorer(item);
            }
            else if (e.ColumnIndex == 3)
            {
                _backupService.RestoreBackup(item);
            }
            else if (e.ColumnIndex == 4)
            {
                _backupService.DeleteBackup(item);
            }
        }
    }
}
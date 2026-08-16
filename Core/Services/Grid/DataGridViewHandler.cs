using EVESyncTool.Core.Services.Backup;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Core.Services.Sync;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EVESyncTool.Core.Services.Grid
{
    public class DataGridViewHandler
    {
        private readonly BackupService _backupService;
        private readonly SyncService _syncService;
        private readonly Func<string> _getCurrentFolder;
        private readonly Action<UserFileItem> _showUserSyncDialog;
        private readonly Action<CharacterFileItem> _showCharSyncDialog;

        public DataGridViewHandler(
            BackupService backupService,
            SyncService syncService,
            Func<string> getCurrentFolder,
            Action<UserFileItem> showUserSyncDialog,
            Action<CharacterFileItem> showCharSyncDialog)
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
            var item = row?.Tag as UserFileItem;
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
            var item = row?.Tag as CharacterFileItem;
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
            var item = row?.Tag as BackupItem;
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
using EVESyncTool.Core.Config;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Core.Services.Log;
using EVESyncTool.Dialogs.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EVESyncTool.Core.Services.Backup
{
    public class BackupService
    {
        private readonly FileSyncManager _fileSyncManager;
        private readonly LogService _logService;
        private readonly ConfigManager _configManager;
        private readonly Func<string> _getCurrentFolder;
        private readonly Action<Action> _invokeOnUI;
        private readonly Action _refreshBackupList;
        private readonly Action _refreshFileList;

        public BackupService(
            FileSyncManager fileSyncManager,
            LogService logService,
            ConfigManager configManager,
            Func<string> getCurrentFolder,
            Action<Action> invokeOnUI,
            Action refreshBackupList,
            Action refreshFileList = null)
        {
            _fileSyncManager = fileSyncManager;
            _logService = logService;
            _configManager = configManager;
            _getCurrentFolder = getCurrentFolder;
            _invokeOnUI = invokeOnUI;
            _refreshBackupList = refreshBackupList;
            _refreshFileList = refreshFileList;
        }

        public void PerformBackup()
        {
            string currentFolder = _getCurrentFolder?.Invoke();
            if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
            {
                CustomMessageBox.Show("请先选择有效的EVE配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string backupBasePath = _configManager.GetBackupPath();
                string backupPath = _fileSyncManager.BackupFolder(currentFolder, msg => _logService.Log("备份", msg, ""), backupBasePath);
                _refreshBackupList?.Invoke();
                CustomMessageBox.Show($"备份完成！\n保存路径: {backupPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logService.Log("备份", "成功", backupPath);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logService.Log("备份", "失败", ex.Message);
            }
        }

        public void DeleteAllBackups()
        {
            var result = CustomMessageBox.Show(
                "确定要删除所有历史备份文件吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                string backupBasePath = _configManager.GetBackupPath();
                int count = _fileSyncManager.DeleteAllBackups(msg => _logService.Log("删除备份", msg, ""), backupBasePath);
                _refreshBackupList?.Invoke();
                CustomMessageBox.Show($"已删除 {count} 个备份", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logService.Log("删除备份", "成功", $"删除了 {count} 个备份");
            }
        }

        public void RestoreBackup(BackupItem item)
        {
            string currentFolder = _getCurrentFolder?.Invoke();
            if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
            {
                CustomMessageBox.Show("请先选择目标文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = CustomMessageBox.Show(
                $"确定要从备份还原吗？\n\n备份: {item.DisplayName}\n目标: {currentFolder}",
                "确认还原",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // ★★★ 检测 EVE 客户端（还原会覆盖文件）★★★
                EveClientGuard.EnsureNoClient();

                try
                {
                    if (item.IsFile)
                    {
                        string destPath = Path.Combine(currentFolder, item.Name);
                        System.IO.File.Copy(item.Path, destPath, true);
                    }
                    else
                    {
                        _fileSyncManager.CopyDirectory(item.Path, currentFolder);
                    }
                    _refreshBackupList?.Invoke();
                    // ★★★ 还原后刷新文件列表（用户/角色表格立即反映还原结果）★★★
                    _refreshFileList?.Invoke();
                    CustomMessageBox.Show("还原完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logService.Log("还原备份", "成功", item.Name);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"还原失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logService.Log("还原备份", "失败", ex.Message);
                }
            }
        }

        public void ShowBackupInExplorer(BackupItem item)
        {
            try
            {
                string path = item.IsFile ? Path.GetDirectoryName(item.Path) : item.Path;
                Process.Start("explorer.exe", path);
                _logService.Log("打开备份位置", "成功", item.Name);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"无法打开文件夹: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logService.Log("打开备份位置", "失败", ex.Message);
            }
        }

        public void DeleteBackup(BackupItem item)
        {
            var result = CustomMessageBox.Show(
                $"确定要删除备份: {item.DisplayName}？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    if (item.IsFile)
                    {
                        System.IO.File.Delete(item.Path);
                    }
                    else
                    {
                        Directory.Delete(item.Path, true);
                    }
                    _refreshBackupList?.Invoke();
                    CustomMessageBox.Show("删除成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logService.Log("删除备份", "成功", item.Name);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logService.Log("删除备份", "失败", ex.Message);
                }
            }
        }

        public List<BackupFolderInfo> GetBackupFolders()
        {
            string backupBasePath = _configManager.GetBackupPath();
            return _fileSyncManager.GetBackupFolders(backupBasePath);
        }

        public string BackupSingleFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                CustomMessageBox.Show("文件不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            try
            {
                string baseBackupDir = _configManager.GetBackupPath();
                if (!Directory.Exists(baseBackupDir))
                    Directory.CreateDirectory(baseBackupDir);

                string fileName = Path.GetFileName(filePath);
                string backupPath = Path.Combine(baseBackupDir, fileName);

                System.IO.File.Copy(filePath, backupPath, true);

                // 修改备份文件的修改时间为当前时间
                System.IO.File.SetLastWriteTime(backupPath, DateTime.Now);

                _refreshBackupList?.Invoke();

                _logService.Log("备份单个文件", "成功", fileName);
                CustomMessageBox.Show($"文件备份完成！\n保存路径: {backupPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return backupPath;
            }
            catch (Exception ex)
            {
                _logService.Log("备份单个文件", "失败", ex.Message);
                CustomMessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
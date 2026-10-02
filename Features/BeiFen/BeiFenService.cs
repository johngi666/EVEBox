using EVEBox.Common.GongYong;
using EVEBox.Common.PeiZhi;
using EVEBox.Features.PeiZhiTongBu;
using EVEBox.OtherTools;
using EVEBox.Features.RiZhi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;



namespace EVEBox.Features.BeiFen
{
    public class BeiFenService
    {
        private readonly WenJianTongBuManager _fileSyncManager;
        private readonly RiZhiService _logService;
        private readonly PeiZhiManager _configManager;
        private readonly BeiFenHeXin _backupCore = new BeiFenHeXin();
        private readonly Func<string> _getCurrentFolder;
        private readonly Action<Action> _invokeOnUI;
        private readonly Action _refreshBackupList;
        private readonly Action _refreshFileList;

        public BeiFenService(
            WenJianTongBuManager fileSyncManager,
            RiZhiService logService,
            PeiZhiManager configManager,
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
                ZiDingYiMessageBox.Show("请先选择有效的EVE配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 文件操作交给 BeiFenHeXin（纯逻辑、可测），这里只负责提示
            var result = _backupCore.BackupWholeFolder(
                currentFolder,
                (string basePath) => _fileSyncManager.BackupFolder(currentFolder, msg => _logService.Log("备份", msg, ""), basePath),
                _configManager.GetBackupPath());

            if (!result.Success)
            {
                ZiDingYiMessageBox.Show($"备份失败: {result.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logService.Log("备份", "失败", result.Message);
                return;
            }

            _refreshBackupList?.Invoke();
            ZiDingYiMessageBox.Show($"备份完成！\n保存路径: {result.Path}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _logService.Log("备份", "成功", result.Path);
        }

        public void DeleteAllBackups()
        {
            var result = ZiDingYiMessageBox.Show(
                "确定要删除所有历史备份文件吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                string backupBasePath = _configManager.GetBackupPath();
                int count = _fileSyncManager.DeleteAllBackups(msg => _logService.Log("删除备份", msg, ""), backupBasePath);
                _refreshBackupList?.Invoke();
                ZiDingYiMessageBox.Show($"已删除 {count} 个备份", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logService.Log("删除备份", "成功", $"删除了 {count} 个备份");
            }
        }

        public void RestoreBackup(BeiFenXiang item)
        {
            string currentFolder = _getCurrentFolder?.Invoke();
            if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
            {
                ZiDingYiMessageBox.Show("请先选择目标文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = ZiDingYiMessageBox.Show(
                $"确定要从备份还原吗？\n\n备份: {item.DisplayName}\n目标: {currentFolder}",
                "确认还原",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                // ★★★ 检测 EVE 客户端（还原会覆盖文件）★★★
                EveKeHuDuanGuard.EnsureNoClient();

                var result = _backupCore.Restore(
                    item,
                    currentFolder,
                    (source, destination) => _fileSyncManager.CopyDirectory(source, destination));

                if (result.Success)
                {
                    _refreshBackupList?.Invoke();
                    // ★★★ 还原后刷新文件列表（用户/角色表格立即反映还原结果）★★★
                    _refreshFileList?.Invoke();
                    ZiDingYiMessageBox.Show("还原完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logService.Log("还原备份", "成功", item.Name);
                }
                else
                {
                    ZiDingYiMessageBox.Show($"还原失败: {result.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logService.Log("还原备份", "失败", result.Message);
                }
            }
        }

        public void ShowBackupInExplorer(BeiFenXiang item)
        {
            try
            {
                string path = item.IsFile ? Path.GetDirectoryName(item.Path) : item.Path;
                Process.Start("explorer.exe", path);
                _logService.Log("打开备份位置", "成功", item.Name);
            }
            catch (Exception ex)
            {
                ZiDingYiMessageBox.Show($"无法打开文件夹: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logService.Log("打开备份位置", "失败", ex.Message);
            }
        }

        public void DeleteBackup(BeiFenXiang item)
        {
            var confirm = ZiDingYiMessageBox.Show(
                $"确定要删除备份: {item.DisplayName}？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                // 备份属用户数据：删除走回收站，误删可捞回（项目规则第 1 条）
                if (HuiShouZhan.ShanChu(item.Path, out string error))
                {
                    _refreshBackupList?.Invoke();
                    ZiDingYiMessageBox.Show("已移入回收站", "删除成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logService.Log("删除备份", "成功", $"已移入回收站: {item.Name}");
                }
                else
                {
                    ZiDingYiMessageBox.Show($"删除失败: {error}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logService.Log("删除备份", "失败", error);
                }
            }
        }

        public List<BeiFenWenJianJiaXinXi> GetBackupFolders()
        {
            string backupBasePath = _configManager.GetBackupPath();
            return _fileSyncManager.GetBackupFolders(backupBasePath);
        }

        public string BackupSingleFile(string filePath)
        {
            var result = _backupCore.BackupSingleFile(filePath, _configManager.GetBackupPath());

            if (!result.Success)
            {
                // 文件不存在属于用户操作问题，用「错误」图标提示与原来一致
                ZiDingYiMessageBox.Show(result.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logService.Log("备份单个文件", "失败", result.Message);
                return null;
            }

            _refreshBackupList?.Invoke();
            _logService.Log("备份单个文件", "成功", Path.GetFileName(result.Path));
            ZiDingYiMessageBox.Show($"文件备份完成！\n保存路径: {result.Path}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return result.Path;
        }
    }
}
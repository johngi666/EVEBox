using EVEBox.Common.Shared;
using EVEBox.Common.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace EVEBox.Features.ConfigSync
{
    public class SyncService
    {
        private readonly FileSyncManager _fileSyncManager;
        private readonly FieldMappingService _fieldMappingService;
        private readonly Action<string, string, string> _logAction;
        private readonly SyncCore _syncCore = new SyncCore();

        public SyncService(
            FileSyncManager fileSyncManager = null,
            FieldMappingService fieldMappingService = null,
            Action<string, string, string> logAction = null)
        {
            _fileSyncManager = fileSyncManager ?? new FileSyncManager();
            _fieldMappingService = fieldMappingService ?? new FieldMappingService();
            _logAction = logAction;
        }

        #region 文件同步

        /// <summary>
        /// 将源文件复制到多个目标路径（逐个复制并记录日志），返回成功数量
        /// </summary>
        public int CopyFileToTargets(string sourcePath, List<string> targetPaths, string operationName)
        {
            if (string.IsNullOrEmpty(sourcePath) || targetPaths == null || targetPaths.Count == 0)
                return 0;

            int successCount = 0;
            string sourceName = Path.GetFileName(sourcePath);

            foreach (string targetPath in targetPaths)
            {
                try
                {
                    System.IO.File.Copy(sourcePath, targetPath, true);
                    Log(operationName, "成功", $"{sourceName} → {Path.GetFileName(targetPath)}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Log(operationName, "失败", $"{Path.GetFileName(targetPath)}: {ex.Message}");
                }
            }

            return successCount;
        }

        /// <summary>
        /// 同步所有文件（完整覆盖 / 部分覆盖）
        /// </summary>
        public async Task SyncAllFilesAsync(
            Func<string> getCurrentFolder,
            Action<string> setCurrentFolder,
            Func<Task> refreshFileList,
            Action<string> logAction)
        {
            string currentFolder = getCurrentFolder?.Invoke();

            // 盘点与「能不能同步」的规则交给 TongBuHeXin（纯逻辑、可测），这里只负责提示
            var inventory = _syncCore.Inventory(currentFolder);

            if (!inventory.CanSync)
            {
                bool folderMissing = string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder);
                CustomMessageBox.Show(
                    inventory.Message,
                    folderMissing ? "错误" : "提示",
                    MessageBoxButtons.OK,
                    folderMissing ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                return;
            }

            logAction?.Invoke("开始覆盖操作 - 完整覆盖");
            try
            {
                _fileSyncManager.FullSync(currentFolder, currentFolder, msg => logAction?.Invoke($"同步: {msg}"));
                await refreshFileList?.Invoke();
                CustomMessageBox.Show("完整覆盖完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ioEx) when (_syncCore.IsFileInUseError(ioEx.Message))
            {
                logAction?.Invoke("完整覆盖失败: 文件被占用");
                CustomMessageBox.Show(
                    $"覆盖失败！\n\n部分文件被其他程序占用（可能是 EVE 客户端正在运行）。\n请关闭所有 EVE 客户端后重试。\n\n{ioEx.Message}",
                    "文件被占用",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"完整覆盖失败: {ex.Message}");
                CustomMessageBox.Show($"覆盖失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 频道判断（聊天频道过滤基础能力）

        public HashSet<string> GetPublicChannelNames()
        {
            return _fieldMappingService.GetPublicChannelNames();
        }

        public bool IsPrivateChat(string title)
        {
            return _fieldMappingService.IsPrivateChat(title);
        }

        public bool IsLocalChannel(string title)
        {
            return _fieldMappingService.IsLocalChannel(title);
        }

        public string ExtractBaseName(string title)
        {
            return _fieldMappingService.ExtractBaseName(title);
        }

        #endregion

        #region 日志辅助

        private void Log(string operation, string status, string details)
        {
            _logAction?.Invoke(operation, status, details);
        }

        #endregion
    }
}

using EVEBox.GongYong;
using EVEBox.PeiZhi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace EVEBox.PeiZhiTongBu
{
    public class TongBuService
    {
        private readonly WenJianTongBuManager _fileSyncManager;
        private ZiDuanYingSheService _fieldMappingService;
        private readonly PeiZhiManager _configManager;
        private readonly Action<string, string, string> _logAction;

        public TongBuService(
            WenJianTongBuManager fileSyncManager = null,
            ZiDuanYingSheService fieldMappingService = null,
            PeiZhiManager configManager = null,
            Action<string, string, string> logAction = null)
        {
            _fileSyncManager = fileSyncManager ?? new WenJianTongBuManager();
            _fieldMappingService = fieldMappingService ?? new ZiDuanYingSheService(new TongBuSheZhi());
            _configManager = configManager ?? new PeiZhiManager();
            _logAction = logAction;

            LoadSettings();
        }

        public void LoadSettings()
        {
            var settings = _configManager.GetSyncSettings();
            _fieldMappingService = new ZiDuanYingSheService(settings);
        }

        public TongBuSheZhi GetSettings()
        {
            return _fieldMappingService.GetSettings();
        }

        public void SaveSettings(TongBuSheZhi settings)
        {
            _configManager.SaveSyncSettings(settings);
            _fieldMappingService = new ZiDuanYingSheService(settings);
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
            if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
            {
                ZiDingYiMessageBox.Show("请先选择EVE配置文件夹", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 获取所有用户文件和角色文件
            var userFiles = GetFilesByPattern(currentFolder, @"^core_user_\d+\.dat$")
                .Where(f => !f.StartsWith("core_user_.dat")).ToList();
            var charFiles = GetFilesByPattern(currentFolder, @"^core_char_\d+\.dat$")
                .Where(f => !f.StartsWith("core_char_.dat")).ToList();

            // 过滤异常文件
            userFiles = userFiles.Where(f => !IsAbnormalFileName(f)).ToList();
            charFiles = charFiles.Where(f => !IsAbnormalFileName(f)).ToList();

            if (userFiles.Count <= 1 && charFiles.Count <= 1)
            {
                ZiDingYiMessageBox.Show("没有足够的文件进行覆盖操作\n每个类型至少需要2个文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ★★★ 修复：使用 System.IO.File.GetLastWriteTime ★★★
            var latestUser = userFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(Path.Combine(currentFolder, f))).FirstOrDefault();
            var latestChar = charFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(Path.Combine(currentFolder, f))).FirstOrDefault();

            logAction?.Invoke("开始覆盖操作 - 完整覆盖");
            try
            {
                _fileSyncManager.FullSync(currentFolder, currentFolder, msg => logAction?.Invoke($"同步: {msg}"));
                await refreshFileList?.Invoke();
                ZiDingYiMessageBox.Show("完整覆盖完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("被占用") || ioEx.Message.Contains("used"))
            {
                logAction?.Invoke($"完整覆盖失败: 文件被占用");
                ZiDingYiMessageBox.Show(
                    $"覆盖失败！\n\n部分文件被其他程序占用（可能是 EVE 客户端正在运行）。\n请关闭所有 EVE 客户端后重试。\n\n{ioEx.Message}",
                    "文件被占用",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"完整覆盖失败: {ex.Message}");
                ZiDingYiMessageBox.Show($"覆盖失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 判断是否为异常文件名
        /// </summary>
        private bool IsAbnormalFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return true;

            string[] abnormalPatterns = {
                "('char'", "('user'", "None", "dat')",
                "('", "')", ".dat.dat", ".."
            };

            foreach (var pattern in abnormalPatterns)
            {
                if (fileName.Contains(pattern))
                    return true;
            }

            return false;
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

        #region 工具方法

        private List<string> GetFilesByPattern(string folder, string pattern)
        {
            var files = new List<string>();
            if (!Directory.Exists(folder))
                return files;

            foreach (string file in Directory.GetFiles(folder))
            {
                string name = Path.GetFileName(file);
                if (Regex.IsMatch(name, pattern))
                    files.Add(name);
            }
            return files;
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
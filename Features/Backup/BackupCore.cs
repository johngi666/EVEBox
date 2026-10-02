using EVEBox.Features.ConfigSync;
using System;
using System.IO;

namespace EVEBox.Features.Backup
{
    /// <summary>
    /// 一次备份/还原操作的结果。
    /// 业务层只负责干活并如实汇报，弹不弹窗、弹什么由界面层决定
    /// （这样业务逻辑才能脱离 WinForms 单独测试）。
    /// </summary>
    public readonly struct BeiFenResult
    {
        public bool Success { get; }
        public string Message { get; }
        public string Path { get; }

        private BeiFenResult(bool succeeded, string message, string path)
        {
            Success = succeeded;
            Message = message;
            Path = path;
        }

        public static BeiFenResult Ok(string path, string message = null)
            => new BeiFenResult(true, message, path);

        public static BeiFenResult Fail(string message)
            => new BeiFenResult(false, message, null);
    }

    /// <summary>
    /// 备份/还原的纯业务逻辑：只碰文件系统，不弹任何窗，因此可以单独写单元测试。
    ///
    /// 原先这些判断和文件操作都写在 BeiFenService 里、和弹窗混在一起，
    /// 想测就得先起一个 WinForms 消息循环，实际等于没法测。
    /// BeiFenService 现在退化成「先问一句 → 调这里 → 再提示一句」的薄壳。
    /// </summary>
    public class BackupCore
    {
        /// <summary>
        /// 备份整个配置文件夹到 backupBasePath，返回新建的备份目录路径。
        /// </summary>
        /// <param name="currentFolder">当前生效的配置文件夹</param>
        /// <param name="backupAction">真正的目录备份动作（由调用方注入，便于测试）</param>
        /// <param name="backupBasePath">备份根目录</param>
        public BeiFenResult BackupWholeFolder(
            string currentFolder,
            Func<string, string> backupAction,
            string backupBasePath)
        {
            if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
                return BeiFenResult.Fail("请先选择有效的EVE配置文件夹");

            if (backupAction == null)
                return BeiFenResult.Fail("内部错误：备份动作未提供");

            try
            {
                string path = backupAction(backupBasePath);
                if (string.IsNullOrEmpty(path))
                    return BeiFenResult.Fail("备份未生成结果");

                return BeiFenResult.Ok(path);
            }
            catch (Exception ex)
            {
                return BeiFenResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 备份单个文件到 backupBasePath 下（同名覆盖），返回备份文件路径。
        /// </summary>
        public BeiFenResult BackupSingleFile(string filePath, string backupBasePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return BeiFenResult.Fail("文件不存在");

            try
            {
                if (!Directory.Exists(backupBasePath))
                    Directory.CreateDirectory(backupBasePath);

                string targetPath = Path.Combine(backupBasePath, Path.GetFileName(filePath));
                File.Copy(filePath, targetPath, true);

                // 备份时间以"刚备份"为准，便于在列表里按时间排序
                File.SetLastWriteTime(targetPath, DateTime.Now);

                return BeiFenResult.Ok(targetPath);
            }
            catch (Exception ex)
            {
                return BeiFenResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 从备份还原到目标文件夹（覆盖同名内容）。
        /// </summary>
        /// <param name="item">要还原的备份项</param>
        /// <param name="targetFolder">还原目标文件夹</param>
        /// <param name="copyDirectory">目录复制动作（由调用方注入，便于测试）</param>
        public BeiFenResult Restore(BackupItem item, string targetFolder, Action<string, string> copyDirectory)
        {
            if (item == null)
                return BeiFenResult.Fail("没有选择备份项");

            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
                return BeiFenResult.Fail("请先选择目标文件夹");

            try
            {
                if (item.IsFile)
                {
                    File.Copy(item.Path, Path.Combine(targetFolder, item.Name), true);
                }
                else
                {
                    if (copyDirectory == null)
                        return BeiFenResult.Fail("内部错误：目录复制动作未提供");

                    copyDirectory(item.Path, targetFolder);
                }

                return BeiFenResult.Ok(targetFolder);
            }
            catch (Exception ex)
            {
                return BeiFenResult.Fail(ex.Message);
            }
        }
    }
}

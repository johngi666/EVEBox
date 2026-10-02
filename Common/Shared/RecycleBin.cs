using System;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace EVEBox.Common.Shared
{
    /// <summary>
    /// 删除文件的统一出口。
    ///
    /// 项目规则：删除文件必须走回收站，禁止永久删除（见交接文档「操作规则」第 1 条）。
    /// 用户数据（备份等）尤其不能直接抹掉，误删要能捞回来。
    ///
    /// 注意：仅用于「用户数据 / 用户可见的文件」。
    /// 程序自己的临时文件（下载残留、解包中间体）不属于此列，仍用 File.Delete 立即清理，
    /// 否则回收站会被临时垃圾塞满。
    /// </summary>
    public static class RecycleBin
    {
        /// <summary>
        /// 把文件或目录送进回收站（目录连同内容一起）。
        /// 目标不存在时按「无需删除」处理，直接返回成功。
        /// </summary>
        /// <param name="path">文件或目录的完整路径</param>
        /// <param name="error">失败原因；成功时为 null</param>
        /// <returns>是否已成功送进回收站</returns>
        public static bool ShanChu(string path, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "路径为空";
                return false;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    FileSystem.DeleteDirectory(
                        path,
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                }
                else if (File.Exists(path))
                {
                    FileSystem.DeleteFile(
                        path,
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                }
                else
                {
                    // 本来就不存在，视为已完成
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            // 回收站不可用（被组策略禁用、卷未启用回收站等）时系统会静默跳过，
            // 这里以「目标是否真的消失」作为最终判据，避免误报成功。
            if (Directory.Exists(path) || File.Exists(path))
            {
                error = "未能移入回收站（可能该磁盘的回收站已禁用）";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 把文件或目录送进回收站；失败时返回 false 并给出原因，同时记录一条日志。
        /// </summary>
        public static bool ShanChu(string path, out string error, Action<string, string> log)
        {
            bool succeeded = ShanChu(path, out error);
            if (!succeeded)
            {
                log?.Invoke("删除", error);
            }
            return succeeded;
        }
    }
}

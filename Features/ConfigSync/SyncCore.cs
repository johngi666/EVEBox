using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EVEBox.Features.ConfigSync
{
    /// <summary>
    /// 配置同步的纯业务逻辑：文件盘点、异常文件名过滤、可同步性判断。
    ///
    /// 这些判断原先散在 TongBuService.SyncAllFilesAsync 里，和弹窗混在一起，
    /// 导致「什么情况下该拒绝同步」这类规则无法单独验证。
    /// 现在服务层只负责提问与提示，规则在这里，可以写单元测试。
    /// </summary>
    public class SyncCore
    {
        /// <summary>用户文件命名规则</summary>
        public const string UserFilePattern = @"^core_user_\d+\.dat$";

        /// <summary>角色文件命名规则</summary>
        public const string CharFilePattern = @"^core_char_\d+\.dat$";

        /// <summary>
        /// 一次同步前的盘点结果。
        /// </summary>
        public class TongBuInventory
        {
            /// <summary>可参与同步的用户文件（文件名，含扩展名）</summary>
            public List<string> UserFiles { get; set; } = new List<string>();

            /// <summary>可参与同步的角色文件</summary>
            public List<string> CharFiles { get; set; } = new List<string>();

            /// <summary>是否可以继续同步；false 时 Message 说明原因</summary>
            public bool CanSync { get; set; }

            /// <summary>不能同步时的原因（可直接展示给用户）</summary>
            public string Message { get; set; }

            /// <summary>最近修改的用户文件（用于完整覆盖时挑基准）</summary>
            public string LatestUserFile { get; set; }

            /// <summary>最近修改的角色文件</summary>
            public string LatestCharFile { get; set; }
        }

        /// <summary>
        /// 盘点目标文件夹里可同步的文件。
        /// 每个类型至少要有 2 个文件才允许覆盖——只有 1 个文件时"覆盖"没有意义。
        /// </summary>
        public TongBuInventory Inventory(string folder)
        {
            var result = new TongBuInventory();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                result.CanSync = false;
                result.Message = "请先选择EVE配置文件夹";
                return result;
            }

            List<string> userFiles = GetFilesByPattern(folder, UserFilePattern)
                .Where(f => !f.StartsWith("core_user_.dat"))
                .Where(f => !IsAbnormalFileName(f))
                .ToList();

            List<string> charFiles = GetFilesByPattern(folder, CharFilePattern)
                .Where(f => !f.StartsWith("core_char_.dat"))
                .Where(f => !IsAbnormalFileName(f))
                .ToList();

            result.UserFiles = userFiles;
            result.CharFiles = charFiles;

            if (userFiles.Count <= 1 && charFiles.Count <= 1)
            {
                result.CanSync = false;
                result.Message = "没有足够的文件进行覆盖操作\n每个类型至少需要2个文件";
                return result;
            }

            result.CanSync = true;
            result.LatestUserFile = Latest(userFiles, folder);
            result.LatestCharFile = Latest(charFiles, folder);
            return result;
        }

        /// <summary>
        /// 判断是否为异常文件名（EVE 生成过一些残缺文件名，参与同步会出错）。
        /// </summary>
        public bool IsAbnormalFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return true;

            string[] abnormalPatterns = {
                "('char'", "('user'", "None", "dat')",
                "('", "')", ".dat.dat", ".."
            };

            return abnormalPatterns.Any(p => fileName.Contains(p));
        }

        /// <summary>
        /// 覆盖失败是否属于「文件被占用」——这类失败要单独给用户一句可操作的提示。
        /// </summary>
        public bool IsFileInUseError(string message)
            => !string.IsNullOrEmpty(message) &&
               (message.Contains("被占用") || message.Contains("used"));

        private static string Latest(List<string> fileNames, string folder)
        {
            if (fileNames == null || fileNames.Count == 0) return null;

            return fileNames
                .OrderByDescending(f => File.GetLastWriteTime(Path.Combine(folder, f)))
                .FirstOrDefault();
        }

        private static List<string> GetFilesByPattern(string folder, string pattern)
        {
            var files = new List<string>();
            if (!Directory.Exists(folder)) return files;

            foreach (string file in Directory.GetFiles(folder))
            {
                string name = Path.GetFileName(file);
                if (Regex.IsMatch(name, pattern))
                    files.Add(name);
            }
            return files;
        }
    }
}

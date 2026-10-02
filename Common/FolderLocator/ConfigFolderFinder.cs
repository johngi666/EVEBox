using EVEBox.App;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;



namespace EVEBox.Common.FolderLocator
{
    /// <summary>
    /// EVE配置文件夹查找
    /// </summary>
    public class ConfigFolderFinder
    {
        private readonly Dictionary<string, string> _serverKeywords;
        private readonly Action<string, string, string> _logAction;
        private readonly Action<string> _onFolderFound;

        public ConfigFolderFinder(Dictionary<string, string> serverKeywords, Action<string, string, string> logAction, Action<string> onFolderFound)
        {
            _serverKeywords = serverKeywords;
            _logAction = logAction;
            _onFolderFound = onFolderFound;
        }

        /// <summary>
        /// 自动查找文件夹（先查缓存，再快速查找）
        /// </summary>
        public string AutoFind(string serverName, string cachedPath, Action updateUi = null)
        {
            updateUi?.Invoke();

            if (cachedPath != null && cachedPath.ToLower().Contains(_serverKeywords[serverName]))
            {
                if (Directory.Exists(cachedPath))
                {
                    // 正常启动都会走这里（缓存命中），不写日志——每次开机都出现的
                    // 成功记录只是噪音，用户看操作日志时会被它挤掉真正有用的信息
                    return cachedPath;
                }
            }

            string found = QuickFind(serverName);

            if (found != null)
            {
                // 同理：自动查找成功也不记日志（失败才记，见下）
                _onFolderFound?.Invoke(found);
                return found;
            }

            // 失败要留痕：这是用户需要知道的信息（为什么没自动找到配置文件夹）
            _logAction?.Invoke("自动查找文件夹", "失败", "未找到设置文件夹");
            return null;
        }

        /// <summary>
        /// 通过 Windows 注册表查找 EVE 安装路径
        /// </summary>
        private string FindFromRegistry()
        {
            try
            {
                // CCP Launcher v2+ 存储路径
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\CCP\EVEOnline"))
                {
                    if (key != null)
                    {
                        var cacheFolder = key.GetValue("CacheFolder") as string;
                        if (!string.IsNullOrEmpty(cacheFolder))
                        {
                            // CacheFolder 通常是 %LOCALAPPDATA%\CCP\EVE，直接检查
                            string expanded = Environment.ExpandEnvironmentVariables(cacheFolder);
                            if (Directory.Exists(expanded))
                            {
                                // 搜索该目录下的 settings_Default
                                try
                                {
                                    foreach (string dir in Directory.GetDirectories(expanded, "*", SearchOption.TopDirectoryOnly))
                                    {
                                        string settingsPath = Path.Combine(dir, "settings_Default");
                                        if (Directory.Exists(settingsPath))
                                            return settingsPath;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // 某些目录无权限遍历属正常情况，但要知道是哪个目录出的问题
                                    _logAction?.Invoke("注册表查找", "跳过目录", $"{expanded} - {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // 备用：检查 CCP 旧版注册表
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\CCP"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            if (subKeyName.Contains("EVE", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var subKey = key.OpenSubKey(subKeyName))
                                {
                                    var path = subKey?.GetValue("Path") as string
                                        ?? subKey?.GetValue("InstallPath") as string;
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        string settingsPath = Path.Combine(path, "settings_Default");
                                        if (Directory.Exists(settingsPath))
                                            return settingsPath;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 注册表读取失败不该静默：查不到文件夹时这是关键线索
                _logAction?.Invoke("注册表查找", "失败", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// 快速查找（注册表 → LOCALAPPDATA → 已知路径 → C/D/E 盘）
        ///
        /// 日志原则：只记「失败」与「异常」。
        /// 查找过程（在哪几个位置找、找到了没）属排错用的追踪信息，
        /// 正常启动每次都会出现，记进用户可见的操作日志只会把有用信息挤掉。
        /// </summary>
        public string QuickFind(string serverName)
        {
            if (!_serverKeywords.TryGetValue(serverName, out string keyword))
                return null;

            // ===== 0. 优先查注册表 =====
            string regPath = FindFromRegistry();
            if (regPath != null)
            {
                return regPath;
            }

            // ===== 1. 优先查找 %LOCALAPPDATA%\CCP\EVE\ =====
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                string evePath = Path.Combine(localAppData, "CCP", "EVE");
                if (Directory.Exists(evePath))
                {
                    string folder = ScanDirectory(evePath, keyword);
                    if (folder != null)
                    {
                        return folder;
                    }
                }
            }

            // ===== 1.5 检查 ProgramData =====
            string programData = Environment.GetEnvironmentVariable("ProgramData");
            if (!string.IsNullOrEmpty(programData))
            {
                string ccpData = Path.Combine(programData, "CCP", "EVE");
                if (Directory.Exists(ccpData))
                {
                    string folder = ScanDirectory(ccpData, keyword);
                    if (folder != null)
                    {
                        return folder;
                    }
                }
            }

            // ===== 1.6 检查 Steam 库 =====
            string[] steamPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "EVE Online"),
                Path.Combine("C:", "ChengXu Files (x86)", "Steam", "steamapps", "common", "EVE Online"),
                Path.Combine("D:", "SteamLibrary", "steamapps", "common", "EVE Online"),
                Path.Combine("E:", "SteamLibrary", "steamapps", "common", "EVE Online")
            };
            foreach (string steamPath in steamPaths)
            {
                if (Directory.Exists(steamPath))
                {
                    string folder = ScanDirectory(steamPath, keyword);
                    if (folder != null)
                    {
                        return folder;
                    }
                }
            }

            // ===== 2. 备用：C/D/E 盘 =====
            string[] drives = { "C:", "D:", "E:" };
            foreach (string drive in drives)
            {
                if (!Directory.Exists(drive)) continue;

                // 2.1 检查 Users\用户名\AppData\Local\CCP\EVE\
                string userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? $"C:\\Users\\{Environment.UserName}";
                string standardPath = Path.Combine(userProfile, "AppData", "Local", "CCP", "EVE");
                if (Directory.Exists(standardPath))
                {
                    string folder = ScanDirectory(standardPath, keyword);
                    if (folder != null)
                    {
                        return folder;
                    }
                }

                // 2.2 检查其他可能路径
                string[] possiblePaths = {
                    Path.Combine(drive, "CCP"),
                    Path.Combine(drive, "ChengXu Files", "CCP"),
                    Path.Combine(drive, "ChengXu Files (x86)", "CCP"),
                    Path.Combine(drive, "Games", "EVE"),
                    Path.Combine(drive, "EVE"),
                    Path.Combine(drive, "Users")
                };
                foreach (string basePath in possiblePaths)
                {
                    if (Directory.Exists(basePath))
                    {
                        string folder = ScanDirectory(basePath, keyword);
                        if (folder != null)
                        {
                            return folder;
                        }
                    }
                }

                // 2.3 根目录下扫描
                try
                {
                    var directories = Directory.GetDirectories(drive, "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in directories)
                    {
                        if (dir.Count(c => c == '\\') - drive.Count(c => c == '\\') > 2) continue;
                        if (dir.ToLower().Contains(keyword.ToLower()))
                        {
                            string settingsPath = Path.Combine(dir, "settings_Default");
                            if (Directory.Exists(settingsPath))
                            {
                                return settingsPath;
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (Exception) { continue; }
            }

            return null;
        }

        /// <summary>
        /// 扫描目录
        /// </summary>
        public string ScanDirectory(string basePath, string keyword)
        {
            if (!Directory.Exists(basePath)) return null;
            try
            {
                var dirs = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories);
                foreach (string dir in dirs)
                {
                    if (dir.ToLower().Contains(keyword.ToLower()))
                    {
                        string settingsPath = Path.Combine(dir, "settings_Default");
                        if (Directory.Exists(settingsPath))
                            return settingsPath;
                    }
                }
            }
            catch (Exception ex)
            {
                // 快速查找失败同样要留痕，否则用户只看到"没找到"
                _logAction?.Invoke("快速查找", "失败", $"{basePath} - {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 深度搜索（全盘扫描）
        /// </summary>
        public string DeepSearch(string serverName, IProgress<string> progress, Func<bool> isCancelled)
        {
            if (!_serverKeywords.TryGetValue(serverName, out string keyword))
                return null;

            DriveInfo[] drives = DriveInfo.GetDrives();

            string[] skipPaths = {
                "Windows", "System32", "ChengXu Files", "ChengXu Files (x86)",
                "ProgramData", "System Volume Information", "$Recycle.Bin",
                "Temp", "tmp", "Cache", "Microsoft", "MSBuild", "Reference Assemblies"
            };

            string[] priorityPaths = { "Games", "Game", "EVE", "CCP", "ChengXu Files", "ChengXu Files (x86)" };

            // 优先搜索 LOCALAPPDATA
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                string evePath = Path.Combine(localAppData, "CCP", "EVE");
                if (Directory.Exists(evePath))
                {
                    progress?.Report($"搜索 LOCALAPPDATA: {evePath}");
                    string result = SearchDirectoryDeep(evePath, keyword, 5, skipPaths);
                    if (result != null) return result;
                }
            }

            foreach (DriveInfo drive in drives)
            {
                if (!drive.IsReady) continue;
                if (drive.DriveType != DriveType.Fixed) continue;

                string driveName = drive.Name;
                progress?.Report($"正在搜索 {driveName}...");

                foreach (string priorityPath in priorityPaths)
                {
                    string fullPath = Path.Combine(driveName, priorityPath);
                    if (Directory.Exists(fullPath))
                    {
                        string result = SearchDirectoryDeep(fullPath, keyword, 3, skipPaths);
                        if (result != null) return result;
                    }
                }

                try
                {
                    var directories = Directory.GetDirectories(driveName);
                    foreach (string dir in directories)
                    {
                        if (isCancelled != null && isCancelled()) return null;

                        string dirName = Path.GetFileName(dir);
                        if (skipPaths.Any(skip => dirName.Contains(skip))) continue;

                        string result = SearchDirectoryDeep(dir, keyword, 3, skipPaths);
                        if (result != null) return result;
                    }
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (Exception) { continue; }
            }

            return null;
        }

        private string SearchDirectoryDeep(string basePath, string keyword, int maxDepth, string[] skipPaths)
        {
            if (maxDepth <= 0) return null;

            try
            {
                foreach (string dir in Directory.GetDirectories(basePath))
                {
                    string dirName = Path.GetFileName(dir);

                    if (skipPaths.Any(skip => dirName.Contains(skip))) continue;

                    if (dirName.ToLower().Contains(keyword.ToLower()))
                    {
                        string settingsPath = Path.Combine(dir, "settings_Default");
                        if (Directory.Exists(settingsPath))
                        {
                            return settingsPath;
                        }
                    }

                    string result = SearchDirectoryDeep(dir, keyword, maxDepth - 1, skipPaths);
                    if (result != null) return result;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 全盘扫描碰到无权限目录属常态，跳过即可（不记日志，避免刷屏）
            }
            catch (Exception ex)
            {
                _logAction?.Invoke("深度搜索", "跳过目录", $"{basePath} - {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 手动选择文件夹
        /// </summary>
        public string ManualSelect(IWin32Window owner)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择EVE配置文件夹 (settings_Default)";
            if (dialog.ShowDialog(owner) == DialogResult.OK)
            {
                string folder = dialog.SelectedPath;
                if (Directory.GetFiles(folder).Any(f => Regex.IsMatch(Path.GetFileName(f), @"^core_user_\d+\.dat$")))
                {
                    return folder;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取默认配置路径（当前服务器下的 settings_Default）
        /// </summary>
        public string GetDefaultPath(string currentFolder)
        {
            if (string.IsNullOrEmpty(currentFolder)) return null;
            string parentDir = Directory.GetParent(currentFolder)?.FullName;
            if (string.IsNullOrEmpty(parentDir)) return null;
            string defaultPath = Path.Combine(parentDir, "settings_Default");
            return Directory.Exists(defaultPath) ? defaultPath : null;
        }

        /// <summary>
        /// 验证文件夹是否有效
        /// </summary>
        public bool IsValidFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return false;
            return Directory.GetFiles(folder).Any(f => Regex.IsMatch(Path.GetFileName(f), @"^core_user_\d+\.dat$"));
        }
    }
}
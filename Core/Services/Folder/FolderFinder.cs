using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EVEBox.Core.Services.Folder
{
    /// <summary>
    /// EVE配置文件夹查找
    /// </summary>
    public class FolderFinder
    {
        private readonly Dictionary<string, string> _serverKeywords;
        private readonly Action<string, string, string> _logAction;
        private readonly Action<string> _onFolderFound;

        public FolderFinder(Dictionary<string, string> serverKeywords, Action<string, string, string> logAction, Action<string> onFolderFound)
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
                    _logAction?.Invoke("自动查找文件夹", "缓存命中", cachedPath);
                    return cachedPath;
                }
            }

            string found = QuickFind(serverName);

            if (found != null)
            {
                _logAction?.Invoke("自动查找文件夹", "成功", found);
                _onFolderFound?.Invoke(found);
                return found;
            }

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
                                catch { }
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
            catch (Exception) { }

            return null;
        }

        /// <summary>
        /// 快速查找（注册表 → LOCALAPPDATA → 已知路径 → C/D/E 盘）
        /// </summary>
        public string QuickFind(string serverName)
        {
            if (!_serverKeywords.TryGetValue(serverName, out string keyword))
                return null;

            _logAction?.Invoke("快速查找", $"查找 {serverName} 配置", "");

            // ===== 0. 优先查注册表 =====
            string regPath = FindFromRegistry();
            if (regPath != null)
            {
                _logAction?.Invoke("快速查找", "注册表命中", regPath);
                return regPath;
            }

            // ===== 1. 优先查找 %LOCALAPPDATA%\CCP\EVE\ =====
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                string evePath = Path.Combine(localAppData, "CCP", "EVE");
                if (Directory.Exists(evePath))
                {
                    _logAction?.Invoke("快速查找", $"检查 LOCALAPPDATA: {evePath}", "");
                    string folder = ScanDirectory(evePath, keyword);
                    if (folder != null)
                    {
                        _logAction?.Invoke("快速查找", "成功", folder);
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
                    _logAction?.Invoke("快速查找", $"检查 ProgramData: {ccpData}", "");
                    string folder = ScanDirectory(ccpData, keyword);
                    if (folder != null)
                    {
                        _logAction?.Invoke("快速查找", "成功", folder);
                        return folder;
                    }
                }
            }

            // ===== 1.6 检查 Steam 库 =====
            string[] steamPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "EVE Online"),
                Path.Combine("C:", "Program Files (x86)", "Steam", "steamapps", "common", "EVE Online"),
                Path.Combine("D:", "SteamLibrary", "steamapps", "common", "EVE Online"),
                Path.Combine("E:", "SteamLibrary", "steamapps", "common", "EVE Online")
            };
            foreach (string steamPath in steamPaths)
            {
                if (Directory.Exists(steamPath))
                {
                    _logAction?.Invoke("快速查找", $"检查 Steam: {steamPath}", "");
                    string folder = ScanDirectory(steamPath, keyword);
                    if (folder != null)
                    {
                        _logAction?.Invoke("快速查找", "成功", folder);
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
                        _logAction?.Invoke("快速查找", "成功", folder);
                        return folder;
                    }
                }

                // 2.2 检查其他可能路径
                string[] possiblePaths = {
                    Path.Combine(drive, "CCP"),
                    Path.Combine(drive, "Program Files", "CCP"),
                    Path.Combine(drive, "Program Files (x86)", "CCP"),
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
                            _logAction?.Invoke("快速查找", "成功", folder);
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
                                _logAction?.Invoke("快速查找", "成功", settingsPath);
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
            catch (Exception) { }
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
                "Windows", "System32", "Program Files", "Program Files (x86)",
                "ProgramData", "System Volume Information", "$Recycle.Bin",
                "Temp", "tmp", "Cache", "Microsoft", "MSBuild", "Reference Assemblies"
            };

            string[] priorityPaths = { "Games", "Game", "EVE", "CCP", "Program Files", "Program Files (x86)" };

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
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }

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
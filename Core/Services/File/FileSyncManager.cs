using EVESyncTool.Core.Config;
using EVESyncTool.Core.Mapping;
using EVESyncTool.Core.Marshal;
using EVESyncTool.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EVESyncTool.Core.Services.File
{
    public class FileSyncManager
    {
        private readonly string _marshalDllPath;
        private readonly MarshalSyncService _marshalService;

        public FileSyncManager(string marshalDllPath = "marshal_ffi.dll")
        {
            _marshalDllPath = marshalDllPath;
            _marshalService = new MarshalSyncService();
        }

        #region 文件同步核心方法

        /// <summary>
        /// 完整同步：用最新文件覆盖同类型的其他文件
        /// </summary>
        public void FullSync(string sourceFolder, string targetFolder, Action<string> logAction = null)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"源文件夹不存在: {sourceFolder}");
            if (!Directory.Exists(targetFolder))
                throw new DirectoryNotFoundException($"目标文件夹不存在: {targetFolder}");

            logAction?.Invoke($"开始完整同步: {Path.GetFileName(sourceFolder)} → {Path.GetFileName(targetFolder)}");

            var files = Directory.GetFiles(sourceFolder);
            int copiedCount = 0;
            int skippedCount = 0;

            // 分别获取用户文件和角色文件
            var userFiles = files.Where(f => Regex.IsMatch(Path.GetFileName(f), @"^core_user_\d+\.dat$")).ToList();
            var charFiles = files.Where(f => Regex.IsMatch(Path.GetFileName(f), @"^core_char_\d+\.dat$")).ToList();

            // ★★★ 找出最新的文件 ★★★
            string latestUser = userFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).FirstOrDefault();
            string latestChar = charFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).FirstOrDefault();

            logAction?.Invoke($"  最新用户文件: {Path.GetFileName(latestUser) ?? "无"}");
            logAction?.Invoke($"  最新角色文件: {Path.GetFileName(latestChar) ?? "无"}");

            // ★★★ 用最新用户文件覆盖其他用户文件 ★★★
            if (!string.IsNullOrEmpty(latestUser))
            {
                foreach (string file in userFiles)
                {
                    if (file == latestUser) continue;  // 跳过自己

                    if (IsFileLocked(file))
                    {
                        skippedCount++;
                        logAction?.Invoke($"  ⚠ 文件被占用，跳过: {Path.GetFileName(file)}");
                        continue;
                    }

                    try
                    {
                        System.IO.File.Copy(latestUser, file, true);
                        copiedCount++;
                        logAction?.Invoke($"  ✓ 已覆盖: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        skippedCount++;
                        logAction?.Invoke($"  ✗ 覆盖失败: {Path.GetFileName(file)} - {ex.Message}");
                    }
                }
            }

            // ★★★ 用最新角色文件覆盖其他角色文件 ★★★
            if (!string.IsNullOrEmpty(latestChar))
            {
                foreach (string file in charFiles)
                {
                    if (file == latestChar) continue;  // 跳过自己

                    if (IsFileLocked(file))
                    {
                        skippedCount++;
                        logAction?.Invoke($"  ⚠ 文件被占用，跳过: {Path.GetFileName(file)}");
                        continue;
                    }

                    try
                    {
                        System.IO.File.Copy(latestChar, file, true);
                        copiedCount++;
                        logAction?.Invoke($"  ✓ 已覆盖: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        skippedCount++;
                        logAction?.Invoke($"  ✗ 覆盖失败: {Path.GetFileName(file)} - {ex.Message}");
                    }
                }
            }

            logAction?.Invoke($"完整同步完成，共覆盖 {copiedCount} 个文件，跳过 {skippedCount} 个文件");
        }

        /// <summary>
        /// 检查文件是否被占用
        /// </summary>
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
        }

        public string GetLatestFile(string folder, string pattern = "*.*")
        {
            if (!Directory.Exists(folder))
                return null;

            var files = Directory.GetFiles(folder, pattern);
            if (files.Length == 0)
                return null;

            return files.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).First();
        }

        public List<string> GetFilesByPattern(string folder, string pattern)
        {
            if (!Directory.Exists(folder))
                return new List<string>();

            return Directory.GetFiles(folder, pattern).ToList();
        }

        #endregion

        #region 部分覆盖

        public async Task<bool> ApplyPartialOverwriteAsync(
            string sourceDatPath,
            string targetDatPath,
            List<SettingItem> selectedSettings,
            Action<string> logAction = null)
        {
            if (selectedSettings == null || selectedSettings.Count == 0)
            {
                logAction?.Invoke("错误: 未选择任何设置项");
                return false;
            }

            try
            {
                if (!System.IO.File.Exists(sourceDatPath))
                {
                    logAction?.Invoke($"错误: 源文件不存在 {sourceDatPath}");
                    return false;
                }
                if (!System.IO.File.Exists(targetDatPath))
                {
                    logAction?.Invoke($"错误: 目标文件不存在 {targetDatPath}");
                    return false;
                }

                logAction?.Invoke($"开始部分覆盖，共 {selectedSettings.Count} 类设置");
                logAction?.Invoke($"  源文件: {Path.GetFileName(sourceDatPath)}");
                logAction?.Invoke($"  目标文件: {Path.GetFileName(targetDatPath)}");

                string sourceJson = _marshalService.ReadDatAsJson(sourceDatPath);
                string targetJson = _marshalService.ReadDatAsJson(targetDatPath);

                using var sourceDoc = JsonDocument.Parse(sourceJson);
                using var targetDoc = JsonDocument.Parse(targetJson);

                var modifications = new Dictionary<string, object>();
                int foundCount = 0;

                foreach (var setting in selectedSettings)
                {
                    if (TryGetValueByPath(sourceDoc.RootElement, setting.JsonPath, out JsonElement sourceValue))
                    {
                        modifications[setting.JsonPath] = ConvertJsonElementToObject(sourceValue);
                        foundCount++;
                        logAction?.Invoke($"    找到: {setting.DisplayName} → {setting.JsonPath}");
                    }
                    else
                    {
                        logAction?.Invoke($"    未找到: {setting.DisplayName} → {setting.JsonPath}");
                    }
                }

                if (modifications.Count == 0)
                {
                    logAction?.Invoke("  错误: 未在源文件中找到任何匹配的字段");
                    return false;
                }

                logAction?.Invoke($"  成功提取 {modifications.Count} 个字段");

                string modifiedTargetJson = ApplyModificationsToJson(targetJson, modifications);
                _marshalService.WriteJsonAsDat(modifiedTargetJson, targetDatPath);

                logAction?.Invoke($"部分覆盖完成，共覆盖 {modifications.Count} 类设置");
                return true;
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"  错误: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncSingleFileAsync(string sourcePath, string targetPath, Action<string> logAction = null)
        {
            try
            {
                if (!System.IO.File.Exists(sourcePath))
                {
                    logAction?.Invoke($"错误: 源文件不存在 {sourcePath}");
                    return false;
                }

                logAction?.Invoke($"同步文件: {Path.GetFileName(sourcePath)} → {Path.GetFileName(targetPath)}");

                // 如果是同一个文件，跳过
                if (sourcePath == targetPath)
                {
                    logAction?.Invoke("  跳过: 源文件和目标文件相同");
                    return true;
                }

                System.IO.File.Copy(sourcePath, targetPath, true);
                logAction?.Invoke("  文件覆盖完成");
                return true;
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"  错误: {ex.Message}");
                return false;
            }
        }

        public void DecodeDatToJson(string datPath, string jsonPath)
        {
            _marshalService.DecodeToFile(datPath, jsonPath);
        }

        public void EncodeJsonToDat(string jsonPath, string datPath)
        {
            _marshalService.EncodeFromFile(jsonPath, datPath);
        }

        #endregion

        #region 备份管理

        public string BackupFolder(string sourceFolder, Action<string> logAction = null, string backupBasePath = null)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"文件夹不存在: {sourceFolder}");

            string baseBackupDir = backupBasePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "EVE配置备份");

            if (!Directory.Exists(baseBackupDir))
                Directory.CreateDirectory(baseBackupDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupName = $"EVE_Setting_Backup_{timestamp}";
            string backupPath = Path.Combine(baseBackupDir, backupName);

            logAction?.Invoke($"创建备份: {backupName}");
            CopyDirectory(sourceFolder, backupPath);
            logAction?.Invoke($"备份完成: {backupPath}");

            return backupPath;
        }

        public List<BackupFolderInfo> GetBackupFolders(string backupBasePath = null)
        {
            string baseBackupDir = backupBasePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "EVE配置备份");
            var result = new List<BackupFolderInfo>();

            if (!Directory.Exists(baseBackupDir))
                return result;

            foreach (string dir in Directory.GetDirectories(baseBackupDir))
            {
                string name = Path.GetFileName(dir);
                if (name.StartsWith("EVE_Setting_Backup_"))
                {
                    result.Add(new BackupFolderInfo
                    {
                        Name = name,
                        Path = dir,
                        CreatedAt = Directory.GetLastWriteTime(dir),
                        FileCount = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length,
                        IsFile = false
                    });
                }
            }

            foreach (string file in Directory.GetFiles(baseBackupDir, "*.dat"))
            {
                string name = Path.GetFileName(file);
                // 只备份有效的配置文件
                if (Regex.IsMatch(name, @"^core_(user|char)_\d+\.dat$"))
                {
                    result.Add(new BackupFolderInfo
                    {
                        Name = name,
                        Path = file,
                        CreatedAt = System.IO.File.GetLastWriteTime(file),
                        FileCount = 1,
                        IsFile = true
                    });
                }
            }

            return result.OrderByDescending(b => b.CreatedAt).ToList();
        }

        public int DeleteAllBackups(Action<string> logAction = null, string backupBasePath = null)
        {
            var backups = GetBackupFolders(backupBasePath);
            int deleted = 0;

            foreach (var backup in backups)
            {
                try
                {
                    if (backup.IsFile)
                    {
                        System.IO.File.Delete(backup.Path);
                    }
                    else
                    {
                        Directory.Delete(backup.Path, true);
                    }
                    deleted++;
                    logAction?.Invoke($"  已删除: {backup.Name}");
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"  删除失败: {backup.Name} - {ex.Message}");
                }
            }

            return deleted;
        }

        #endregion

        #region JSON 操作辅助方法

        private bool TryGetValueByPath(JsonElement root, string path, out JsonElement value)
        {
            value = default;
            if (string.IsNullOrEmpty(path)) return false;

            string[] segments = path.Split('.');
            JsonElement current = root;

            foreach (string segment in segments)
            {
                try
                {
                    if (current.ValueKind == JsonValueKind.Object)
                    {
                        if (current.TryGetProperty(segment, out JsonElement next))
                        {
                            current = next;
                            continue;
                        }

                        if (segment.StartsWith("bytes:"))
                        {
                            string key = segment.Substring(6);
                            if (current.TryGetProperty(key, out JsonElement next2))
                            {
                                current = next2;
                                continue;
                            }
                        }

                        string prefixedKey = segment.StartsWith("bytes:") ? segment : $"bytes:{segment}";
                        if (current.TryGetProperty(prefixedKey, out JsonElement next3))
                        {
                            current = next3;
                            continue;
                        }
                    }

                    if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out int index))
                    {
                        if (index >= 0 && index < current.GetArrayLength())
                        {
                            current = current[index];
                            continue;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            value = current;
            return true;
        }

        private object ConvertJsonElementToObject(JsonElement element)
        {
            return ConvertToModifiableObject(element);
        }

        private object ConvertToModifiableObject(JsonElement element)
        {
            try
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in element.EnumerateObject())
                        {
                            dict[prop.Name] = ConvertToModifiableObject(prop.Value);
                        }
                        return dict;

                    case JsonValueKind.Array:
                        var list = new List<object>();
                        foreach (var item in element.EnumerateArray())
                        {
                            list.Add(ConvertToModifiableObject(item));
                        }
                        return list;

                    case JsonValueKind.String:
                        return element.GetString() ?? string.Empty;

                    case JsonValueKind.Number:
                        if (element.TryGetInt64(out long longVal))
                            return longVal;
                        if (element.TryGetDouble(out double doubleVal))
                            return doubleVal;
                        return element.GetRawText();

                    case JsonValueKind.True:
                        return true;

                    case JsonValueKind.False:
                        return false;

                    case JsonValueKind.Null:
                        return null;

                    default:
                        return element.GetRawText();
                }
            }
            catch
            {
                return element.GetRawText();
            }
        }

        private string ApplyModificationsToJson(string originalJson, Dictionary<string, object> modifications)
        {
            try
            {
                using var doc = JsonDocument.Parse(originalJson);
                var root = doc.RootElement;

                var modifiableRoot = ConvertToModifiableObject(root);

                foreach (var kvp in modifications)
                {
                    SetValueByPath(modifiableRoot, kvp.Key, kvp.Value);
                }

                return JsonSerializer.Serialize(modifiableRoot, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"应用修改到 JSON 失败: {ex.Message}", ex);
            }
        }

        private void SetValueByPath(object root, string path, object value)
        {
            if (root == null) return;

            string[] segments = path.Split('.');
            object current = root;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                string segment = segments[i];
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.ContainsKey(segment))
                    {
                        dict[segment] = new Dictionary<string, object>();
                    }
                    current = dict[segment];
                }
                else
                {
                    return;
                }
            }

            string lastSegment = segments[segments.Length - 1];
            if (current is Dictionary<string, object> lastDict)
            {
                lastDict[lastSegment] = value;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 检查 prefs.ini 中是否已开启舰船标签显示（bracketsAlwaysShowShipText=1）
        /// </summary>
        public bool IsShipTagEnabled(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return false;

            string iniPath = Path.Combine(folder, "prefs.ini");
            if (!System.IO.File.Exists(iniPath))
                return false;

            foreach (string line in System.IO.File.ReadAllLines(iniPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("bracketsAlwaysShowShipText", StringComparison.OrdinalIgnoreCase))
                {
                    int eq = trimmed.IndexOf('=');
                    if (eq >= 0 && trimmed.Substring(eq + 1).Trim() == "1")
                        return true;
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 设置 prefs.ini 的舰船标签显示开关（1开/0关）
        /// 已存在该键则更新值，不存在则在文件最顶部插入（不重复添加）
        /// </summary>
        public void SetShipTag(string folder, bool enabled)
        {
            if (string.IsNullOrEmpty(folder))
                return;

            string iniPath = Path.Combine(folder, "prefs.ini");
            string value = enabled ? "1" : "0";
            string newLine = "bracketsAlwaysShowShipText=" + value;

            if (!System.IO.File.Exists(iniPath))
            {
                // 文件不存在：创建并写入
                System.IO.File.WriteAllText(iniPath, newLine + Environment.NewLine);
                return;
            }

            var lines = System.IO.File.ReadAllLines(iniPath).ToList();
            int index = lines.FindIndex(l =>
                l.TrimStart().StartsWith("bracketsAlwaysShowShipText", StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                lines[index] = newLine;
            }
            else
            {
                // ★★★ 最上面一排插入 ★★★
                lines.Insert(0, newLine);
            }

            System.IO.File.WriteAllLines(iniPath, lines);
        }

        public void CopyDirectory(string sourceDir, string destDir, bool overwrite = true)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                System.IO.File.Copy(file, destFile, overwrite);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir, overwrite);
            }
        }

        public string GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
                return fullPath;

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            return fullPath.Substring(basePath.Length).TrimStart('\\', '/');
        }

        #endregion
    }
}
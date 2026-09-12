using EVEBox.Core;
using EVEBox.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EVEBox.Core.Services.File
{
    public class FileListService
    {
        private readonly HttpClient _httpClient;
        private string _currentServer;
        private readonly Action<string, string, string> _logAction;
        private readonly Action _onUpdate;

        public FileListService(
            HttpClient httpClient,
            string currentServer,
            Action<string, string, string> logAction,
            Action onUpdate)
        {
            _httpClient = httpClient;
            _currentServer = currentServer;
            _logAction = logAction;
            _onUpdate = onUpdate;
        }

        public void UpdateServer(string server)
        {
            _currentServer = server;
        }

        public (List<UserFileItem> users, List<CharacterFileItem> chars) ScanFolder(string folder)
        {
            var users = new List<UserFileItem>();
            var chars = new List<CharacterFileItem>();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return (users, chars);

            foreach (string file in Directory.GetFiles(folder))
            {
                string fileName = Path.GetFileName(file);

                var userMatch = Regex.Match(fileName, @"^core_user_(\d+)\.dat$");
                if (userMatch.Success)
                {
                    string userId = userMatch.Groups[1].Value;
                    users.Add(new UserFileItem
                    {
                        FileName = fileName,
                        UserId = userId,
                        FilePath = file,
                        ModifyTime = System.IO.File.GetLastWriteTime(file),
                        DisplayName = userId
                    });
                    continue;
                }

                var charMatch = Regex.Match(fileName, @"^core_char_(\d+)\.dat$");
                if (charMatch.Success)
                {
                    string charId = charMatch.Groups[1].Value;
                    string displayName = GetCharacterDisplayName(charId);

                    chars.Add(new CharacterFileItem
                    {
                        FileName = fileName,
                        CharacterId = charId,
                        CharacterName = displayName,
                        FilePath = file,
                        ModifyTime = System.IO.File.GetLastWriteTime(file)
                    });
                }
            }

            // ★★★ 默认按修改时间递减排序（最新在前）★★★
            return (users.OrderByDescending(u => u.ModifyTime).ToList(),
                    chars.OrderByDescending(c => c.ModifyTime).ToList());
        }

        public string GetUserDisplayName(string userId)
        {
            return userId;
        }

        public string GetCharacterDisplayName(string charId)
        {
            string cachedName = CharacterCacheManager.GetCachedName(charId);
            if (!string.IsNullOrEmpty(cachedName))
                return cachedName;
            return charId;
        }

        public async Task<Dictionary<string, string>> BatchQueryCharacterNamesAsync(
            List<string> ids,
            Action<int, string> onNameReceived)
        {
            var result = new Dictionary<string, string>();
            int batchSize = 20;

            var idsToQuery = new List<string>();
            foreach (var id in ids)
            {
                string cachedName = CharacterCacheManager.GetCachedName(id);
                if (!string.IsNullOrEmpty(cachedName))
                {
                    result[id] = cachedName;
                    if (int.TryParse(id, out int intId))
                        onNameReceived?.Invoke(intId, cachedName);
                }
                else
                {
                    idsToQuery.Add(id);
                }
            }

            if (idsToQuery.Count == 0)
                return result;

            _logAction?.Invoke("查询角色名", $"开始查询 {idsToQuery.Count} 个角色", "");

            for (int i = 0; i < idsToQuery.Count; i += batchSize)
            {
                var batch = idsToQuery.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(id => QueryCharacterNameAsync(id, onNameReceived));
                var batchResults = await Task.WhenAll(tasks);

                foreach (var kvp in batchResults)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        result[kvp.Key] = kvp.Value;
                        CharacterCacheManager.SaveName(kvp.Key, kvp.Value);
                    }
                }

                if (i + batchSize < idsToQuery.Count)
                    await Task.Delay(100);
            }

            return result;
        }

        private async Task<KeyValuePair<string, string>> QueryCharacterNameAsync(
            string charId,
            Action<int, string> onNameReceived)
        {
            const int maxRetries = 1;
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var server = ServerInfo.GetByDisplayName(_currentServer);
                    string url = $"{server.EsiBaseUrl}/latest/characters/{charId}/?datasource={server.DataSource}";

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("User-Agent", "EVEConfigManager/1.0");
                    request.Headers.Add("Accept", "application/json");

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        string name = doc.RootElement.GetProperty("name").GetString();

                        if (!string.IsNullOrEmpty(name) && int.TryParse(charId, out int id))
                        {
                            onNameReceived?.Invoke(id, name);
                            return new KeyValuePair<string, string>(charId, name);
                        }
                    }
                    else
                    {
                        // HTTP 4xx 不重试（用户不存在等），5xx 需要重试
                        bool shouldRetry = (int)response.StatusCode >= 500 && attempt < maxRetries;
                        _logAction?.Invoke("查询角色名", "失败", $"HTTP {response.StatusCode} - {charId}{(shouldRetry ? " (将重试)" : "")}");
                        if (!shouldRetry) break;
                    }
                }
                catch (TaskCanceledException)
                {
                    if (attempt < maxRetries)
                    {
                        _logAction?.Invoke("查询角色名", "超时", $"{charId} (第{attempt + 1}次，将重试)");
                        await Task.Delay(500);
                        continue;
                    }
                    _logAction?.Invoke("查询角色名", "超时", charId);
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                    {
                        _logAction?.Invoke("查询角色名", "异常", $"{charId}: {ex.Message} (将重试)");
                        await Task.Delay(500);
                        continue;
                    }
                    _logAction?.Invoke("查询角色名", "异常", $"{charId}: {ex.Message}");
                }
                break;
            }

            return new KeyValuePair<string, string>(charId, null);
        }

        public string GetCharFileDisplayName(string fileName)
        {
            var match = Regex.Match(fileName, @"^core_char_(\d+)\.dat$");
            if (!match.Success)
                return fileName;

            string charId = match.Groups[1].Value;
            return GetCharacterDisplayName(charId);
        }

        public string GetUserFileDisplayName(string fileName)
        {
            var match = Regex.Match(fileName, @"^core_user_(\d+)\.dat$");
            if (!match.Success)
                return fileName;

            string userId = match.Groups[1].Value;
            return userId;
        }
    }

    public class UserFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime ModifyTime { get; set; }
        public int RowIndex { get; set; }
    }

    public class CharacterFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string CharacterId { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime ModifyTime { get; set; }
        public int RowIndex { get; set; }
    }

    public class BackupItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int RowIndex { get; set; }
        public bool IsFile { get; set; }
    }

    public class BackupFolderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int FileCount { get; set; }
        public bool IsFile { get; set; }
    }
}
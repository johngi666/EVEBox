using EVEBox.PeiZhi;
using EVEBox.RiZhi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace EVEBox.PeiZhiTongBu
{
    public class WenJianLieBiaoShuaXinService
    {
        private readonly WenJianLieBiaoService _fileListService;
        private readonly PeiZhiManager _configManager;
        private readonly RiZhiService _logService;
        private readonly Func<string> _getCurrentFolder;
        private readonly Action<Action> _invokeOnUI;

        private bool _isRefreshing = false;

        private List<YongHuWenJianXiang> _userFileItems = new List<YongHuWenJianXiang>();
        private List<JiaoSeWenJianXiang> _charFileItems = new List<JiaoSeWenJianXiang>();
        private List<BeiFenXiang> _backupItems = new List<BeiFenXiang>();

        public IReadOnlyList<YongHuWenJianXiang> UserFileItems => _userFileItems.AsReadOnly();
        public IReadOnlyList<JiaoSeWenJianXiang> CharFileItems => _charFileItems.AsReadOnly();
        public IReadOnlyList<BeiFenXiang> BackupItems => _backupItems.AsReadOnly();
        public bool IsRefreshing => _isRefreshing;

        public WenJianLieBiaoShuaXinService(
            WenJianLieBiaoService fileListService,
            PeiZhiManager configManager,
            RiZhiService logService,
            Func<string> getCurrentFolder,
            Action<Action> invokeOnUI)
        {
            _fileListService = fileListService;
            _configManager = configManager;
            _logService = logService;
            _getCurrentFolder = getCurrentFolder;
            _invokeOnUI = invokeOnUI;
        }

        public async Task RefreshFileListAsync(
            Action<DataGridView, List<YongHuWenJianXiang>> updateUserGrid,
            Action<DataGridView, List<JiaoSeWenJianXiang>> updateCharGrid,
            Action<int, int, int> updateStatusLabels,
            Action<List<BeiFenXiang>> updateBackupList = null)
        {
            if (_isRefreshing)
            {
                _logService.Log("刷新文件列表", "跳过", "正在刷新中");
                return;
            }

            _isRefreshing = true;

            try
            {
                string currentFolder = _getCurrentFolder?.Invoke();
                if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
                    return;

                var (users, chars) = _fileListService.ScanFolder(currentFolder);

                // ★★★ 应用用户备注到 DisplayName ★★★
                var remarks = _configManager.GetUserRemarks();
                foreach (var user in users)
                {
                    if (remarks != null && remarks.TryGetValue(user.UserId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                    {
                        user.DisplayName = remark;
                    }
                    else
                    {
                        user.DisplayName = user.UserId;
                    }
                }

                _userFileItems = users;
                _invokeOnUI?.Invoke(() =>
                {
                    updateUserGrid?.Invoke(null, users);
                });

                var idsToQuery = new List<string>();
                foreach (var item in chars)
                {
                    if (string.IsNullOrEmpty(item.CharacterName) || item.CharacterName == item.CharacterId)
                    {
                        item.CharacterName = "查询中...";
                        idsToQuery.Add(item.CharacterId);
                    }
                }
                _charFileItems = chars;
                _invokeOnUI?.Invoke(() =>
                {
                    updateCharGrid?.Invoke(null, chars);
                });

                if (idsToQuery.Count > 0)
                {
                    _logService.Log("查询角色名", $"开始查询 {idsToQuery.Count} 个角色", "");

                    var names = await _fileListService.BatchQueryCharacterNamesAsync(
                        idsToQuery,
                        (characterId, characterName) =>
                        {
                            UpdateCharacterNameInList(characterId, characterName, updateCharGrid);
                        }
                    );

                    JiaoSeHuanCunManager.SaveNames(names);
                    _configManager.Save();
                }

                _invokeOnUI?.Invoke(() =>
                {
                    updateStatusLabels?.Invoke(users.Count, chars.Count, _backupItems.Count);
                });

                _logService.Log("刷新文件列表", "成功", $"用户: {users.Count}, 角色: {chars.Count}");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public void UpdateCharacterNameInList(
            int characterId,
            string characterName,
            Action<DataGridView, List<JiaoSeWenJianXiang>> updateCharGrid)
        {
            _invokeOnUI?.Invoke(() =>
            {
                for (int i = 0; i < _charFileItems.Count; i++)
                {
                    var item = _charFileItems[i];
                    if (item.CharacterId == characterId.ToString())
                    {
                        item.CharacterName = characterName;
                        break;
                    }
                }
                updateCharGrid?.Invoke(null, _charFileItems);
            });
        }

        public void RefreshBackupList(
            Action<DataGridView, List<BeiFenXiang>> updateBackupGrid,
            WenJianTongBuManager fileSyncManager)
        {
            // ★★★ 使用配置的备份路径（与写入保持一致，避免自定义路径时读空）★★★
            var backups = fileSyncManager.GetBackupFolders(_configManager.GetBackupPath());

            _backupItems.Clear();
            foreach (var backup in backups.OrderByDescending(b => b.CreatedAt))
            {
                string displayName;

                if (backup.IsFile)
                {
                    displayName = GetBackupFileDisplayName(backup.Name);
                }
                else
                {
                    displayName = $"📁 {backup.Name}";
                }

                _backupItems.Add(new BeiFenXiang
                {
                    Name = backup.Name,
                    DisplayName = displayName,
                    CreatedAt = backup.CreatedAt,
                    Path = backup.Path,
                    IsFile = backup.IsFile,
                    RowIndex = _backupItems.Count
                });
            }

            _invokeOnUI?.Invoke(() =>
            {
                updateBackupGrid?.Invoke(null, _backupItems);
            });
        }

        private string GetBackupFileDisplayName(string fileName)
        {
            // 解析 core_user_xxx
            var userMatch = Regex.Match(fileName, @"^core_user_(\d+)");
            if (userMatch.Success)
            {
                string userId = userMatch.Groups[1].Value;
                // ★★★ 检查是否有用户备注 ★★★
                var remarks = _configManager.GetUserRemarks();
                if (remarks != null && remarks.TryGetValue(userId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                {
                    return $"📄 {remark} ({userId})";
                }
                return $"📄 用户 {userId}";
            }

            // 解析 core_char_xxx
            var charMatch = Regex.Match(fileName, @"^core_char_(\d+)");
            if (charMatch.Success)
            {
                string charId = charMatch.Groups[1].Value;
                string cachedName = JiaoSeHuanCunManager.GetCachedName(charId);
                if (!string.IsNullOrEmpty(cachedName))
                    return $"📄 {cachedName}";
                return $"📄 角色 {charId}";
            }

            return $"📄 {fileName}";
        }

        public void ClearAllLists(
            Action<DataGridView> clearUserGrid,
            Action<DataGridView> clearCharGrid,
            Action<DataGridView> clearBackupGrid)
        {
            _userFileItems.Clear();
            _charFileItems.Clear();
            _backupItems.Clear();

            _invokeOnUI?.Invoke(() =>
            {
                clearUserGrid?.Invoke(null);
                clearCharGrid?.Invoke(null);
                clearBackupGrid?.Invoke(null);
            });
        }

        /// <summary>
        /// ★★★ 刷新用户备注显示（外部调用） ★★★
        /// </summary>
        public void RefreshUserRemarks(Action<DataGridView, List<YongHuWenJianXiang>> updateUserGrid)
        {
            var remarks = _configManager.GetUserRemarks();
            foreach (var user in _userFileItems)
            {
                if (remarks != null && remarks.TryGetValue(user.UserId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                {
                    user.DisplayName = remark;
                }
                else
                {
                    user.DisplayName = user.UserId;
                }
            }

            _invokeOnUI?.Invoke(() =>
            {
                updateUserGrid?.Invoke(null, _userFileItems);
            });
        }
    }
}
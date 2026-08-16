using EVESyncTool.Core;
using EVESyncTool.Core.Config;
using EVESyncTool.Core.Mapping;
using EVESyncTool.Core.Services;
using EVESyncTool.Core.Services.Backup;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Core.Services.Folder;
using EVESyncTool.Core.Services.Grid;
using EVESyncTool.Core.Services.Log;
using EVESyncTool.Core.Services.ServerStatus;
using EVESyncTool.Core.Services.Sync;
using EVESyncTool.Core.Services.Update;
using EVESyncTool.Core.UI;
using EVESyncTool.Dialogs;
using EVESyncTool.Dialogs.Common;
using EVESyncTool.Dialogs.Config;
using EVESyncTool.Dialogs.Info;
using EVESyncTool.Dialogs.Progress;
using EVESyncTool.Dialogs.Sync;
using EVESyncTool.Core.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESyncTool
{
    public partial class MainForm : Form
    {
        private readonly ConfigManager _configManager;
        private readonly LogService _logService;
        private readonly FolderService _folderService;
        private readonly FileListRefreshService _fileListRefreshService;
        private readonly FileListService _fileListService;
        private readonly BackupService _backupService;
        private readonly SyncService _syncService;
        private readonly DataGridViewHandler _dataGridViewHandler;
        private readonly ServerStatusManager _serverStatusManager;
        private readonly UpdateDownloader _updateDownloader;
        private readonly UpdateService _updateService;

        private readonly LeftPanelBuilder _leftPanel;
        private readonly RightPanelBuilder _rightPanel;
        private readonly TitleBarBuilder _titleBarBuilder;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private string _currentServer = "曙光服 (Infinity)";
        private string _currentFolder;

        private List<UserFileItem> _userFileItems = new List<UserFileItem>();
        private List<CharacterFileItem> _charFileItems = new List<CharacterFileItem>();
        private List<BackupItem> _backupItems = new List<BackupItem>();

        private HelpForm _helpForm;
        private LogForm _logForm;

        // 运行期间检查更新（每分钟一次，最多 10 次后停止）
        private readonly System.Windows.Forms.Timer _updateCheckTimer;
        private int _updateCheckCount = 1; // 启动立即检查算第 1 次

        public string CurrentFolder => _currentFolder;

        public MainForm()
        {
            _configManager = new ConfigManager();
            _currentServer = _configManager.GetLastServer();

            _logService = new LogService();

            var folderFinder = new FolderFinder(ServerInfo.ToKeywordMap(), _logService.Log, null);

            _updateDownloader = new UpdateDownloader(_httpClient);
            _updateService = new UpdateService(_httpClient, _updateDownloader, _logService.Log, this);

            _fileListService = new FileListService(
                _httpClient,
                _currentServer,
                _logService.Log,
                null
            );

            var fileSyncManager = new FileSyncManager();

            _fileListRefreshService = new FileListRefreshService(
                _fileListService,
                _configManager,
                _logService,
                () => _currentFolder,
                action => action.Invoke()
            );

            _folderService = new FolderService(
                _configManager,
                folderFinder,
                _logService,
                async (folder) => await LoadConfigFilesAsync(folder)
            );
            _folderService.SetCurrentServer(_currentServer);

            _backupService = new BackupService(
                fileSyncManager,
                _logService,
                _configManager,
                () => _currentFolder,
                action => action.Invoke(),
                () => RefreshBackupList()
            );

            _syncService = new SyncService(
                fileSyncManager,
                null,
                _configManager,
                _logService.Log
            );

            _serverStatusManager = new ServerStatusManager(_httpClient, _currentFolder, _logService.Log);

            _dataGridViewHandler = new DataGridViewHandler(
                _backupService,
                _syncService,
                () => _currentFolder,
                (item) => ShowUserSyncDialog((UserFileItem)item),
                (item) => ShowCharSyncDialog((CharacterFileItem)item)
            );

            _leftPanel = new LeftPanelBuilder();
            _rightPanel = new RightPanelBuilder();
            _titleBarBuilder = new TitleBarBuilder(this);

            InitializeComponent();

            // 加载并应用上次的主题模式（保存于 evesync_config.json 的 UseDarkMode）
            ThemeManager.SetDarkMode(_configManager.Config.UseDarkMode);
            ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme(ThemeManager.IsDarkMode);

            _serverStatusManager.SetStatusLabels(
                _leftPanel.LblInfinityStatus,
                _leftPanel.LblSerenityStatus,
                _leftPanel.LblTranquilityStatus
            );

            BindEvents();

            _leftPanel.CmbServer.SelectedItem = _currentServer;

            _logService.Log("程序启动", "成功", "");
            _ = AutoFindFolderAsync();
            _serverStatusManager.Start();

            // 启动时立即检查一次，之后每分钟再查（网络不稳定时尽快捕获到新版本）
            _updateCheckTimer = new System.Windows.Forms.Timer();
            _updateCheckTimer.Interval = 60 * 1000;
            _updateCheckTimer.Tick += async (s, e) =>
            {
                // 检测满 10 次后停止，直到下次启动
                if (_updateCheckCount >= 10)
                {
                    _updateCheckTimer.Stop();
                    return;
                }
                _updateCheckCount++;
                await _updateService.CheckForUpdatesAsync();
            };
            _updateCheckTimer.Start();
            _ = _updateService.CheckForUpdatesAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "EVE配置管理工具";
            this.Size = new Size(1100, 588);
            this.MinimumSize = new Size(1050, 588);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 248, 255);

            this.Controls.Add(_titleBarBuilder.Build());
            _titleBarBuilder.BtnHelp.Click += BtnHelp_Click;
            _titleBarBuilder.BtnLog.Click += BtnLog_Click;
            _titleBarBuilder.BtnCheckUpdate.Click += BtnCheckUpdate_Click;
            _titleBarBuilder.BtnTheme.Click += BtnTheme_Click;
            // 设置按钮已移除
            // _titleBarBuilder.BtnSettings.Click += BtnSettings_Click;

            TableLayoutPanel mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(15, 0, 15, 0),
                Location = new Point(0, 35)
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            mainContainer.Controls.Add(_leftPanel.Build(), 0, 0);
            mainContainer.Controls.Add(_rightPanel.Build(), 1, 0);

            this.Controls.Add(mainContainer);

            this.FormClosing += (s, e) =>
            {
                _serverStatusManager?.Stop();
                _updateCheckTimer?.Stop();
                _configManager.FlushSave(); // 确保防抖中的配置（如新加的配置方案）写入磁盘
            };
        }

        private void BindEvents()
        {
            _leftPanel.CmbServer.SelectedIndexChanged += async (s, e) =>
            {
                string newServer = _leftPanel.CmbServer.SelectedItem?.ToString() ?? "曙光服 (Infinity)";
                await OnServerChanged(newServer);
            };

            _leftPanel.BtnOpenFolder.Click += (s, e) => _folderService.OpenCurrentFolder();
            _leftPanel.BtnLoadDefault.Click += async (s, e) => await _folderService.LoadDefaultFolderAsync();
            _leftPanel.BtnSelectFolder.Click += async (s, e) => await _folderService.ManualSelectFolderAsync(this);
            _leftPanel.BtnVersionManage.Click += async (s, e) => await OpenVersionManage();
            _leftPanel.BtnBackup.Click += (s, e) => _backupService.PerformBackup();
            _leftPanel.BtnDeleteAllBackups.Click += (s, e) => _backupService.DeleteAllBackups();

            // 快捷覆盖
            _leftPanel.BtnSync.Click += async (s, e) =>
            {
                var result = CustomMessageBox.Show(
                    "确定要执行快捷覆盖吗？\n\n" +
                    "将用当前最新的配置文件覆盖所有其他配置文件。\n" +
                    "建议先点击「备份当前配置」进行备份。",
                    "确认覆盖",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    await _syncService.SyncAllFilesAsync(
                        () => _currentFolder,
                        (folder) => { _currentFolder = folder; },
                        async () => await RefreshFileListAsync(),
                        (msg) => _logService.Log(msg)
                    );
                }
            };

            // ★★★ 绑定用户备注编辑事件 ★★★
            _rightPanel.UserRemarkEdited += OnUserRemarkEdited;

            _rightPanel.DgvUserFiles.CellClick += (s, e) => _dataGridViewHandler.OnUserFileCellClick(s as DataGridView, e);
            _rightPanel.DgvCharFiles.CellClick += (s, e) => _dataGridViewHandler.OnCharFileCellClick(s as DataGridView, e);
            _rightPanel.DgvBackups.CellClick += (s, e) => _dataGridViewHandler.OnBackupCellClick(s as DataGridView, e);
        }

        // ===== 用户备注编辑事件处理 =====
        private void OnUserRemarkEdited(object sender, UserRemarkEditEventArgs e)
        {
            if (string.IsNullOrEmpty(e.UserId)) return;

            // 保存备注
            _configManager.SaveUserRemark(e.UserId, e.Remark);

            // 更新显示
            string displayName = _configManager.GetUserDisplayName(e.UserId);
            _rightPanel.UpdateUserRemarkDisplay(e.UserId, displayName);

            // 更新本地列表中的DisplayName
            foreach (var item in _userFileItems)
            {
                if (item.UserId == e.UserId)
                {
                    item.DisplayName = displayName;
                    break;
                }
            }

            _logService.Log("用户备注", string.IsNullOrEmpty(e.Remark) ? "删除" : "更新", $"{e.UserId} → {e.Remark}");
        }

        private async Task OnServerChanged(string newServer)
        {
            _currentServer = newServer;
            _configManager.SaveLastServer(newServer);

            // ★ 同步角色名查询的服务器，否则会用旧服务器查询新服务器的角色 → 一直"查询中"
            _fileListService.UpdateServer(newServer);

            await _folderService.SwitchServerAsync(
                newServer,
                (server) => ShowSearchFailDialog()
            );

            _logService.Log("切换服务器", "完成", newServer);
        }

        private async Task AutoFindFolderAsync()
        {
            _leftPanel.BtnOpenFolder.Text = "正在查找...";
            _leftPanel.BtnOpenFolder.Enabled = false;

            await _folderService.AutoFindFolderAsync(() => { });

            UpdateFolderButtonState();
        }

        private async Task LoadConfigFilesAsync(string folder)
        {
            _currentFolder = folder;
            UpdateFolderButtonState();

            await RefreshFileListAsync();
            RefreshBackupList();

            _configManager.Save();
            _logService.Log("加载配置文件", "成功", folder);
        }

        private void UpdateFolderButtonState()
        {
            _folderService.UpdateFolderButtonState((text, enabled) =>
            {
                _leftPanel.BtnOpenFolder.Text = text;
                _leftPanel.BtnOpenFolder.Enabled = enabled;
            });
        }

        private async Task RefreshFileListAsync()
        {
            await _fileListRefreshService.RefreshFileListAsync(
                (grid, items) =>
                {
                    _userFileItems = items;
                    _rightPanel.DgvUserFiles.Rows.Clear();

                    // ★★★ 获取用户备注 ★★★
                    var remarks = _configManager.GetUserRemarks();

                    foreach (var item in items)
                    {
                        // 获取显示名（有备注显示备注，无备注显示ID）
                        string displayName = _configManager.GetUserDisplayName(item.UserId);
                        item.DisplayName = displayName;

                        int rowIndex = _rightPanel.DgvUserFiles.Rows.Add(
                            displayName,  // ← 显示备注或ID
                            item.ModifyTime.ToString("MM-dd HH:mm"),
                            "💾",
                            "📂"
                        );

                        // ★★★ 存储数据项到行Tag（排序后仍能对应到正确文件）★★★
                        if (rowIndex >= 0)
                        {
                            _rightPanel.DgvUserFiles.Rows[rowIndex].Tag = item;
                        }
                    }

                    // ★★★ 默认按修改时间递减排序（覆盖上次手动排序状态）★★★
                    _rightPanel.DgvUserFiles.Sort(_rightPanel.DgvUserFiles.Columns[1], ListSortDirection.Descending);
                },
                (grid, items) =>
                {
                    _charFileItems = items;
                    _rightPanel.DgvCharFiles.Rows.Clear();
                    foreach (var item in items)
                    {
                        int rowIndex = _rightPanel.DgvCharFiles.Rows.Add(
                            item.CharacterName ?? item.CharacterId,
                            item.CharacterId,
                            item.ModifyTime.ToString("MM-dd HH:mm"),
                            "💾",
                            "📂"
                        );

                        // ★★★ 存储数据项到行Tag（排序后仍能对应到正确文件）★★★
                        if (rowIndex >= 0)
                        {
                            _rightPanel.DgvCharFiles.Rows[rowIndex].Tag = item;
                        }
                    }

                    // ★★★ 默认按修改时间递减排序（覆盖上次手动排序状态）★★★
                    _rightPanel.DgvCharFiles.Sort(_rightPanel.DgvCharFiles.Columns[2], ListSortDirection.Descending);
                },
                (userCount, charCount, backupCount) =>
                {
                    _rightPanel.LblUserTitle.Text = $"用户配置文件 ({userCount}个文件)";
                    _rightPanel.LblCharTitle.Text = $"角色配置文件 ({charCount}个文件)";
                    _rightPanel.LblBackupTitle.Text = $"备份管理 ({backupCount}个备份)";
                }
            );
        }

        private void RefreshBackupList()
        {
            _fileListRefreshService.RefreshBackupList(
                (grid, items) =>
                {
                    _backupItems = items;
                    _rightPanel.DgvBackups.Rows.Clear();
                    foreach (var item in items)
                    {
                        int rowIndex = _rightPanel.DgvBackups.Rows.Add(
                            item.DisplayName,
                            item.CreatedAt.ToString("MM-dd HH:mm"),
                            "📂",
                            "↩️",
                            "🗑️"
                        );

                        // ★★★ 存储数据项到行Tag（排序后仍能对应到正确备份）★★★
                        if (rowIndex >= 0)
                        {
                            _rightPanel.DgvBackups.Rows[rowIndex].Tag = item;
                        }
                    }
                },
                new FileSyncManager()
            );
        }

        private void ShowSearchFailDialog()
        {
            using (var dialog = new SearchFailDialog(_currentServer))
            {
                dialog.Owner = this;
                var choice = dialog.ShowDialogAndGetResult();

                switch (choice)
                {
                    case SearchFailDialog.UserChoice.SwitchServer:
                        CustomMessageBox.Show("请从左侧下拉框选择其他服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case SearchFailDialog.UserChoice.DeepSearch:
                        _ = _folderService.DeepSearchAndLoadAsync(this);
                        break;
                    case SearchFailDialog.UserChoice.ManualSelect:
                        _ = _folderService.ManualSelectFolderAsync(this);
                        break;
                    case SearchFailDialog.UserChoice.Cancel:
                        UpdateFolderButtonState();
                        break;
                }
            }
        }

        private async Task OpenVersionManage()
        {
            if (string.IsNullOrEmpty(_currentFolder) || !System.IO.Directory.Exists(_currentFolder))
            {
                CustomMessageBox.Show("请先选择有效的EVE配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new VersionManageDialog(_currentFolder, (msg) => _logService.Log(msg)))
            {
                dialog.OnSchemesChanged += async () => await RefreshFileListAsync();
                dialog.ShowDialog(this);
            }

            _logService.Log("打开配置方案管理", "成功", "");
        }

        private void ShowUserSyncDialog(UserFileItem sourceItem)
        {
            var otherFiles = _userFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                CustomMessageBox.Show("没有其他用户文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建目标列表：显示名（备注优先）+ 用户ID（用于匹配备注）
            var targets = otherFiles.Select(item => new FileTargetItem(
                item.FilePath,
                _configManager.GetUserDisplayName(item.UserId),
                item.UserId
            )).ToList();

            ShowSyncDialog(
                sourceItem.FilePath,
                sourceItem.DisplayName ?? sourceItem.UserId,
                targets,
                _configManager.GetUserRemarks(),
                "同步用户文件");
        }

        private void ShowCharSyncDialog(CharacterFileItem sourceItem)
        {
            var otherFiles = _charFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                CustomMessageBox.Show("没有其他角色文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 角色文件没有备注，DisplayName 为角色名或ID
            var targets = otherFiles.Select(item => new FileTargetItem(
                item.FilePath,
                item.CharacterName ?? item.CharacterId
            )).ToList();

            ShowSyncDialog(
                sourceItem.FilePath,
                sourceItem.CharacterName ?? sourceItem.CharacterId,
                targets,
                null,
                "同步角色文件");
        }

        /// <summary>
        /// 通用同步对话框：选择目标 → 复制 → 刷新列表
        /// </summary>
        private void ShowSyncDialog(
            string sourceFilePath,
            string sourceDisplayName,
            List<FileTargetItem> targets,
            Dictionary<string, string> remarks,
            string operationName)
        {
            using (var fileDialog = new SyncDialog(
                sourceDisplayName,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                remarks))
            {
                if (fileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int count = _syncService.CopyFileToTargets(sourceFilePath, fileDialog.SelectedTargets, operationName);
                _ = RefreshFileListAsync();
                CustomMessageBox.Show($"同步完成，共同步 {count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            if (_helpForm == null || _helpForm.IsDisposed)
            {
                _helpForm = new HelpForm();
                _helpForm.Owner = this;
                _helpForm.FormClosed += (s, args) => _helpForm = null;
                _helpForm.Show();
                _logService.Log("打开使用说明", "成功", "");
            }
            else
            {
                _helpForm.Close();
                _helpForm = null;
                _logService.Log("关闭使用说明窗口", "成功", "");
            }
        }

        private void BtnLog_Click(object sender, EventArgs e)
        {
            if (_logForm == null || _logForm.IsDisposed)
            {
                _logForm = new LogForm(_logService.GetLogs().ToList());
                _logForm.Owner = this;
                _logForm.FormClosed += (s, args) => _logForm = null;
                _logForm.Show();
                _logService.Log("查看操作日志", "成功", "");
            }
            else
            {
                _logForm.Close();
                _logForm = null;
                _logService.Log("关闭操作日志窗口", "成功", "");
            }
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            ThemeManager.Toggle();
            _configManager.Config.UseDarkMode = ThemeManager.IsDarkMode;
            _configManager.Save();
            _logService.Log("主题切换", "成功", ThemeManager.IsDarkMode ? "夜间模式" : "日间模式");
        }

        private async void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            _logService.Log("版本检查", "手动触发", "");
            await _updateService.CheckForUpdatesAsync(showResultWhenUpToDate: true);
        }

        private void ApplyTheme(bool isDark)
        {
            // ★★★ 禁用重绘块：标题栏和主体颜色一次性应用、一次性重绘，避免切换闪烁 ★★★
            ThemeManager.BeginThemeUpdate(this);
            try
            {
                _titleBarBuilder.ApplyTheme(isDark);
                ThemeManager.ApplyCore(this);
            }
            finally
            {
                ThemeManager.EndThemeUpdate(this);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    BtnHelp_Click(null, null);
                    return true;
                case Keys.F2:
                    BtnLog_Click(null, null);
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
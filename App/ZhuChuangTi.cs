using EVEBox.BeiFen;
using EVEBox.FuWuQiZhuangTai;
using EVEBox.GengXin;
using EVEBox.GongYong;
using EVEBox.JianChuanBiaoQian;
using EVEBox.JieMian;
using EVEBox.PeiZhi;
using EVEBox.PeiZhiFangAn;
using EVEBox.PeiZhiTongBu;
using EVEBox.QiTaGongJu;
using EVEBox.RiZhi;
using EVEBox.WenJianJia;
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



namespace EVEBox.App
{
    public partial class ZhuChuangTi : Form
    {
        private readonly PeiZhiManager _configManager;
        private readonly RiZhiService _logService;
        private readonly WenJianJiaService _folderService;
        private readonly WenJianLieBiaoShuaXinService _fileListRefreshService;
        private readonly WenJianLieBiaoService _fileListService;
        private readonly BeiFenService _backupService;
        private readonly TongBuService _syncService;
        private readonly BiaoGeHandler _dataGridViewHandler;
        private readonly FuWuQiZhuangTaiManager _serverStatusManager;
        private readonly GengXinDownloader _updateDownloader;
        private readonly GengXinService _updateService;

        private readonly ZuoCeMianBanBuilder _leftPanel;
        private readonly YouMianBanBuilder _rightPanel;
        private readonly BiaoTiLanBuilder _titleBarBuilder;
        private readonly PeiZhiFangAnView _schemeView;

        // ===== 标签页内容区 =====
        private readonly Panel _contentHost = new Panel();
        private readonly Panel _panelSync = new Panel();
        private readonly Panel _panelBackup = new Panel();
        private readonly Panel _panelScheme = new Panel();
        private readonly Panel _panelHelp = new Panel();
        private readonly Panel _panelLog = new Panel();
        private readonly Panel _panelUpdate = new Panel();
        private readonly Panel _panelTools = new Panel();

        // 配置同步标签控件
        private readonly ComboBox _cmbServer = new ComboBox();
        private readonly Button _btnOpenFolder = new Button();
        private readonly Button _btnLoadDefault = new Button();
        private readonly Button _btnSelectFolder = new Button();
        private readonly Button _btnSync = new Button();

        // 更新标签控件
        private readonly Button _btnCheckUpdate = new Button();
        private readonly Button _btnGithub = new Button();
        private readonly Button _btnGitee = new Button();

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private string _currentServer = "曙光服 (Infinity)";
        private string _currentFolder;

        private List<YongHuWenJianXiang> _userFileItems = new List<YongHuWenJianXiang>();
        private List<JiaoSeWenJianXiang> _charFileItems = new List<JiaoSeWenJianXiang>();
        private List<BeiFenXiang> _backupItems = new List<BeiFenXiang>();

        private RichTextBox _rtbLog;
        private System.Windows.Forms.Timer _logRefreshTimer;
        private int _lastLogCount;

        // 运行期间检查更新（每分钟一次，最多 10 次后停止）
        private readonly System.Windows.Forms.Timer _updateCheckTimer;
        private int _updateCheckCount = 1; // 启动立即检查算第 1 次

        public string CurrentFolder => _currentFolder;

        public ZhuChuangTi()
        {
            _configManager = new PeiZhiManager();
            _currentServer = _configManager.GetLastServer();

            _logService = new RiZhiService();

            var folderFinder = new WenJianJiaFinder(FuWuQiXinXi.ToKeywordMap(), _logService.Log, null);

            _updateDownloader = new GengXinDownloader(_httpClient);
            _updateService = new GengXinService(_httpClient, _updateDownloader, _logService.Log, this);

            _fileListService = new WenJianLieBiaoService(
                _httpClient,
                _currentServer,
                _logService.Log,
                null
            );

            var fileSyncManager = new WenJianTongBuManager();

            _fileListRefreshService = new WenJianLieBiaoShuaXinService(
                _fileListService,
                _configManager,
                _logService,
                () => _currentFolder,
                action => action.Invoke()
            );

            _folderService = new WenJianJiaService(
                _configManager,
                folderFinder,
                _logService,
                async (folder) => await LoadConfigFilesAsync(folder)
            );
            _folderService.SetCurrentServer(_currentServer);

            _backupService = new BeiFenService(
                fileSyncManager,
                _logService,
                _configManager,
                () => _currentFolder,
                action => action.Invoke(),
                () => RefreshBackupList(),
                () => { _ = RefreshFileListAsync(); }
            );

            _syncService = new TongBuService(
                fileSyncManager,
                null,
                _configManager,
                _logService.Log
            );

            _serverStatusManager = new FuWuQiZhuangTaiManager(_httpClient, _currentFolder, _logService.Log);

            _dataGridViewHandler = new BiaoGeHandler(
                _backupService,
                _syncService,
                () => _currentFolder,
                (item) => ShowUserSyncDialog((YongHuWenJianXiang)item),
                (item) => ShowCharSyncDialog((JiaoSeWenJianXiang)item)
            );

            _leftPanel = new ZuoCeMianBanBuilder();
            _rightPanel = new YouMianBanBuilder();
            _titleBarBuilder = new BiaoTiLanBuilder(this);
            _schemeView = new PeiZhiFangAnView(msg => _logService.Log(msg));

            InitializeComponent();

            // 加载并应用上次的主题模式（保存于 evesync_config.json 的 UseDarkMode）
            ZhuTiManager.SetDarkMode(_configManager.Config.UseDarkMode);
            ZhuTiManager.ThemeChanged += ApplyTheme;
            ApplyTheme(ZhuTiManager.IsDarkMode);

            _serverStatusManager.SetStatusLabels(
                _leftPanel.LblInfinityStatus,
                _leftPanel.LblSerenityStatus,
                _leftPanel.LblTranquilityStatus
            );

            BindEvents();

            _cmbServer.SelectedItem = _currentServer;

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
            this.Text = "EVE BOX";
            this.Size = new Size(950, 588);
            this.MinimumSize = new Size(950, 588);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 248, 255);

            // 注意：WinForms 按 Controls 逆序处理停靠——标题栏必须最后添加，
            // 否则 Dock=Fill 的内容区先占满全屏，标题栏会覆盖在内容区顶部
            BuildTabPanels();

            TableLayoutPanel mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(15, 0, 15, 0),
                Location = new Point(0, 35)
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            mainContainer.Controls.Add(_leftPanel.Build(), 0, 0);
            mainContainer.Controls.Add(_contentHost, 1, 0);

            this.Controls.Add(mainContainer);
            this.Controls.Add(_titleBarBuilder.Build());
            _leftPanel.BtnTheme.Click += BtnTheme_Click;
            _leftPanel.BtnShipTag.Click += BtnShipTag_Click;

            // ★★★ 标签切换 ★★★
            _leftPanel.TabSelected += OnTabSelected;
            ShowTab(ZuoCeMianBanBuilder.TabSync);

            this.FormClosing += (s, e) =>
            {
                _serverStatusManager?.Stop();
                _updateCheckTimer?.Stop();
                _configManager.FlushSave(); // 确保防抖中的配置（如新加的配置方案）写入磁盘
            };
        }

        /// <summary>
        /// 构建 7 个标签页内容面板
        /// </summary>
        private void BuildTabPanels()
        {
            // ===== 配置同步：顶部工具条 + 用户/角色表格 =====
            _panelSync.Dock = DockStyle.Fill;
            _panelSync.BackColor = Color.White;

            FlowLayoutPanel syncTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(6, 3, 6, 3),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            Label lblServer = new Label
            {
                Text = "服务器:",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Margin = new Padding(0, 13, 0, 0)
            };
            syncTop.Controls.Add(lblServer);

            _cmbServer.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbServer.Font = new Font("Microsoft YaHei", 10);
            _cmbServer.Size = new Size(150, 36);
            _cmbServer.Margin = new Padding(0, 10, 10, 0);
            _cmbServer.Items.AddRange(new object[] { "曙光服 (Infinity)", "晨曦服 (Serenity)", "国际服 (Tranquility)" });
            syncTop.Controls.Add(_cmbServer);

            syncTop.Controls.Add(CreateTabButton(_btnOpenFolder, "未识别到可用配置", Color.FromArgb(70, 130, 180), Color.White, 150));
            syncTop.Controls.Add(CreateTabButton(_btnLoadDefault, "默认配置路径", Color.FromArgb(70, 130, 180), Color.White, 110));
            syncTop.Controls.Add(CreateTabButton(_btnSelectFolder, "手动选择文件夹", Color.FromArgb(70, 130, 180), Color.White, 120));
            syncTop.Controls.Add(CreateTabButton(_btnSync, "快捷覆盖", Color.FromArgb(50, 205, 50), Color.White, 100));

            // ★★★ Dock 逆序规则：先放 Top 内容，后放 Top 工具条（否则工具条盖住内容）★★★
            // 框体高度减小：同步面板固定高度，不再占满整个标签页
            Panel syncPanel = _rightPanel.BuildSyncPanel();
            syncPanel.Dock = DockStyle.Top;
            syncPanel.Height = 505;
            _panelSync.Controls.Add(syncPanel);
            _panelSync.Controls.Add(syncTop);

            // ===== 备份管理：顶部按钮 + 备份表格 =====
            _panelBackup.Dock = DockStyle.Fill;
            _panelBackup.BackColor = Color.White;

            // 顶部操作条：左右分布（同配置方案页布局/参数，仅颜色保留备份页原色）
            Panel backupTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            // 左侧：备份当前配置（参数对齐配置方案页按钮，颜色保留原绿色，事件直接绑定）
            Button btnBackupNow = new Button
            {
                Text = "💾 备份当前配置",
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 205, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnBackupNow.FlatAppearance.BorderSize = 0;
            btnBackupNow.Click += (s, e) => _backupService.PerformBackup();

            // 右侧：删除所有备份（参数对齐配置方案页按钮，颜色保留原橙色，事件直接绑定）
            Button btnDeleteAll = new Button
            {
                Text = "🗑️ 删除所有备份",
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 69, 0),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnDeleteAll.FlatAppearance.BorderSize = 0;
            btnDeleteAll.Click += (s, e) => _backupService.DeleteAllBackups();

            // ★★★ Dock 逆序：标题 Fill 最先 Add，按钮 Left/Right 后停靠（标题居中填两按钮间）★★★
            backupTop.Controls.Add(_rightPanel.LblBackupTitle);
            backupTop.Controls.Add(btnBackupNow);
            backupTop.Controls.Add(btnDeleteAll);

            // ★★★ Dock 逆序规则：先放 Top 内容，后放 Top 工具条 ★★★
            // 框体高度减小：备份面板固定高度，不再占满整个标签页
            Panel backupPanel = _rightPanel.BuildBackupPanel();
            backupPanel.Dock = DockStyle.Top;
            backupPanel.Height = 495;
            _panelBackup.Controls.Add(backupPanel);
            _panelBackup.Controls.Add(backupTop);

            // ===== 配置方案：UserControl 嵌入 =====
            _panelScheme.Dock = DockStyle.Fill;
            _panelScheme.BackColor = Color.White;
            // ★★★ 外层不再设 Padding：按钮条与备份页一样贴边；列表框体的左右下边距在 PeiZhiFangAnView 内单独处理 ★★★
            _schemeView.Dock = DockStyle.Fill;
            _schemeView.OnSchemesChanged += async () => await RefreshFileListAsync();
            _panelScheme.Controls.Add(_schemeView);

            // ===== 使用说明：RichTextBox 显示说明文本 =====
            _panelHelp.Dock = DockStyle.Fill;
            _panelHelp.BackColor = Color.White;
            _panelHelp.Padding = new Padding(10);

            RichTextBox rtbHelp = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Microsoft YaHei", 9),
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.Fixed3D
            };
            rtbHelp.Text = EVEBox.GongYong.BangZhuWenBen.Content;
            _panelHelp.Controls.Add(rtbHelp);

            // ===== 操作日志：RichTextBox + 定时刷新 =====
            _panelLog.Dock = DockStyle.Fill;
            _panelLog.BackColor = Color.White;
            _panelLog.Padding = new Padding(10);

            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.Fixed3D
            };
            _panelLog.Controls.Add(_rtbLog);

            _logRefreshTimer = new System.Windows.Forms.Timer();
            _logRefreshTimer.Interval = 500;
            _logRefreshTimer.Tick += (s, e) => AppendNewLogs();
            _logRefreshTimer.Start();

            // ===== 更新：检查更新 + GitHub + Gitee =====
            _panelUpdate.Dock = DockStyle.Fill;
            _panelUpdate.BackColor = Color.White;

            FlowLayoutPanel updatePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10, 12, 10, 10),
                BackColor = Color.White
            };
            updatePanel.Controls.Add(CreateTabButton(_btnCheckUpdate, "🔍 检查更新", Color.FromArgb(70, 130, 180), Color.White, 120));
            updatePanel.Controls.Add(CreateTabButton(_btnGithub, "🐙 GitHub", Color.FromArgb(36, 41, 46), Color.White, 110));
            updatePanel.Controls.Add(CreateTabButton(_btnGitee, "🚩 Gitee", Color.FromArgb(199, 29, 35), Color.White, 100));

            // 版本信息（当前版本/更新内容/更新日期）
            RichTextBox rtbVersion = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Microsoft YaHei", 10),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            rtbVersion.Text =
                $"当前版本：{YingYongXinXi.Version}\n\n" +
                "更新内容：\n" +
                $"{YingYongXinXi.ReleaseNotes}\n\n" +
                $"更新日期：{YingYongXinXi.ReleaseDate}";

            // ★★★ Dock 逆序规则：先放 Fill 内容，后放 Top 按钮条 ★★★
            _panelUpdate.Controls.Add(rtbVersion);
            _panelUpdate.Controls.Add(updatePanel);

            // ===== 其他工具：7 个新功能（先立项占位） =====
            _panelTools.Dock = DockStyle.Fill;
            _panelTools.BackColor = Color.FromArgb(245, 245, 250);

            FlowLayoutPanel toolsList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(245, 245, 250)
            };

            string[] toolNames = new[]
            {
                "查看各种日志",
                "配置模版一键导入",
                "种菜模版一键导入",
                "装备方案一键导入",
                "舰跳跃距离查询",
                "各种查询网址汇总",
                "总览导入",
            };

            for (int i = 0; i < toolNames.Length; i++)
            {
                string name = toolNames[i];
                Button btn = new Button
                {
                    Text = $"{i + 1}. {name}",
                    Width = 620,
                    Height = 44,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(70, 130, 180),
                    Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(12, 0, 0, 0),
                    Margin = new Padding(0, 0, 0, 8)
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btn.Click += (s, e) => ZiDingYiMessageBox.Show(
                    $"「{name}」功能开发中，敬请期待！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                toolsList.Controls.Add(btn);
            }

            _panelTools.Controls.Add(toolsList);

            _contentHost.Dock = DockStyle.Fill;
            _contentHost.BackColor = Color.White;
            _contentHost.Controls.AddRange(new Control[]
            {
                _panelSync, _panelBackup, _panelScheme, _panelHelp, _panelLog, _panelUpdate, _panelTools
            });
        }

        /// <summary>
        /// 标签切换：只显示当前标签面板
        /// </summary>
        private void OnTabSelected(int index)
        {
            ShowTab(index);
        }

        private void ShowTab(int index)
        {
            // ★★★ 切换时挂起布局，避免表格/工具条重新排布产生刷新闪烁动画 ★★★
            _contentHost.SuspendLayout();
            _panelSync.Visible = index == ZuoCeMianBanBuilder.TabSync;
            _panelBackup.Visible = index == ZuoCeMianBanBuilder.TabBackup;
            _panelScheme.Visible = index == ZuoCeMianBanBuilder.TabScheme;
            _panelHelp.Visible = index == ZuoCeMianBanBuilder.TabHelp;
            _panelLog.Visible = index == ZuoCeMianBanBuilder.TabLog;
            _panelUpdate.Visible = index == ZuoCeMianBanBuilder.TabUpdate;
            _panelTools.Visible = index == ZuoCeMianBanBuilder.TabTools;
            _contentHost.ResumeLayout();
        }

        private Button CreateTabButton(Button btn, string text, Color backColor, Color foreColor, int width)
        {
            btn.Text = text;
            btn.Size = new Size(width, 36);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.Margin = new Padding(2, 6, 6, 0);
            return btn;
        }

        private Label CreatePlaceholderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 11),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
        }

        private void BindEvents()
        {
            // ===== 配置同步标签 =====
            _cmbServer.SelectedIndexChanged += async (s, e) =>
            {
                string newServer = _cmbServer.SelectedItem?.ToString() ?? "曙光服 (Infinity)";
                await OnServerChanged(newServer);
            };

            _btnOpenFolder.Click += (s, e) => _folderService.OpenCurrentFolder();
            _btnLoadDefault.Click += async (s, e) => await _folderService.LoadDefaultFolderAsync();
            _btnSelectFolder.Click += async (s, e) => await _folderService.ManualSelectFolderAsync(this);

            // 快捷覆盖
            _btnSync.Click += async (s, e) =>
            {
                var result = ZiDingYiMessageBox.Show(
                    "确定要执行快捷覆盖吗？\n\n" +
                    "将用当前最新的配置文件覆盖所有其他配置文件。\n" +
                    "建议先点击「备份当前配置」进行备份。",
                    "确认覆盖",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // ★★★ 检测 EVE 客户端（修改文件操作）；选"否"则取消操作 ★★★
                    if (!EveKeHuDuanGuard.EnsureNoClient()) return;

                    await _syncService.SyncAllFilesAsync(
                        () => _currentFolder,
                        (folder) => { _currentFolder = folder; },
                        async () => await RefreshFileListAsync(),
                        (msg) => _logService.Log(msg)
                    );
                }
            };

            // ===== 备份管理标签（按钮事件已在 BuildTabPanels 内直接绑定）=====

            // ===== 更新标签 =====
            _btnCheckUpdate.Click += BtnCheckUpdate_Click;
            _btnGithub.Click += (s, e) => OpenUrl("https://github.com/johngi666/EVEBox");
            _btnGitee.Click += (s, e) => OpenUrl("https://gitee.com/minisangel/EVEBox");

            // ★★★ 绑定用户备注编辑事件 ★★★
            _rightPanel.UserRemarkEdited += OnUserRemarkEdited;

            _rightPanel.DgvUserFiles.CellClick += (s, e) => _dataGridViewHandler.OnUserFileCellClick(s as DataGridView, e);
            _rightPanel.DgvCharFiles.CellClick += (s, e) => _dataGridViewHandler.OnCharFileCellClick(s as DataGridView, e);
            _rightPanel.DgvBackups.CellClick += (s, e) => _dataGridViewHandler.OnBackupCellClick(s as DataGridView, e);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // 忽略打开失败
            }
        }

        // ===== 用户备注编辑事件处理 =====
        private void OnUserRemarkEdited(object sender, YongHuBeiZhuBianJiCanShu e)
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
            _btnOpenFolder.Text = "正在查找...";
            _btnOpenFolder.Enabled = false;

            await _folderService.AutoFindFolderAsync(() => { });

            UpdateFolderButtonState();
        }

        private async Task LoadConfigFilesAsync(string folder)
        {
            _currentFolder = folder;
            UpdateFolderButtonState();

            await RefreshFileListAsync();
            RefreshBackupList();
            UpdateShipTagButtonState();
            _schemeView?.SetParentFolder(folder);

            _configManager.Save();
            _logService.Log("加载配置文件", "成功", folder);
        }

        private void UpdateFolderButtonState()
        {
            _folderService.UpdateFolderButtonState((text, enabled) =>
            {
                _btnOpenFolder.Text = text;
                _btnOpenFolder.Enabled = enabled;
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
                    // ★★★ 同步更新标题计数（备份/删除后立即反映真实数量）★★★
                    _rightPanel.LblBackupTitle.Text = $"备份管理 ({items.Count}个备份)";
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
                new WenJianTongBuManager()
            );
        }

        private void ShowSearchFailDialog()
        {
            using (var dialog = new SouSuoShiBaiDialog(_currentServer))
            {
                dialog.Owner = this;
                var choice = dialog.ShowDialogAndGetResult();

                switch (choice)
                {
                    case SouSuoShiBaiDialog.YongHuXuanZe.SwitchServer:
                        ZiDingYiMessageBox.Show("请切换到「配置同步」页，在服务器下拉框选择其他服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case SouSuoShiBaiDialog.YongHuXuanZe.DeepSearch:
                        _ = _folderService.DeepSearchAndLoadAsync(this);
                        break;
                    case SouSuoShiBaiDialog.YongHuXuanZe.ManualSelect:
                        _ = _folderService.ManualSelectFolderAsync(this);
                        break;
                    case SouSuoShiBaiDialog.YongHuXuanZe.Cancel:
                        UpdateFolderButtonState();
                        break;
                }
            }
        }

        private void ShowUserSyncDialog(YongHuWenJianXiang sourceItem)
        {
            var otherFiles = _userFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                ZiDingYiMessageBox.Show("没有其他用户文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建目标列表：显示名（备注优先）+ 用户ID（用于匹配备注）
            var targets = otherFiles.Select(item => new WenJianMuBiaoXiang(
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

        private void ShowCharSyncDialog(JiaoSeWenJianXiang sourceItem)
        {
            var otherFiles = _charFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                ZiDingYiMessageBox.Show("没有其他角色文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 角色文件没有备注，DisplayName 为角色名或ID
            var targets = otherFiles.Select(item => new WenJianMuBiaoXiang(
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
            List<WenJianMuBiaoXiang> targets,
            Dictionary<string, string> remarks,
            string operationName)
        {
            using (var fileDialog = new TongBuDialog(
                sourceDisplayName,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                remarks))
            {
                if (fileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                // ★★★ 检测 EVE 客户端（修改文件操作）；选"否"则取消操作 ★★★
                if (!EveKeHuDuanGuard.EnsureNoClient()) return;

                int count = _syncService.CopyFileToTargets(sourceFilePath, fileDialog.SelectedTargets, operationName);
                _ = RefreshFileListAsync();
                ZiDingYiMessageBox.Show($"同步完成，共同步 {count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            ZhuTiManager.Toggle();
            _configManager.Config.UseDarkMode = ZhuTiManager.IsDarkMode;
            _configManager.Save();
            _logService.Log("主题切换", "成功", ZhuTiManager.IsDarkMode ? "夜间模式" : "日间模式");
        }

        // ===== 全局舰船标签显示开关 =====
        private void BtnShipTag_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFolder) || !Directory.Exists(_currentFolder))
            {
                ZiDingYiMessageBox.Show("请先选择有效的EVE配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ★★★ 修改 prefs.ini 也检测客户端；选"否"则取消操作 ★★★
            if (!EveKeHuDuanGuard.EnsureNoClient()) return;

            bool enabled = !JianChuanBiaoQianTool.IsEnabled(_currentFolder);
            JianChuanBiaoQianTool.SetEnabled(_currentFolder, enabled);
            UpdateShipTagButtonState();
            _logService.Log("舰船标签", enabled ? "开启" : "关闭", Path.Combine(_currentFolder, "prefs.ini"));
        }

        private void UpdateShipTagButtonState()
        {
            bool enabled = JianChuanBiaoQianTool.IsEnabled(_currentFolder);
            _leftPanel.BtnShipTag.Text = enabled ? "舰船标签:开启" : "舰船标签:关闭";
            _leftPanel.BtnShipTag.BackColor = enabled
                ? Color.FromArgb(50, 205, 50)
                : Color.FromArgb(70, 130, 180);
        }

        private async void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            _logService.Log("版本检查", "手动触发", "");
            await _updateService.CheckForUpdatesAsync(showResultWhenUpToDate: true);
        }

        private void ApplyTheme(bool isDark)
        {
            // ★★★ 禁用重绘块：标题栏和主体颜色一次性应用、一次性重绘，避免切换闪烁 ★★★
            ZhuTiManager.BeginThemeUpdate(this);
            try
            {
                _titleBarBuilder.ApplyTheme(isDark);
                ZhuTiManager.ApplyCore(this);
                // ★★★ 主题按钮：显示夜间模式→按钮暗黑色；显示日间模式→按钮太阳光色（金黄）；文字白色 ★★★
                // 放在 ApplyCore 之后，避免被蓝色→Accent 逻辑覆盖
                _leftPanel.BtnTheme.Text = isDark ? "☀️日间模式" : "🌙夜间模式";
                _leftPanel.BtnTheme.BackColor = isDark
                    ? Color.FromArgb(255, 170, 0)   // 太阳光色（显示日间模式时）
                    : Color.FromArgb(40, 40, 40);   // 暗黑色（显示夜间模式时）
                _leftPanel.BtnTheme.ForeColor = Color.White;
                UpdateShipTagButtonState();
            }
            finally
            {
                ZhuTiManager.EndThemeUpdate(this);
            }
        }

        /// <summary>
        /// 追加操作日志面板的新日志（定时器驱动）
        /// </summary>
        private void AppendNewLogs()
        {
            if (_rtbLog == null || _rtbLog.IsDisposed) return;

            var logs = _logService.GetLogs();
            int currentCount = logs.Count;
            if (currentCount > _lastLogCount)
            {
                for (int i = _lastLogCount; i < currentCount; i++)
                {
                    if (_rtbLog.Text.Length > 0)
                        _rtbLog.AppendText(Environment.NewLine);
                    _rtbLog.AppendText(logs[i]);
                }
                _rtbLog.SelectionStart = _rtbLog.Text.Length;
                _rtbLog.ScrollToCaret();
                _lastLogCount = currentCount;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    _leftPanel.SelectTab(ZuoCeMianBanBuilder.TabHelp);
                    ShowTab(ZuoCeMianBanBuilder.TabHelp);
                    return true;
                case Keys.F2:
                    _leftPanel.SelectTab(ZuoCeMianBanBuilder.TabLog);
                    ShowTab(ZuoCeMianBanBuilder.TabLog);
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
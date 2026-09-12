using EVEBox.Common.GongYong;
using EVEBox.OtherTools;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace EVEBox.Features.PeiZhiFangAn
{
    /// <summary>
    /// 配置方案管理视图（从 BanBenGuanLiDialog 迁移为可嵌入标签页的 UserControl）
    /// </summary>
    public class PeiZhiFangAnView : UserControl
    {
        private readonly PeiZhiFangAnManager _manager;
        private string _parentFolder;
        private ObservableCollection<FangAn> _schemes;
        private FlowLayoutPanel schemesContainer;
        private Panel _listContainer;
        private readonly Action<string> _addLogCallback;

        /// <summary>
        /// 方案变化事件（切换/更新/增删后触发，供主窗体刷新文件列表）
        /// </summary>
        public event Action OnSchemesChanged;

        public PeiZhiFangAnView(Action<string> addLogCallback)
        {
            _addLogCallback = addLogCallback;
            _manager = new PeiZhiFangAnManager();
            _schemes = new ObservableCollection<FangAn>();
            Initialize();
            LoadSchemes();
        }

        /// <summary>
        /// 设置目标文件夹（父文件夹），切换/更新操作作用于此目录
        /// </summary>
        public void SetParentFolder(string folder)
        {
            _parentFolder = folder;
        }

        private void AddLog(string message) => _addLogCallback?.Invoke(message);

        private void Initialize()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 250);

            // 方案列表（滚动）：不采用边距，改为框体宽高各比可用区域小 10px（四周约 5px 间距）
            Panel listContainer = new Panel
            {
                Dock = DockStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.FromArgb(235, 235, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5)
            };
            _listContainer = listContainer;
            schemesContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            listContainer.Controls.Add(schemesContainer);

            // ★★★ 容器尺寸变化时统一刷新所有方案行的宽度（避免首行正常、后续行变短）★★★
            listContainer.SizeChanged += (s, e) => UpdateSchemeRowWidths();

            // 顶部操作条（添加/创建配置方案）
            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };
            Button btnAdd = new Button
            {
                Text = "+ 添加配置方案",
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 160, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            // 显式清除默认边距，与备份页按钮对齐
            btnAdd.Margin = new Padding(0, 0, 0, 0);
            btnAdd.Click += BtnAdd_Click;

            // 右侧：创建配置方案（在当前服务器根目录新建方案文件夹）
            Button btnCreate = new Button
            {
                Text = "+ 创建配置方案",
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            // 显式清除默认边距，与备份页按钮对齐
            btnCreate.Margin = new Padding(0, 0, 0, 0);
            btnCreate.Click += BtnCreate_Click;

            // ★★★ 与备份页一致：先 Add Left（添加），后 Add Right（创建）★★★
            bottomBar.Controls.Add(btnAdd);
            bottomBar.Controls.Add(btnCreate);

            this.Controls.Add(listContainer);
            this.Controls.Add(bottomBar);

            // ★★★ 框体宽高各比可用区域小 10px（四周约 5px 间距），并随窗口缩放保持 ★★★
            LayoutListContainer();
            this.Resize += (s, e) => LayoutListContainer();
        }

        /// <summary>
        /// 定位方案列表框体：宽高各减小 10px（相对于按钮条下方的可用区域）
        /// </summary>
        private void LayoutListContainer()
        {
            if (_listContainer == null) return;
            int availWidth = ClientSize.Width;
            int availHeight = ClientSize.Height - 50; // 减去顶部按钮条高度
            _listContainer.SetBounds(5, 55, availWidth - 17, availHeight - 17);
        }

        private void LoadSchemes()
        {
            _schemes.Clear();
            foreach (var scheme in _manager.GetAll()) _schemes.Add(scheme);
            RefreshSchemesList();
        }

        private void RefreshSchemesList()
        {
            schemesContainer.Controls.Clear();
            foreach (var scheme in _schemes) schemesContainer.Controls.Add(CreateSchemeRow(scheme));
            if (schemesContainer.Controls.Count == 0)
            {
                schemesContainer.Controls.Add(new Label
                {
                    Text = "暂无配置方案，点击 [+ 添加配置方案] 添加",
                    AutoSize = true,
                    Font = new Font("Microsoft YaHei", 10),
                    ForeColor = Color.Gray,
                    Margin = new Padding(10)
                });
            }
        }

        private Panel CreateSchemeRow(FangAn scheme)
        {
            int width = GetRowWidth();
            Panel row = new Panel
            {
                Width = width,
                Height = 40,
                Margin = new Padding(0, 0, 0, 5),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Label lblName = new Label
            {
                Text = scheme.Name,
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180)
            };
            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                Location = new Point(width - 390, 5),
                Size = new Size(380, 32),
                FlowDirection = FlowDirection.LeftToRight
            };

            bool pathExists = Directory.Exists(scheme.FolderPath);

            Button btnSwitch = CreateRowButton("切换", pathExists ? Color.FromArgb(70, 130, 180) : Color.FromArgb(150, 150, 150));
            btnSwitch.Enabled = pathExists;
            btnSwitch.Click += (s, e) => BtnSwitch_Click(scheme);

            Button btnUpdate = CreateRowButton("更新", pathExists ? Color.FromArgb(50, 205, 50) : Color.FromArgb(150, 150, 150));
            btnUpdate.Enabled = pathExists;
            btnUpdate.Click += (s, e) => BtnUpdate_Click(scheme);

            // 显示：打开方案文件夹
            Button btnShow = CreateRowButton("显示", Color.FromArgb(100, 149, 237));
            btnShow.Click += (s, e) => BtnShow_Click(scheme);

            Button btnBackup = CreateRowButton("备份", Color.FromArgb(255, 165, 0));
            btnBackup.Click += (s, e) => BtnBackup_Click(scheme);

            Button btnRestore = CreateRowButton("还原", Color.FromArgb(70, 130, 180));
            btnRestore.Click += (s, e) => BtnRestore_Click(scheme);

            Button btnRemove = CreateRowButton("移除", Color.FromArgb(255, 69, 0));
            btnRemove.Click += (s, e) => BtnRemove_Click(scheme);

            btnPanel.Controls.AddRange(new Control[] { btnSwitch, btnUpdate, btnShow, btnBackup, btnRestore, btnRemove });
            row.Controls.Add(lblName);
            row.Controls.Add(btnPanel);
            row.Tag = btnPanel; // 记录按钮面板，供统一宽度刷新

            row.Resize += (s, e) =>
            {
                row.Width = GetRowWidth();
                btnPanel.Location = new Point(row.Width - 390, 5);
            };
            return row;
        }

        /// <summary>
        /// 方案行宽度：基于外层列表容器（不依赖 schemesContainer 的 AutoSize 收缩）
        /// </summary>
        private int GetRowWidth()
        {
            int containerWidth = _listContainer?.ClientSize.Width ?? 0;
            return Math.Max(containerWidth - 20, 450);
        }

        /// <summary>
        /// 统一刷新所有方案行的宽度（列表容器尺寸变化时调用）
        /// </summary>
        private void UpdateSchemeRowWidths()
        {
            if (schemesContainer == null) return;
            int width = GetRowWidth();
            foreach (Control ctrl in schemesContainer.Controls)
            {
                if (ctrl is Panel row && row.Tag is FlowLayoutPanel btnPanel)
                {
                    row.Width = width;
                    btnPanel.Location = new Point(width - 390, 5);
                }
            }
        }

        private Button CreateRowButton(string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                Size = new Size(55, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0)
            };
        }

        private void BtnSwitch_Click(FangAn scheme)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                ZiDingYiMessageBox.Show("错误：父文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(scheme.FolderPath))
            {
                ZiDingYiMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = ZiDingYiMessageBox.Show(
                $"确定将 [{scheme.Name}] 切换到父文件夹吗？\n\n此操作将用方案文件覆盖当前配置。",
                "确认切换",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（切换方案会覆盖文件）；选"否"则取消操作 ★★★
            if (!EveKeHuDuanGuard.EnsureNoClient()) return;

            try
            {
                CopyDirectoryContents(scheme.FolderPath, _parentFolder);
                _manager.UpdateLastUsed(scheme.Id);
                OnSchemesChanged?.Invoke();
                AddLog($"切换完成 [{scheme.Name}]");
                ZiDingYiMessageBox.Show($"切换完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ZiDingYiMessageBox.Show($"切换失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(FangAn scheme)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                ZiDingYiMessageBox.Show("错误：父文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(scheme.FolderPath))
            {
                ZiDingYiMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = ZiDingYiMessageBox.Show(
                $"将父文件夹配置更新到 [{scheme.Name}]？",
                "确认更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（更新方案会写入文件）；选"否"则取消操作 ★★★
            if (!EveKeHuDuanGuard.EnsureNoClient()) return;

            Task.Run(() =>
            {
                try
                {
                    CopyDirectoryContents(_parentFolder, scheme.FolderPath);
                    _manager.UpdateLastUsed(scheme.Id);
                    OnSchemesChanged?.Invoke();
                    ZiDingYiMessageBox.Show($"更新完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ZiDingYiMessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private async void BtnBackup_Click(FangAn scheme)
        {
            if (!Directory.Exists(scheme.FolderPath))
            {
                ZiDingYiMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string backupPath = await Task.Run(() =>
            {
                try
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string path = Path.Combine(desktop, $"{scheme.Name}_Backup_{timestamp}");
                    Directory.CreateDirectory(path);
                    CopyDirectoryContents(scheme.FolderPath, path);
                    return path;
                }
                catch (Exception)
                {
                    AddLog("备份失败");
                    return null;
                }
            });

            if (backupPath != null)
            {
                AddLog($"备份完成 [{scheme.Name}] -> {backupPath}");
                ZiDingYiMessageBox.Show($"备份完成\n保存路径: {backupPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                ZiDingYiMessageBox.Show("备份失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(FangAn scheme)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择备份文件夹";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            string backupPath = dialog.SelectedPath;
            var result = ZiDingYiMessageBox.Show(
                $"将备份还原到 [{scheme.Name}]？\n这将覆盖目标文件夹中的所有文件。",
                "确认还原", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（还原会覆盖文件）；选"否"则取消操作 ★★★
            if (!EveKeHuDuanGuard.EnsureNoClient()) return;

            Task.Run(() =>
            {
                try
                {
                    CopyDirectoryContents(backupPath, scheme.FolderPath);
                    OnSchemesChanged?.Invoke();
                    ZiDingYiMessageBox.Show($"还原完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ZiDingYiMessageBox.Show($"还原失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void BtnRemove_Click(FangAn scheme)
        {
            var result = ZiDingYiMessageBox.Show(
                $"移除 [{scheme.Name}]？\n此操作不会删除实际文件夹。",
                "确认移除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _manager.Remove(scheme.Id);
            _schemes.Remove(scheme);
            RefreshSchemesList();
            OnSchemesChanged?.Invoke();
            AddLog($"已移除 [{scheme.Name}]");
            ZiDingYiMessageBox.Show($"已移除 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择配置方案文件夹";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            AddScheme(dialog.SelectedPath);
        }

        /// <summary>
        /// 在资源管理器中打开方案文件夹
        /// </summary>
        private void BtnShow_Click(FangAn scheme)
        {
            try
            {
                if (!Directory.Exists(scheme.FolderPath))
                {
                    ZiDingYiMessageBox.Show($"方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Process.Start(new ProcessStartInfo(scheme.FolderPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ZiDingYiMessageBox.Show($"无法打开文件夹: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 创建配置方案：在当前服务器根目录（与 settings_Default 平级）新建文件夹并加入列表
        /// </summary>
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                ZiDingYiMessageBox.Show("请先选择有效的EVE配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 服务器根目录 = 当前配置文件夹（settings_Default）的父目录
            string serverRoot = Directory.GetParent(_parentFolder)?.FullName;
            if (string.IsNullOrEmpty(serverRoot) || !Directory.Exists(serverRoot))
            {
                ZiDingYiMessageBox.Show("无法定位服务器配置根目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = PromptForSchemeName();
            if (string.IsNullOrEmpty(name)) return;

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ZiDingYiMessageBox.Show("方案名称包含非法字符", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newPath = Path.Combine(serverRoot, name);
            if (Directory.Exists(newPath))
            {
                ZiDingYiMessageBox.Show($"同名文件夹已存在: {name}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(newPath);
                AddLog($"已创建方案文件夹: {newPath}");
                AddScheme(newPath);
            }
            catch (Exception ex)
            {
                ZiDingYiMessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 简易输入框：输入方案名称
        /// </summary>
        private string PromptForSchemeName()
        {
            using var dlg = new Form
            {
                Text = "创建配置方案",
                Size = new Size(360, 165),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lbl = new Label
            {
                Text = "方案名称（将与 settings_Default 平级创建）：",
                Location = new Point(15, 12),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9)
            };
            TextBox txt = new TextBox
            {
                Location = new Point(15, 40),
                Size = new Size(310, 25),
                Font = new Font("Microsoft YaHei", 10)
            };
            Button ok = new Button
            {
                Text = "确定",
                Location = new Point(170, 82),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold)
            };
            ok.FlatAppearance.BorderSize = 0;
            Button cancel = new Button
            {
                Text = "取消",
                Location = new Point(255, 82),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };

            dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;

            return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
        }

        private void AddScheme(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            if (_schemes.Any(s => s.FolderPath == folderPath))
            {
                ZiDingYiMessageBox.Show($"文件夹已存在: {Path.GetFileName(folderPath)}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string schemeName = Path.GetFileName(folderPath);
            int counter = 1;
            while (_schemes.Any(s => s.Name == schemeName))
            {
                schemeName = $"{Path.GetFileName(folderPath)} ({counter++})";
            }

            var scheme = new FangAn
            {
                Name = schemeName,
                FolderPath = folderPath
            };

            if (_manager.Add(scheme))
            {
                _schemes.Add(scheme);
                RefreshSchemesList();
                OnSchemesChanged?.Invoke();
                AddLog($"已添加: {schemeName}");
                ZiDingYiMessageBox.Show($"已添加: {schemeName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AddLog($"添加失败: {schemeName}");
                ZiDingYiMessageBox.Show($"添加失败: {schemeName}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyDirectoryContents(string source, string dest)
        {
            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(dest, fileName);
                File.Copy(file, destFile, true);
            }
        }
    }
}

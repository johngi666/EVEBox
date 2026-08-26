using EVESyncTool.Core.Config;
using EVESyncTool.Core.Services;
using EVESyncTool.Core.UI;
using EVESyncTool.Core.Config;
using EVESyncTool.Dialogs.Common;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Config
{
    public partial class VersionManageDialog : BaseDialog
    {
        // 恢复原风格：标题居中、深蓝标题栏（日间）
        protected override bool CenterTitle => true;

        private readonly ConfigSchemeManager _manager;
        private string _parentFolder;
        private ObservableCollection<ConfigScheme> _schemes;
        private FlowLayoutPanel schemesContainer;
        private Action<string> _addLogCallback;
        private Button btnAdd;
        private Button btnCancel;

        public event Action OnSchemesChanged;

        public VersionManageDialog(string parentFolder, Action<string> addLogCallback)
        {
            _parentFolder = parentFolder;
            _addLogCallback = addLogCallback;
            _manager = new ConfigSchemeManager();
            _schemes = new ObservableCollection<ConfigScheme>();
            InitializeForm();
            LoadSchemes();
        }

        private void AddLog(string message) => _addLogCallback?.Invoke(message);

        protected override void ApplyTitleBarStyle()
        {
            // 日间用深蓝标题栏，夜间用主题色
            TitleBar.BackColor = ThemeManager.IsDarkMode
                ? ThemeManager.TitleBar
                : Color.FromArgb(40, 100, 150);
        }

        private void InitializeForm()
        {
            this.Size = new Size(600, 520);
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.TopMost = false;

            Panel listContainer = new Panel
            {
                Location = new Point(15, 50),
                Size = new Size(this.Width - 30, 380),
                AutoScroll = true,
                BackColor = Color.FromArgb(235, 235, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            schemesContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(5)
            };
            listContainer.Controls.Add(schemesContainer);

            btnAdd = new Button
            {
                Text = "+ 添加配置方案",
                Size = new Size(150, 35),
                Location = new Point(15, 445),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 160, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnCancel = new Button
            {
                Text = "关闭",
                Size = new Size(100, 35),
                Location = new Point(this.Width - 115, 445),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 120, 140),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            Panel contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            contentPanel.Controls.Add(listContainer);
            contentPanel.Controls.Add(btnAdd);
            contentPanel.Controls.Add(btnCancel);
            this.Controls.Add(contentPanel);

            this.Resize += (s, e) =>
            {
                listContainer.Size = new Size(this.Width - 30, this.Height - 135);
                btnAdd.Location = new Point(15, this.Height - 75);
                btnCancel.Location = new Point(this.Width - 115, this.Height - 75);
            };

            btnCancel.Location = new Point(this.Width - 115, 445);
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
                schemesContainer.Controls.Add(new Label { Text = "暂无配置方案，点击 [+ 添加配置方案] 添加", AutoSize = true, Font = new Font("Microsoft YaHei", 10), ForeColor = Color.Gray });
        }

        private Panel CreateSchemeRow(ConfigScheme scheme)
        {
            int width = Math.Max(schemesContainer.Width - 20, 350);
            Panel row = new Panel { Width = width, Height = 40, Margin = new Padding(0, 0, 0, 5), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Label lblName = new Label { Text = scheme.Name, Location = new Point(10, 10), AutoSize = true, Font = new Font("Microsoft YaHei", 10, FontStyle.Bold), ForeColor = Color.FromArgb(70, 130, 180) };
            FlowLayoutPanel btnPanel = new FlowLayoutPanel { Location = new Point(width - 310, 5), Size = new Size(300, 32), FlowDirection = FlowDirection.LeftToRight };

            bool pathExists = Directory.Exists(scheme.FolderPath);

            Button btnSwitch = new Button { Text = "切换", Size = new Size(55, 28), FlatStyle = FlatStyle.Flat, BackColor = pathExists ? Color.FromArgb(70, 130, 180) : Color.FromArgb(150, 150, 150), ForeColor = Color.White, Font = new Font("Microsoft YaHei", 8, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(2, 0, 2, 0) };
            btnSwitch.Click += (s, e) => BtnSwitch_Click(scheme);
            btnSwitch.Enabled = pathExists;

            Button btnUpdate = new Button { Text = "更新", Size = new Size(55, 28), FlatStyle = FlatStyle.Flat, BackColor = pathExists ? Color.FromArgb(50, 205, 50) : Color.FromArgb(150, 150, 150), ForeColor = Color.White, Font = new Font("Microsoft YaHei", 8, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(2, 0, 2, 0) };
            btnUpdate.Click += (s, e) => BtnUpdate_Click(scheme);
            btnUpdate.Enabled = pathExists;

            Button btnBackup = new Button { Text = "备份", Size = new Size(55, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(255, 165, 0), ForeColor = Color.White, Font = new Font("Microsoft YaHei", 8, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(2, 0, 2, 0) };
            btnBackup.Click += (s, e) => BtnBackup_Click(scheme);

            Button btnRestore = new Button { Text = "还原", Size = new Size(55, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 130, 180), ForeColor = Color.White, Font = new Font("Microsoft YaHei", 8, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(2, 0, 2, 0) };
            btnRestore.Click += (s, e) => BtnRestore_Click(scheme);

            Button btnRemove = new Button { Text = "移除", Size = new Size(55, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(255, 69, 0), ForeColor = Color.White, Font = new Font("Microsoft YaHei", 8, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(2, 0, 2, 0) };
            btnRemove.Click += (s, e) => BtnRemove_Click(scheme);

            btnPanel.Controls.AddRange(new Control[] { btnSwitch, btnUpdate, btnBackup, btnRestore, btnRemove });
            row.Controls.Add(lblName);
            row.Controls.Add(btnPanel);

            row.Resize += (s, e) => { row.Width = Math.Max(schemesContainer.Width - 20, 350); btnPanel.Location = new Point(row.Width - 310, 5); };
            return row;
        }

        // ===== 切换（已移除自动备份，添加确认弹窗） =====
        private void BtnSwitch_Click(ConfigScheme scheme)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                CustomMessageBox.Show("错误：父文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(scheme.FolderPath))
            {
                CustomMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ★★★ 确认弹窗 ★★★
            var result = CustomMessageBox.Show(
                $"确定将 [{scheme.Name}] 切换到父文件夹吗？\n\n此操作将用方案文件覆盖当前配置。",
                "确认切换",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（切换方案会覆盖文件）；选"否"则取消操作 ★★★
            if (!EveClientGuard.EnsureNoClient()) return;

            try
            {
                CopyDirectoryContents(scheme.FolderPath, _parentFolder);
                _manager.UpdateLastUsed(scheme.Id);
                OnSchemesChanged?.Invoke();
                AddLog($"切换完成 [{scheme.Name}]");
                CustomMessageBox.Show($"切换完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"切换失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(ConfigScheme scheme)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                CustomMessageBox.Show("错误：父文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(scheme.FolderPath))
            {
                CustomMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = CustomMessageBox.Show(
                $"将父文件夹配置更新到 [{scheme.Name}]？",
                "确认更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（更新方案会写入文件）；选"否"则取消操作 ★★★
            if (!EveClientGuard.EnsureNoClient()) return;

            Task.Run(() =>
            {
                try
                {
                    CopyDirectoryContents(_parentFolder, scheme.FolderPath);
                    _manager.UpdateLastUsed(scheme.Id);
                    OnSchemesChanged?.Invoke();
                    CustomMessageBox.Show($"更新完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private async void BtnBackup_Click(ConfigScheme scheme)
        {
            if (!Directory.Exists(scheme.FolderPath))
            {
                CustomMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                CustomMessageBox.Show($"备份完成\n保存路径: {backupPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                CustomMessageBox.Show("备份失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(ConfigScheme scheme)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择备份文件夹";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            string backupPath = dialog.SelectedPath;
            var result = CustomMessageBox.Show(
                $"将备份还原到 [{scheme.Name}]？\n这将覆盖目标文件夹中的所有文件。",
                "确认还原", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（还原会覆盖文件）；选"否"则取消操作 ★★★
            if (!EveClientGuard.EnsureNoClient()) return;

            Task.Run(() =>
            {
                try
                {
                    CopyDirectoryContents(backupPath, scheme.FolderPath);
                    OnSchemesChanged?.Invoke();
                    CustomMessageBox.Show($"还原完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"还原失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void BtnRemove_Click(ConfigScheme scheme)
        {
            var result = CustomMessageBox.Show(
                $"移除 [{scheme.Name}]？\n此操作不会删除实际文件夹。",
                "确认移除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _manager.Remove(scheme.Id);
            _schemes.Remove(scheme);
            RefreshSchemesList();
            OnSchemesChanged?.Invoke();
            AddLog($"已移除 [{scheme.Name}]");
            CustomMessageBox.Show($"已移除 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择配置方案文件夹";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            AddScheme(dialog.SelectedPath);
        }

        private void AddScheme(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            if (_schemes.Any(s => s.FolderPath == folderPath))
            {
                CustomMessageBox.Show($"文件夹已存在: {Path.GetFileName(folderPath)}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string schemeName = Path.GetFileName(folderPath);
            int counter = 1;
            while (_schemes.Any(s => s.Name == schemeName))
            {
                schemeName = $"{Path.GetFileName(folderPath)} ({counter++})";
            }

            var scheme = new ConfigScheme
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
                CustomMessageBox.Show($"已添加: {schemeName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AddLog($"添加失败: {schemeName}");
                CustomMessageBox.Show($"添加失败: {schemeName}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
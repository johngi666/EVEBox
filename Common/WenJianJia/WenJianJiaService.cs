using EVEBox.App;
using EVEBox.Common.GongYong;
using EVEBox.Common.PeiZhi;
using EVEBox.Features.RiZhi;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace EVEBox.Common.WenJianJia
{
    public class WenJianJiaService
    {
        private readonly PeiZhiManager _configManager;
        private readonly WenJianJiaFinder _folderFinder;
        private readonly RiZhiService _logService;
        private readonly Func<string, Task> _onFolderLoaded;

        private string _currentServer;
        private string _currentFolder;
        private bool _isInitialized = false;

        public string CurrentFolder => _currentFolder;
        public bool IsInitialized => _isInitialized;
        public string CurrentServer => _currentServer;

        public WenJianJiaService(
            PeiZhiManager configManager,
            WenJianJiaFinder folderFinder,
            RiZhiService logService,
            Func<string, Task> onFolderLoaded)
        {
            _configManager = configManager;
            _folderFinder = folderFinder;
            _logService = logService;
            _onFolderLoaded = onFolderLoaded;
        }

        public void SetCurrentServer(string server)
        {
            _currentServer = server;
        }

        public async Task AutoFindFolderAsync(Action updateUi = null)
        {
            if (_isInitialized)
            {
                _logService.Log("自动查找文件夹", "跳过", "已初始化");
                return;
            }

            updateUi?.Invoke();

            _logService.Log("自动查找文件夹", _currentServer, "");

            string cachedPath = _configManager.GetCachedPath();
            string found = _folderFinder.AutoFind(_currentServer, cachedPath, null);

            if (found != null)
            {
                _currentFolder = found;
                _configManager.SaveCachedPath(found);
                await LoadConfigFilesAsync(found);
            }
            else
            {
                _currentFolder = null;
                _logService.Log("自动查找文件夹", "失败", "未找到设置文件夹");
            }

            _isInitialized = true;
        }

        public async Task DeepSearchAndLoadAsync(IWin32Window owner)
        {
            using (var progressForm = new SouSuoJinDuForm())
            {
                if (owner is Form form)
                {
                    progressForm.Owner = form;
                }
                progressForm.Show();
                Application.DoEvents();

                string found = null;

                try
                {
                    var task = Task.Run(() =>
                    {
                        var progress = new Progress<string>(s => progressForm.UpdateStatus(s));
                        return _folderFinder.DeepSearch(_currentServer, progress, () => progressForm.IsCancelled);
                    });

                    while (!task.IsCompleted && !progressForm.IsCancelled)
                    {
                        Application.DoEvents();
                        await Task.Delay(100);
                    }

                    if (progressForm.IsCancelled)
                    {
                        progressForm.CloseForm();
                        ZiDingYiMessageBox.Show("深度搜索已取消", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    found = task.Result;
                }
                catch (Exception ex)
                {
                    _logService.Log("深度搜索", "异常", ex.Message);
                }

                progressForm.CloseForm();

                if (found != null)
                {
                    _currentFolder = found;
                    _configManager.SaveCachedPath(found);
                    await LoadConfigFilesAsync(found);
                    ZiDingYiMessageBox.Show($"已找到配置文件夹", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logService.Log("深度搜索", "成功", found);
                }
                else
                {
                    ZiDingYiMessageBox.Show($"未找到{_currentServer}配置文件夹\n请手动选择", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public async Task LoadConfigFilesAsync(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                _logService.Log("加载配置文件", "失败", "文件夹不存在");
                return;
            }

            _currentFolder = folder;
            _configManager.Save();

            if (_onFolderLoaded != null)
            {
                await _onFolderLoaded(folder);
            }
            // 加载成功日志由 _onFolderLoaded（ZhuChuangTi）记录，避免重复
        }

        public string GetDefaultPath()
        {
            if (string.IsNullOrEmpty(_currentFolder))
                return null;

            return _folderFinder.GetDefaultPath(_currentFolder);
        }

        public async Task SwitchServerAsync(string newServer, Action<string> onShowSearchFail)
        {
            _currentServer = newServer;
            _isInitialized = false;

            string found = _folderFinder.QuickFind(_currentServer);

            if (found != null)
            {
                _currentFolder = found;
                _configManager.SaveCachedPath(found);
                await LoadConfigFilesAsync(found);
                _logService.Log("切换服务器", "成功找到配置", found);
            }
            else
            {
                _currentFolder = null;
                _logService.Log("切换服务器", "未找到配置", "");
                onShowSearchFail?.Invoke(_currentServer);
            }

            _isInitialized = true;
        }

        public async Task ManualSelectFolderAsync(IWin32Window owner)
        {
            string folder = _folderFinder.ManualSelect(owner);
            if (folder != null && _folderFinder.IsValidFolder(folder))
            {
                _currentFolder = folder;
                _configManager.SaveCachedPath(folder);
                await LoadConfigFilesAsync(folder);
                ZiDingYiMessageBox.Show("已加载配置文件夹", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logService.Log("手动选择文件夹", "成功", folder);
            }
            else if (folder != null)
            {
                ZiDingYiMessageBox.Show("选择的文件夹不是有效的EVE配置文件夹", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logService.Log("手动选择文件夹", "失败", "无效的EVE配置文件夹");
            }
        }

        public async Task LoadDefaultFolderAsync()
        {
            if (string.IsNullOrEmpty(_currentFolder))
            {
                await AutoFindFolderAsync();
                return;
            }

            string defaultPath = _folderFinder.GetDefaultPath(_currentFolder);
            if (defaultPath != null)
            {
                _currentFolder = defaultPath;
                _configManager.SaveCachedPath(defaultPath);
                await LoadConfigFilesAsync(defaultPath);
                ZiDingYiMessageBox.Show($"已切换到默认配置文件夹", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logService.Log("加载默认配置文件夹", "成功", defaultPath);
            }
            else
            {
                ZiDingYiMessageBox.Show($"未找到默认配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logService.Log("加载默认配置文件夹", "失败", "settings_Default 不存在");
            }
        }

        public void OpenCurrentFolder()
        {
            if (!string.IsNullOrEmpty(_currentFolder) && Directory.Exists(_currentFolder))
            {
                try
                {
                    Process.Start("explorer.exe", _currentFolder);
                    _logService.Log("打开配置文件夹", "成功", _currentFolder);
                }
                catch (Exception ex)
                {
                    ZiDingYiMessageBox.Show($"无法打开文件夹:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _logService.Log("打开配置文件夹", "失败", ex.Message);
                }
            }
        }

        public void UpdateFolderButtonState(Action<string, bool> setButtonState)
        {
            if (!string.IsNullOrEmpty(_currentFolder) && Directory.Exists(_currentFolder))
            {
                setButtonState?.Invoke("📂 打开当前配置文件夹", true);
            }
            else
            {
                setButtonState?.Invoke("❌ 未识别到可用配置", false);
            }
        }

        public string GetCurrentFolder()
        {
            return _currentFolder;
        }

        public bool IsValidFolder(string folder)
        {
            return _folderFinder.IsValidFolder(folder);
        }
    }
}
using EVEBox.App;
using EVEBox.Common.Shared;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace EVEBox.Features.Update
{
    /// <summary>
    /// 自动更新服务：多源检测新版本 + 程序内下载安装（带进度、自动替换重启）
    /// </summary>
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly UpdateDownloader _downloader;
        private readonly Action<string, string, string> _logAction;
        private readonly Form _owner;

        // 本次运行已提醒过的版本（点"稍后提醒"后不再重复弹）
        private string _lastNotifiedVersion;

        public UpdateService(
            HttpClient httpClient,
            UpdateDownloader downloader,
            Action<string, string, string> logAction,
            Form owner)
        {
            _httpClient = httpClient;
            _downloader = downloader;
            _logAction = logAction;
            _owner = owner;
        }

        /// <summary>
        /// 检查更新（多地址轮询，任一成功即结束）
        /// </summary>
        public async Task CheckForUpdatesAsync(bool showResultWhenUpToDate = false)
        {
            string lastError = null;

            foreach (string url in AppInfo.UpdateCheckUrls)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    string json = await _httpClient.GetStringAsync(url, cts.Token);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string remoteVersion = root.GetProperty("version").GetString();
                    string downloadUrl = root.TryGetProperty("downloadUrl", out var d) ? d.GetString()
                        : root.TryGetProperty("url", out var u) ? u.GetString() : AppInfo.ReleasesUrl;
                    string notes = root.TryGetProperty("notes", out var n) ? n.GetString() : "";

                    if (VersionNumber.GengXin(remoteVersion, AppInfo.Version))
                    {
                        // 同一版本本次运行只提醒一次（点"稍后提醒"后不再重复弹）
                        if (remoteVersion == _lastNotifiedVersion)
                            return;
                        _lastNotifiedVersion = remoteVersion;

                        _owner.Invoke(new Action(async () =>
                        {
                            using UpdateDialog dialog = new UpdateDialog(remoteVersion, notes, downloadUrl);
                            dialog.Owner = _owner;
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                await DownloadAndInstallUpdateAsync(remoteVersion, downloadUrl);
                            }
                        }));
                    }
                    else
                    {
                        if (showResultWhenUpToDate)
                        {
                            _owner.Invoke(new Action(() =>
                                CustomMessageBox.Show($"当前已是最新版本 {AppInfo.Version}", "版本检查",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information)));
                        }
                    }
                    return; // 任一地址成功即结束
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    // 尝试下一个地址
                }
            }

            // 所有地址都失败
            if (showResultWhenUpToDate)
            {
                _owner.Invoke(new Action(() =>
                    CustomMessageBox.Show($"检查更新失败，请检查网络连接。\n\n{lastError}", "版本检查",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
        }

        /// <summary>
        /// 下载新版本并自动替换重启
        /// </summary>
        private async Task DownloadAndInstallUpdateAsync(string version, string downloadUrl)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string dir = Path.GetDirectoryName(exePath) ?? string.Empty;

                using DownloadProgressDialog dialog = new DownloadProgressDialog(version);
                dialog.Owner = _owner;
                dialog.Show();

                var progress = new Progress<int>(p => dialog.UpdateProgress(p, $"已下载 {p}%"));
                string zanCunExePath = await Task.Run(() =>
                    _downloader.DownloadAndPrepareAsync(downloadUrl, dir, progress, CancellationToken.None));

                if (dialog.IsCancelled)
                {
                    dialog.Close();
                    _logAction?.Invoke("自动更新", "已取消", version);
                    return;
                }

                if (string.IsNullOrEmpty(zanCunExePath))
                {
                    dialog.Close();
                    CustomMessageBox.Show("下载失败，请稍后重试，或点击标题栏 GitHub/Gitee 按钮手动下载。",
                        "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _logAction?.Invoke("自动更新", "下载失败", downloadUrl);
                    return;
                }

                dialog.UpdateProgress(100, "下载完成，即将重启安装...");
                await Task.Delay(600);

                // 启动替换脚本：杀进程 → 删除旧 exe → 新 exe 落成包内文件名（EVE BOX.exe）→ 重启 → 自删
                _downloader.ApplyUpdateAndRestart(exePath, zanCunExePath);
                dialog.Close();
                _logAction?.Invoke("自动更新", "成功", $"已下载 {version}，程序即将重启");
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logAction?.Invoke("自动更新", "异常", ex.Message);
            }
        }
    }
}

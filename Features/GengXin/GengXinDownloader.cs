using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;



namespace EVEBox.Features.GengXin
{
    /// <summary>
    /// 更新下载与安装服务
    /// 1. 下载新 exe（带进度）
    /// 2. 生成替换脚本（cmd），由脚本完成：杀进程 → 替换 → 重启 → 自删
    /// </summary>
    public class GengXinDownloader
    {
        private readonly HttpClient _httpClient;

        public GengXinDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 下载文件到指定路径，返回是否成功
        /// </summary>
        public async Task<bool> DownloadAsync(
            string url,
            string savePath,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "EVEConfigManager/1.0");
                request.Headers.Add("Accept", "application/octet-stream");

                using var response = await _httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return false;

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[81920];
                long readBytes = 0;
                int count;

                while ((count = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    readBytes += count;
                    if (totalBytes > 0)
                    {
                        int percent = (int)(readBytes * 100 / totalBytes);
                        progress?.Report(percent);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // 网络中断、取消等由调用方处理
                return false;
            }
        }

        /// <summary>
        /// 下载并准备新 exe（支持直接下载 .exe 或 .zip 压缩包自动解压）
        /// </summary>
        public async Task<bool> DownloadAndPrepareAsync(
            string url,
            string finalExePath,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            bool isZip = url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            string tempPath = Path.Combine(Path.GetTempPath(),
                "eve_update_" + Guid.NewGuid().ToString("N") + (isZip ? ".zip" : ".tmp"));

            try
            {
                if (!await DownloadAsync(url, tempPath, progress, cancellationToken))
                    return false;

                if (isZip)
                {
                    string extractDir = Path.Combine(Path.GetTempPath(),
                        "eve_extract_" + Guid.NewGuid().ToString("N"));
                    System.IO.Directory.CreateDirectory(extractDir);
                    ZipFile.ExtractToDirectory(tempPath, extractDir);

                    string zipExe = System.IO.Directory
                        .GetFiles(extractDir, "*.exe", SearchOption.AllDirectories)
                        .FirstOrDefault();
                    if (zipExe == null)
                        return false;

                    System.IO.File.Move(zipExe, finalExePath, true);
                }
                else
                {
                    System.IO.File.Move(tempPath, finalExePath, true);
                }

                return true;
            }
            finally
            {
                try { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); } catch { }
            }
        }

        /// <summary>
        /// 生成替换脚本并启动，随后主程序应自行退出
        /// 脚本逻辑：等待主程序退出 → 替换 exe → 重启 → 删除自身
        /// </summary>
        public void ApplyUpdateAndRestart(string currentExePath, string newExePath)
        {
            string exeName = Path.GetFileName(currentExePath);
            string scriptPath = Path.Combine(Path.GetDirectoryName(currentExePath), "update_install.cmd");

            // 注意：路径中的中文/空格已用引号包裹，%~f0 为脚本自身路径
            string content =
                "@echo off\r\n" +
                "chcp 65001 >nul\r\n" +
                ":wait\r\n" +
                $"taskkill /f /im \"{exeName}\" >nul 2>&1\r\n" +
                "timeout /t 2 /nobreak >nul\r\n" +
                $"del /f \"{currentExePath}\" >nul 2>&1\r\n" +
                $"if exist \"{currentExePath}\" goto wait\r\n" +
                $"move /y \"{newExePath}\" \"{currentExePath}\" >nul\r\n" +
                $"start \"\" \"{currentExePath}\"\r\n" +
                "del /f \"%~f0\"\r\n";

            System.IO.File.WriteAllText(scriptPath, content);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }
}

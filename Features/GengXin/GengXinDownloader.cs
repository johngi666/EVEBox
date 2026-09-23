using EVEBox.App;
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
    /// 1. 下载更新包（带进度），解压取包内主程序
    /// 2. 生成替换脚本（cmd），由脚本完成：杀进程 → 删旧 exe → 替换 → 重启 → 自删
    /// 安装后的文件名取包内主程序名（EVE BOX.exe），不再沿用被替换的旧文件名，
    /// 这样从旧版（EVE配置管理工具.exe）更新过来也能得到正确的程序名。
    /// </summary>
    public class GengXinDownloader
    {
        /// <summary>
        /// 暂存文件名后缀：先落成 "EVE BOX.new.exe"，避免与正在运行的旧 exe 同名冲突
        /// </summary>
        public const string ZanCunHouZhui = ".new.exe";

        /// <summary>等待主程序退出的最大次数（每次 1 秒），超时则走兜底分支</summary>
        private const int ZuiDaDengDaiCiShu = 60;

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
        /// 成功返回暂存的新 exe 完整路径（形如 "EVE BOX.new.exe"），失败返回 null。
        /// </summary>
        public async Task<string> DownloadAndPrepareAsync(
            string url,
            string targetDir,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            bool isZip = url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            string tempPath = Path.Combine(Path.GetTempPath(),
                "eve_update_" + Guid.NewGuid().ToString("N") + (isZip ? ".zip" : ".tmp"));
            string extractDir = null;

            try
            {
                if (!await DownloadAsync(url, tempPath, progress, cancellationToken))
                    return null;

                string sourceExe;
                string sourceName;

                if (isZip)
                {
                    extractDir = Path.Combine(Path.GetTempPath(),
                        "eve_extract_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(extractDir);
                    ZipFile.ExtractToDirectory(tempPath, extractDir);

                    // 包内若有多个 exe（比如附带的小工具），取体积最大的那个当主程序
                    sourceExe = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories)
                        .OrderByDescending(f => new FileInfo(f).Length)
                        .FirstOrDefault();
                    if (sourceExe == null)
                        return null;
                    sourceName = Path.GetFileName(sourceExe);
                }
                else
                {
                    sourceExe = tempPath;
                    string urlName = Path.GetFileName(new Uri(url).AbsolutePath);
                    sourceName = urlName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? urlName
                        : YingYongXinXi.ExeFileName;
                }

                string zanCunLuJing = Path.Combine(targetDir,
                    Path.GetFileNameWithoutExtension(sourceName) + ZanCunHouZhui);
                File.Move(sourceExe, zanCunLuJing, true);
                return zanCunLuJing;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                try { if (extractDir != null && Directory.Exists(extractDir)) Directory.Delete(extractDir, true); } catch { }
            }
        }

        /// <summary>
        /// 暂存路径 → 安装后的文件名：去掉 ".new" 后缀，即包内主程序名
        /// </summary>
        public static string JieXiAnZhuangMing(string zanCunExeLuJing)
        {
            string ming = Path.GetFileName(zanCunExeLuJing);
            if (ming.EndsWith(ZanCunHouZhui, StringComparison.OrdinalIgnoreCase))
                return ming.Substring(0, ming.Length - ZanCunHouZhui.Length) + ".exe";
            return ming;
        }

        /// <summary>
        /// 生成替换脚本内容
        /// 脚本逻辑：杀进程 → 删除旧 exe → 把暂存的新 exe 换成正式名字 → 重启 → 删除自身
        /// </summary>
        public static string ShengChengAnZhuangJiaoBen(string dangQianExeLuJing, string zanCunExeLuJing)
        {
            string muLu = Path.GetDirectoryName(dangQianExeLuJing) ?? string.Empty;
            string anZhuangLuJing = Path.Combine(muLu, JieXiAnZhuangMing(zanCunExeLuJing));
            string dangQianMing = Path.GetFileName(dangQianExeLuJing);
            string anZhuangMing = Path.GetFileName(anZhuangLuJing);

            // 注意：路径中的空格/中文已用引号包裹，%~f0 为脚本自身路径
            return
                "@echo off\r\n" +
                "chcp 65001 >nul\r\n" +
                "set /a n=0\r\n" +
                ":wait\r\n" +
                $"taskkill /f /im \"{dangQianMing}\" >nul 2>&1\r\n" +
                $"taskkill /f /im \"{anZhuangMing}\" >nul 2>&1\r\n" +
                "timeout /t 1 /nobreak >nul\r\n" +
                "set /a n+=1\r\n" +
                $"if %n% gtr {ZuiDaDengDaiCiShu} goto giveup\r\n" +
                $"del /f /q \"{dangQianExeLuJing}\" >nul 2>&1\r\n" +
                $"if exist \"{dangQianExeLuJing}\" goto wait\r\n" +
                $"move /y \"{zanCunExeLuJing}\" \"{anZhuangLuJing}\" >nul\r\n" +
                $"if exist \"{zanCunExeLuJing}\" goto giveup\r\n" +
                $"start \"\" \"{anZhuangLuJing}\"\r\n" +
                "del /f \"%~f0\"\r\n" +
                "exit /b\r\n" +
                ":giveup\r\n" +
                // 兜底：删不掉或换不动时，至少把还能启动的那一份拉起来，别把用户卡在黑屏
                $"if exist \"{zanCunExeLuJing}\" (start \"\" \"{zanCunExeLuJing}\") else (start \"\" \"{dangQianExeLuJing}\")\r\n" +
                "del /f \"%~f0\"\r\n";
        }

        /// <summary>
        /// 生成替换脚本并启动，随后主程序应自行退出
        /// </summary>
        public void ApplyUpdateAndRestart(string dangQianExeLuJing, string zanCunExeLuJing)
        {
            string muLu = Path.GetDirectoryName(dangQianExeLuJing) ?? string.Empty;
            string scriptPath = Path.Combine(muLu, "update_install.cmd");

            File.WriteAllText(scriptPath, ShengChengAnZhuangJiaoBen(dangQianExeLuJing, zanCunExeLuJing));

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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// c3q.cc 星系数据源：三个服务器接口路径一致，只是根地址不同，故共用本实现。
    /// 数据文件是 "变量名={...}" 格式（非纯 JSON），需先剥离变量名前缀。
    /// 每个服务器一个实例，数据互不混用。
    /// 星系数据不常变动，已随程序内置（OtherTools\XingXiJuLi\ShuJu\，见 EVEBox.csproj）：
    /// 优先读内置资源，运行时无需联网、不生成文件；仅内置资源缺失时才回退网络下载。
    /// </summary>
    public class C3qXingXiShuJuYuan : IXingXiShuJuYuan
    {
        /// <summary>内置数据资源名前缀，与 EVEBox.csproj 里 EmbeddedResource 的 LogicalName 对应</summary>
        private const string ResourcePrefix = "EVEBoxXingXiJuLi/";

        /// <summary>网络下载最多尝试次数（首次 + 4 次重试）</summary>
        private const int MaxAttempts = 5;

        /// <summary>相邻两次尝试的等待时间（毫秒）：1s / 2s / 3s / 4s</summary>
        private static readonly int[] RetryDelaysMs = { 1000, 2000, 3000, 4000 };

        private readonly XingXiFuWuQi _fwq;
        private readonly HttpClient _http;
        private readonly List<XingXi> _systems = new List<XingXi>();

        public C3qXingXiShuJuYuan(XingXiFuWuQi fwq, HttpClient http)
        {
            _fwq = fwq;
            _http = http;
        }

        public string ServerName => _fwq.Name;

        public IReadOnlyList<XingXi> Systems => _systems;

        public async Task LoadAsync(Action<string> log)
        {
            _systems.Clear();

            string systemsJson = await LoadTextAsync("systems/zh/", "星系数据", log);
            string positionJson = await LoadTextAsync("systemposition/zh/", "星系坐标", log);

            var nameMap = ParseDict(systemsJson);
            var posMap = ParseDict(positionJson);

            // 以坐标表为准遍历（每个星系都有坐标，名称表用于补名字与安全等级）
            foreach (var kv in posMap)
            {
                if (!int.TryParse(kv.Key, out int id)) continue;
                var pos = kv.Value;
                if (pos.ValueKind != JsonValueKind.Array || pos.GetArrayLength() < 3) continue;

                double x = pos[0].GetDouble();
                double y = pos[1].GetDouble();
                double z = pos[2].GetDouble();

                string name = kv.Key;
                double security = 0.0;
                if (nameMap.TryGetValue(kv.Key, out var nameArr) &&
                    nameArr.ValueKind == JsonValueKind.Array && nameArr.GetArrayLength() >= 2)
                {
                    if (double.TryParse(nameArr[0].GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double sec))
                        security = sec;
                    name = nameArr[1].GetString() ?? name;
                }

                _systems.Add(new XingXi
                {
                    Id = id,
                    Name = name,
                    X = x,
                    Y = y,
                    Z = z,
                    Security = security,
                    PinyinInitial = PinYinGongJu.QuShouZiMu(name)
                });
            }

            log?.Invoke($"{_fwq.Name} 已加载 {_systems.Count} 个星系");
        }

        /// <summary>
        /// 取数据文本：优先读内置资源（编译进 exe，运行时不需要联网、不生成文件）；
        /// 内置资源缺失时回退到网络下载。
        /// </summary>
        private async Task<string> LoadTextAsync(string path, string dataName, Action<string> log)
        {
            string embedded = ReadEmbedded(path);
            if (embedded != null)
            {
                log?.Invoke($"{_fwq.Name} 正在读取内置{dataName}…");
                return embedded;
            }

            log?.Invoke($"{_fwq.Name} 正在下载{dataName}…");
            return await GetAsync(path, log);
        }

        /// <summary>
        /// 读内置资源：LogicalName 形如 EVEBoxXingXiJuLi/{代号}/systems_zh.txt。
        /// 资源名里的目录分隔符可能是 \ 或 /，与内置模板（NeiZhiMoBanZiYuan）同一处理方式；找不到返回 null。
        /// </summary>
        private string ReadEmbedded(string path)
        {
            string fileName = path.TrimEnd('/').Replace('/', '_') + ".txt";
            string targetName = ResourcePrefix + _fwq.Key + "/" + fileName;

            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (!string.Equals(resourceName.Replace('\\', '/'), targetName, StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            return null;
        }

        /// <summary>
        /// 网络下载（内置资源缺失时的兜底）：拿到响应头即返回、响应体流式读取，最多尝试 5 次（间隔 1s/2s/3s/4s）。
        /// c3q 的服务器常在数据发完后异常断开连接（missing close_notify），
        /// 一次性 ReadAsStringAsync 会因此抛 "Error while copying content to a stream"；
        /// 流式读取 + 重试可把这种断连的影响降到最低。失败时日志带已读字节数，便于判断数据是否完整。
        /// </summary>
        private async Task<string> GetAsync(string path, Action<string> log)
        {
            string url = _fwq.BaseUrl + path;
            Exception lastError = null;
            long lastReadBytes = 0;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                long readBytes = 0;
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.TryAddWithoutValidation("Referer", _fwq.Referer);
                    req.Headers.TryAddWithoutValidation("Accept", "*/*");
                    req.Headers.TryAddWithoutValidation("User-Agent", "EVEBox/1.0");

                    // ResponseHeadersRead：拿到响应头就返回，响应体用 ReadAsync 循环流式读取
                    using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();

                    using var stream = await resp.Content.ReadAsStreamAsync();
                    using var buffer = new MemoryStream();
                    byte[] chunk = new byte[81920];
                    int n;
                    while ((n = await stream.ReadAsync(chunk, 0, chunk.Length)) > 0)
                    {
                        buffer.Write(chunk, 0, n); // 内存缓冲，不涉及 I/O 阻塞，无需 WriteAsync
                        readBytes += n;
                    }

                    return Encoding.UTF8.GetString(buffer.ToArray());
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    lastReadBytes = readBytes;

                    if (attempt < MaxAttempts)
                    {
                        int delayMs = RetryDelaysMs[attempt - 1];
                        log?.Invoke($"{_fwq.Name} 下载失败（第 {attempt} 次，已读取 {readBytes} 字节）：{ex.Message}；{delayMs / 1000} 秒后重试…");
                        await Task.Delay(delayMs);
                    }
                    else
                    {
                        log?.Invoke($"{_fwq.Name} 下载失败：重试 {MaxAttempts - 1} 次后仍失败，最后一次已读取 {readBytes} 字节（数据可能不完整）：{ex.Message}");
                    }
                }
            }

            throw new Exception(
                $"{_fwq.Name} 数据下载失败：重试 {MaxAttempts - 1} 次后仍失败，最后一次已读取 {lastReadBytes} 字节（数据可能不完整）：{lastError?.Message}",
                lastError);
        }

        /// <summary>
        /// 把 "变量名={json}" 文本剥掉前缀后解析成字典。
        /// 注意：JsonElement 只是 JsonDocument 的视图，doc 释放后会失效，
        /// 因此这里用 Clone() 让每个元素独立持有数据。
        /// </summary>
        private static Dictionary<string, JsonElement> ParseDict(string text)
        {
            int idx = text.IndexOf('=');
            if (idx >= 0)
                text = text.Substring(idx + 1);

            using var doc = JsonDocument.Parse(text);
            var dict = new Dictionary<string, JsonElement>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();
            return dict;
        }
    }
}

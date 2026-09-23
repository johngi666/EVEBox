using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// c3q.cc 星系数据源：三个服务器接口路径一致，只是根地址不同，故共用本实现。
    /// 数据文件是 "变量名={...}" 格式（非纯 JSON），需先剥离变量名前缀。
    /// 每个服务器一个实例，数据互不混用。
    /// </summary>
    public class C3qXingXiShuJuYuan : IXingXiShuJuYuan
    {
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

            log?.Invoke($"{_fwq.Name} 正在下载星系数据…");
            string systemsJson = await GetAsync("systems/zh/");
            log?.Invoke($"{_fwq.Name} 正在下载星系坐标…");
            string positionJson = await GetAsync("systemposition/zh/");

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

        private async Task<string> GetAsync(string path)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, _fwq.BaseUrl + path);
            req.Headers.TryAddWithoutValidation("Referer", _fwq.Referer);
            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
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

using EVEBox.App;
using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 仓库根的 version.json 是客户端的更新检查源，写坏一个转义就会让所有人的自动更新失效，
/// 这里把「能不能解析」「版本号是否和程序一致」固定成测试。
/// version.json 里的 Windows 路径反斜杠必须写成 \\，否则不是合法 JSON。
/// </summary>
public class VersionJsonTests
{
    [Fact]
    public void 版本文件是合法Json且版本号与程序一致()
    {
        string? luJing = ZhaoBanBenWenJian();
        if (luJing == null)
            return;   // 不在仓库目录里跑（例如只拷了测试程序集），跳过

        string wenBen = File.ReadAllText(luJing);

        // 解析不了就是发布事故：客户端 UpdateCheckUrls 全靠它
        using var doc = JsonDocument.Parse(wenBen);
        var gen = doc.RootElement;

        string banBen = gen.GetProperty("version").GetString()!;
        Assert.Equal(YingYongXinXi.Version, banBen);

        string xiaZai = gen.GetProperty("downloadUrl").GetString()!;
        Assert.Contains(banBen, xiaZai);
        Assert.EndsWith(".zip", xiaZai);
        Assert.StartsWith("https://", xiaZai);

        Assert.False(string.IsNullOrWhiteSpace(gen.GetProperty("notes").GetString()));
    }

    /// <summary>从测试程序集往上找带 version.json 的仓库根目录</summary>
    private static string? ZhaoBanBenWenJian()
    {
        DirectoryInfo? muLu = new DirectoryInfo(AppContext.BaseDirectory);
        while (muLu != null)
        {
            string wenJian = Path.Combine(muLu.FullName, "version.json");
            if (File.Exists(wenJian) && File.Exists(Path.Combine(muLu.FullName, "EVEBox.csproj")))
                return wenJian;
            muLu = muLu.Parent;
        }
        return null;
    }
}

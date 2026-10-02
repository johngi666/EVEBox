using EVEBox.OtherTools.XingXiJuLi;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 星系间距查询的内置数据：三服（曙光/晨曦/宁静）星系数据已编译进 exe
/// （OtherTools\XingXiJuLi\ShuJu\），运行时直接读内置资源，不需要联网、不生成文件。
/// 这里验证六份数据都已嵌入、三服都能完整解析加载。
/// </summary>
public class XingXiJuLiShuJuTests
{
    private static async Task<C3qXingXiShuJuYuan> LoadYuanAsync(XingXiFuWuQi fwq)
    {
        // 数据已内置在程序集里，这里的 HttpClient 只是构造参数，往回收站/网络兜底路径不会走到
        using var http = new HttpClient();
        var yuan = new C3qXingXiShuJuYuan(fwq, http);
        await yuan.LoadAsync(_ => { });
        return yuan;
    }

    [Fact]
    public void 六份数据都已编译进程序集()
    {
        var names = typeof(C3qXingXiShuJuYuan).Assembly.GetManifestResourceNames();
        string[] qiDai =
        {
            "EVEBoxXingXiJuLi/sg/systems_zh.txt",
            "EVEBoxXingXiJuLi/sg/systemposition_zh.txt",
            "EVEBoxXingXiJuLi/gf/systems_zh.txt",
            "EVEBoxXingXiJuLi/gf/systemposition_zh.txt",
            "EVEBoxXingXiJuLi/tq/systems_zh.txt",
            "EVEBoxXingXiJuLi/tq/systemposition_zh.txt",
        };

        foreach (string xiang in qiDai)
        {
            Assert.Contains(names, n =>
                string.Equals(n.Replace('\\', '/'), xiang, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("曙光服")]
    [InlineData("晨曦服")]
    [InlineData("宁静服")]
    public async Task 三服内置星系数据都能完整加载(string fuWuQiMing)
    {
        var fwq = XingXiFuWuQi.AnMingCheng(fuWuQiMing);
        var yuan = await LoadYuanAsync(fwq);

        Assert.Equal(fwq.Name, yuan.ServerName);
        Assert.True(yuan.Systems.Count > 8000, $"{fuWuQiMing} 只加载到 {yuan.Systems.Count} 个星系");
        Assert.All(yuan.Systems, s =>
        {
            Assert.True(s.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(s.Name));
        });

        // 坐标数据不应全为零（抽一个现实存在的星系：坦欧 30000001）
        var tanOu = yuan.Systems.FirstOrDefault(s => s.Id == 30000001);
        Assert.NotNull(tanOu);
        Assert.Equal("坦欧", tanOu.Name);
        Assert.Equal(0.9, tanOu.Security, 3);
        Assert.True(tanOu.X != 0 || tanOu.Y != 0 || tanOu.Z != 0);
    }
}

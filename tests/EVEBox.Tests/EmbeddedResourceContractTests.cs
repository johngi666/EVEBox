using EVEBox.Common.Shared;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 打包与重命名后的「内置资源契约」核对。
///
/// 这些资源名是运行时按前缀读取的（内置模板与星系数据），
/// 文件名/目录英文化时很容易只改了物理路径、忘了逻辑名，导致数据静默丢失。
/// 这里把契约钉死，改名或调整打包路径时能立刻发现。
/// </summary>
public class EmbeddedResourceContractTests
{
    private static string[] ZiYuanMing()
    {
        // 程序集名是带空格的 "EVE BOX"，不能按名字 Load；
        // 用任一类型取所在程序集，与 BuiltInTemplateResource 内部做法一致
        var asm = typeof(RecycleBin).Assembly;
        return asm.GetManifestResourceNames()
            .Select(n => n.Replace('\\', '/'))
            .ToArray();
    }

    [Fact]
    public void 模板资源的逻辑名前缀为_EVEBoxTemplates()
    {
        var names = ZiYuanMing()
            .Where(n => n.StartsWith("EVEBoxTemplates/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(names);

        // 四个模块都必须各有一份模板资源，且模块段用英文目录名
        Assert.Contains(names, n => n.StartsWith("EVEBoxTemplates/ConfigSchemeImport/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.StartsWith("EVEBoxTemplates/PlantingTemplate/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.StartsWith("EVEBoxTemplates/FittingPlan/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.StartsWith("EVEBoxTemplates/OverviewTemplate/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void 星系数据的逻辑名前缀为_EVEBoxSystemDistance()
    {
        var names = ZiYuanMing()
            .Where(n => n.StartsWith("EVEBoxSystemDistance/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // 三服 × 两份文件 = 6 份
        Assert.Equal(6, names.Length);

        foreach (string fu in new[] { "sg", "gf", "tq" })
        {
            Assert.Contains(names, n => n.EndsWith($"{fu}/systems_zh.txt", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.EndsWith($"{fu}/systemposition_zh.txt", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void 不再残留旧的中文资源前缀()
    {
        var names = ZiYuanMing();

        Assert.DoesNotContain(names, n => n.Contains("XingXiJuLi", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("ZhongCaiMoBan", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("ZongLanMoBan", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("ZhuangPeiFangAn", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("PeiZhiFangAnDaoRu", StringComparison.OrdinalIgnoreCase));
    }
}

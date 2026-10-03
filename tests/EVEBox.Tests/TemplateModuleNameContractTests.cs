using EVEBox.OtherTools.ConfigSchemeImport;
using EVEBox.OtherTools.FittingPlan;
using EVEBox.OtherTools.OverviewTemplate;
using EVEBox.OtherTools.PlantingTemplate;
using EVEBox.OtherTools.TemplateImport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 「视图里写的模块名」与「打包进去的资源名」必须一致。
///
/// 背景：模块目录英文化时改了名（ZhuangPeiFangAn → FittingPlan 等），
/// csproj 的 LogicalName 前缀跟着改了，但视图代码里 MoBanMuLu("旧名")
/// 是**字符串字面量**，改名工具只处理标识符、扫不到它，于是两边错开——
/// 表现是内置模板列表空白（用户反馈「装配方案的内置模板没有加载进去」）。
///
/// 这条测试把各视图公开的模块名拿来，核对它在程序集内嵌资源里确实存在。
/// 再出现同类错位会立刻失败。
/// </summary>
public class TemplateModuleNameContractTests
{
    /// <summary>资源逻辑名的统一前缀（与 csproj 的 LogicalName 对应）</summary>
    private const string ZiYuanQianZhui = "EVEBoxTemplates/";

    /// <summary>各模板导入模块的公开模块名</summary>
    public static IEnumerable<(string MingCheng, string MoKuai)> GeMoKuai()
    {
        yield return ("种菜模板导入", PlantingTemplateView.MoKuaiMing);
        yield return ("装配方案导入", FittingPlanView.MoKuaiMing);
        yield return ("总览模板导入", OverviewTemplateView.MoKuaiMing);
        yield return ("配置方案导入", ConfigSchemeImportView.MoKuaiMing);
    }

    private static string[] ZiYuan()
        => typeof(BuiltInTemplateResource).Assembly
            .GetManifestResourceNames()
            .Select(n => n.Replace('\\', '/'))
            .ToArray();

    [Fact]
    public void 每个模板导入模块都有对应的内嵌资源()
    {
        var ziYuan = ZiYuan();
        var wenTi = new List<string>();

        foreach (var (mingCheng, moKuai) in GeMoKuai())
        {
            if (string.IsNullOrEmpty(moKuai))
            {
                wenTi.Add($"{mingCheng}：模块名为空");
                continue;
            }

            bool you = ziYuan.Any(n => n.StartsWith(ZiYuanQianZhui + moKuai + "/", StringComparison.OrdinalIgnoreCase));
            if (!you) wenTi.Add($"{mingCheng}：模块名「{moKuai}」在程序集里没有对应资源");
        }

        Assert.True(wenTi.Count == 0,
            "以下模块名与打包资源不一致（内置模板会加载不出来）：\n  " + string.Join("\n  ", wenTi));
    }

    [Fact]
    public void 每个模板导入模块的资源目录都非空()
    {
        var ziYuan = ZiYuan();

        foreach (var (mingCheng, moKuai) in GeMoKuai())
        {
            var benMoKuai = ziYuan
                .Where(n => n.StartsWith(ZiYuanQianZhui + moKuai + "/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.True(benMoKuai.Length > 0, $"{mingCheng}（{moKuai}）没有任何模板资源");
        }
    }

    [Theory]
    [InlineData("种菜模板导入", "PlantingTemplate")]
    [InlineData("装配方案导入", "FittingPlan")]
    [InlineData("总览模板导入", "OverviewTemplate")]
    [InlineData("配置方案导入", "ConfigSchemeImport")]
    public void 模块名与释放目录名一致且落在文档EVE的templates下(string mingCheng, string yuQiMoKuai)
    {
        var moKuais = GeMoKuai().ToDictionary(x => x.MingCheng, x => x.MoKuai);
        Assert.True(moKuais.TryGetValue(mingCheng, out string? shiJi), $"{mingCheng} 未在清单里");
        Assert.Equal(yuQiMoKuai, shiJi);

        // 释放目录 = 文档\EVE\templates\<模块名>
        string wenDang = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string yuQiMuLu = Path.Combine(wenDang, "EVE", "templates", yuQiMoKuai);
        Assert.Equal(yuQiMuLu, BuiltInTemplateResource.MoBanMuLu(shiJi));
    }
}

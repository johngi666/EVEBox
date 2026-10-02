using EVEBox.OtherTools.TemplateImport;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 内置模板资源：模板文件编进 exe，启动时释放到 文档\EVE\templates\。
/// </summary>
public class BuiltInTemplateResourceTests : IDisposable
{
    private readonly string _root;

    public BuiltInTemplateResourceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_ziyuan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void 资源里确实包含了四个模块的内置模板()
    {
        var ziYuan = BuiltInTemplateResource.LieChuZiYuan();

        Assert.NotEmpty(ziYuan);
        // 资源名应当能被规范化成 templates 下的相对路径
        Assert.All(ziYuan, x => Assert.DoesNotContain("..", x.Value));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("PlantingTemplate/"));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("FittingPlan/"));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("OverviewTemplate/"));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("ConfigSchemeImport/"));
    }

    [Fact]
    public void 缺失时全部写出()
    {
        int shu = BuiltInTemplateResource.QueBaoDaoChu(_root);

        Assert.True(shu > 0);
        foreach (var xiang in BuiltInTemplateResource.LieChuZiYuan())
        {
            string luJing = Path.Combine(_root, xiang.Value.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(luJing), $"应写出：{xiang.Value}");
            Assert.True(new FileInfo(luJing).Length > 0, $"不应为空：{xiang.Value}");
        }
    }

    [Fact]
    public void 内容一致时不重复写()
    {
        BuiltInTemplateResource.QueBaoDaoChu(_root);

        // 再释放一次：文件都在且大小一致，应当一个都不写
        Assert.Equal(0, BuiltInTemplateResource.QueBaoDaoChu(_root));
    }

    [Fact]
    public void 文件损坏或缺失时自动补上()
    {
        BuiltInTemplateResource.QueBaoDaoChu(_root);

        var xiang = BuiltInTemplateResource.LieChuZiYuan().First();
        string luJing = Path.Combine(_root, xiang.Value.Replace('/', Path.DirectorySeparatorChar));

        // 截断成不完整的内容
        File.WriteAllText(luJing, "坏了");
        // 另外删掉一个
        var diErGe = BuiltInTemplateResource.LieChuZiYuan()[1];
        File.Delete(Path.Combine(_root, diErGe.Value.Replace('/', Path.DirectorySeparatorChar)));

        int shu = BuiltInTemplateResource.QueBaoDaoChu(_root);

        Assert.Equal(2, shu);
        Assert.True(new FileInfo(luJing).Length > 3);
        Assert.True(File.Exists(Path.Combine(_root, diErGe.Value.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public void 目标目录不存在时自动创建()
    {
        string shenLuJing = Path.Combine(_root, "a", "b", "templates");

        BuiltInTemplateResource.QueBaoDaoChu(shenLuJing);

        Assert.True(Directory.Exists(shenLuJing));
        Assert.NotEmpty(Directory.GetFiles(shenLuJing, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void 释放位置在文档EVE下()
    {
        string wenDang = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.Equal(Path.Combine(wenDang, "EVE", "templates"), BuiltInTemplateResource.MoBanGenMuLu);
        Assert.Equal(Path.Combine(BuiltInTemplateResource.MoBanGenMuLu, "PlantingTemplate"),
            BuiltInTemplateResource.MoBanMuLu("PlantingTemplate"));
    }

    [Fact]
    public void 旧目录全是内置模板时会被清理()
    {
        string jiu = Path.Combine(_root, "旧");
        BuiltInTemplateResource.QueBaoDaoChu(jiu);

        Assert.Null(BuiltInTemplateResource.ZhaoChuFeiNeiZhiWenJian(jiu));
        Assert.True(BuiltInTemplateResource.QingLiJiuMuLu(jiu));
        Assert.False(Directory.Exists(jiu));
    }

    [Fact]
    public void 旧目录里有用户文件时保留()
    {
        string jiu = Path.Combine(_root, "旧二");
        BuiltInTemplateResource.QueBaoDaoChu(jiu);

        string yongHu = Path.Combine(jiu, "OverviewTemplate", "我自己的.yaml");
        File.WriteAllText(yongHu, "x");

        Assert.Equal(yongHu, BuiltInTemplateResource.ZhaoChuFeiNeiZhiWenJian(jiu));
        Assert.False(BuiltInTemplateResource.QingLiJiuMuLu(jiu));
        Assert.True(File.Exists(yongHu));
    }

    [Fact]
    public void 旧目录里的内置文件被改过也保留()
    {
        string jiu = Path.Combine(_root, "旧三");
        BuiltInTemplateResource.QueBaoDaoChu(jiu);

        var xiang = BuiltInTemplateResource.LieChuZiYuan()[0];
        File.WriteAllText(Path.Combine(jiu, xiang.Value.Replace('/', Path.DirectorySeparatorChar)),
            new string('x', 4096));

        Assert.NotNull(BuiltInTemplateResource.ZhaoChuFeiNeiZhiWenJian(jiu));
        Assert.False(BuiltInTemplateResource.QingLiJiuMuLu(jiu));
        Assert.True(Directory.Exists(jiu));
    }

    [Fact]
    public void 旧目录不存在时不做任何事()
    {
        Assert.False(BuiltInTemplateResource.QingLiJiuMuLu(Path.Combine(_root, "根本不存在")));
    }
}

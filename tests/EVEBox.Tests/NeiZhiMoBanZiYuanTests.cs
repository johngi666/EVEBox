using EVEBox.OtherTools.MoBanDaoRu;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 内置模板资源：模板文件编进 exe，启动时释放到 templates\ 目录。
/// </summary>
public class NeiZhiMoBanZiYuanTests : IDisposable
{
    private readonly string _root;

    public NeiZhiMoBanZiYuanTests()
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
        var ziYuan = NeiZhiMoBanZiYuan.LieChuZiYuan();

        Assert.NotEmpty(ziYuan);
        // 资源名应当能被规范化成 templates 下的相对路径
        Assert.All(ziYuan, x => Assert.DoesNotContain("..", x.Value));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("ZhongCaiMoBan/"));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("ZhuangPeiFangAn/"));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("ZongLanMoBan/"));
        Assert.Contains(ziYuan, x => x.Value.StartsWith("PeiZhiFangAnDaoRu/"));
    }

    [Fact]
    public void 缺失时全部写出()
    {
        int shu = NeiZhiMoBanZiYuan.QueBaoDaoChu(_root);

        Assert.True(shu > 0);
        foreach (var xiang in NeiZhiMoBanZiYuan.LieChuZiYuan())
        {
            string luJing = Path.Combine(_root, xiang.Value.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(luJing), $"应写出：{xiang.Value}");
            Assert.True(new FileInfo(luJing).Length > 0, $"不应为空：{xiang.Value}");
        }
    }

    [Fact]
    public void 内容一致时不重复写()
    {
        NeiZhiMoBanZiYuan.QueBaoDaoChu(_root);

        // 再释放一次：文件都在且大小一致，应当一个都不写
        Assert.Equal(0, NeiZhiMoBanZiYuan.QueBaoDaoChu(_root));
    }

    [Fact]
    public void 文件损坏或缺失时自动补上()
    {
        NeiZhiMoBanZiYuan.QueBaoDaoChu(_root);

        var xiang = NeiZhiMoBanZiYuan.LieChuZiYuan().First();
        string luJing = Path.Combine(_root, xiang.Value.Replace('/', Path.DirectorySeparatorChar));

        // 截断成不完整的内容
        File.WriteAllText(luJing, "坏了");
        // 另外删掉一个
        var diErGe = NeiZhiMoBanZiYuan.LieChuZiYuan()[1];
        File.Delete(Path.Combine(_root, diErGe.Value.Replace('/', Path.DirectorySeparatorChar)));

        int shu = NeiZhiMoBanZiYuan.QueBaoDaoChu(_root);

        Assert.Equal(2, shu);
        Assert.True(new FileInfo(luJing).Length > 3);
        Assert.True(File.Exists(Path.Combine(_root, diErGe.Value.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public void 目标目录不存在时自动创建()
    {
        string shenLuJing = Path.Combine(_root, "a", "b", "templates");

        NeiZhiMoBanZiYuan.QueBaoDaoChu(shenLuJing);

        Assert.True(Directory.Exists(shenLuJing));
        Assert.NotEmpty(Directory.GetFiles(shenLuJing, "*", SearchOption.AllDirectories));
    }
}

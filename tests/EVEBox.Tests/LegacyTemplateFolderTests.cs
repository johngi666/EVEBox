using EVEBox.OtherTools.TemplateImport;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 模板模块目录英文化后的旧目录迁移。
///
/// 背景：模块目录改名（如 ZhongCaiMoBan → PlantingTemplate）后，
/// 老用户的 文档\EVE\templates 下会留着旧中文目录，与新目录并排出现重复模板。
/// 启动时要清掉旧目录，但**只清内容全是内置原件的**——
/// 用户自己往里放的文件必须保留（与程序目录旧 templates 的处理一致）。
/// </summary>
public class LegacyTemplateFolderTests : IDisposable
{
    private readonly string _root;

    public LegacyTemplateFolderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_legacy_tpl_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    /// <summary>把内置模板按「旧中文模块名」释放一份，模拟老用户升级前留下的目录。</summary>
    private void FangZhiJiuMuLu(string jiuMoKuai, string xinMoKuai)
    {
        foreach (var xiang in BuiltInTemplateResource.LieChuZiYuan())
        {
            string qianZhui = xinMoKuai + "/";
            if (!xiang.Value.StartsWith(qianZhui, StringComparison.OrdinalIgnoreCase)) continue;

            string xiangDui = xiang.Value.Substring(qianZhui.Length).Replace('/', Path.DirectorySeparatorChar);
            string muBiao = Path.Combine(_root, jiuMoKuai, xiangDui);
            Directory.CreateDirectory(Path.GetDirectoryName(muBiao)!);
            File.WriteAllText(muBiao, "内置内容占位");
        }
    }

    [Fact]
    public void 旧中文目录里全是内置原件时会被清理()
    {
        FangZhiJiuMuLu("ZhongCaiMoBan", "PlantingTemplate");

        // 按大小比对，所以占位内容长度要一致才能被判为「内置原件」；
        // 这里直接改为真实释放，确保比对通过
        Directory.Delete(Path.Combine(_root, "ZhongCaiMoBan"), true);
        BuiltInTemplateResource.QueBaoDaoChu(_root);
        // 复制一份到旧目录名下，内容与内置完全一致
        CopyDir(Path.Combine(_root, "PlantingTemplate"), Path.Combine(_root, "ZhongCaiMoBan"));

        int qingLi = BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root);

        Assert.Equal(1, qingLi);
        Assert.False(Directory.Exists(Path.Combine(_root, "ZhongCaiMoBan")), "旧目录应已被清理");
        Assert.True(Directory.Exists(Path.Combine(_root, "PlantingTemplate")), "新目录必须保留");
    }

    [Fact]
    public void 旧目录里有用户自加文件时保留整个目录()
    {
        BuiltInTemplateResource.QueBaoDaoChu(_root);
        CopyDir(Path.Combine(_root, "PlantingTemplate"), Path.Combine(_root, "ZhongCaiMoBan"));

        // 用户自己放了一个模板
        File.WriteAllText(Path.Combine(_root, "ZhongCaiMoBan", "我的自定义模板.json"), "{}");

        int qingLi = BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root);

        Assert.Equal(0, qingLi);
        Assert.True(Directory.Exists(Path.Combine(_root, "ZhongCaiMoBan")), "含用户文件的旧目录必须保留");
        Assert.True(File.Exists(Path.Combine(_root, "ZhongCaiMoBan", "我的自定义模板.json")));
    }

    [Fact]
    public void 没有旧目录时不误报()
    {
        BuiltInTemplateResource.QueBaoDaoChu(_root);

        Assert.Equal(0, BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root));
    }

    [Fact]
    public void 旧目录本身不存在时被跳过()
    {
        // 只建新目录，不建任何旧目录
        Directory.CreateDirectory(Path.Combine(_root, "PlantingTemplate"));

        Assert.Equal(0, BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root));
    }

    [Theory]
    [InlineData("ZhongCaiMoBan", "PlantingTemplate")]
    [InlineData("ZongLanMoBan", "OverviewTemplate")]
    public void 改过名的模块旧目录会被识别并清理(string jiuMoKuai, string xinMoKuai)
    {
        BuiltInTemplateResource.QueBaoDaoChu(_root);
        CopyDir(Path.Combine(_root, xinMoKuai), Path.Combine(_root, jiuMoKuai));

        int qingLi = BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root);

        Assert.Equal(1, qingLi);
        Assert.False(Directory.Exists(Path.Combine(_root, jiuMoKuai)));
        Assert.True(Directory.Exists(Path.Combine(_root, xinMoKuai)));
    }

    [Theory]
    [InlineData("PeiZhiFangAnDaoRu")]
    [InlineData("ZhuangPeiFangAn")]
    public void 非内置模块的同名目录不会被清理(string feiNeiZhiMuKuai)
    {
        // 回归用例：这两个目录名历史上出现过，但它们并不是内置模块
        //（内置模块只有 ConfigSchemeImport / PlantingTemplate / FittingPlan / OverviewTemplate）。
        // 绝不能把它们写进「旧名映射」去删——曾这么干过，把用户机器上的目录清掉了。
        Directory.CreateDirectory(Path.Combine(_root, feiNeiZhiMuKuai));
        File.WriteAllText(Path.Combine(_root, feiNeiZhiMuKuai, "留存.txt"), "内容");

        int qingLi = BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root);

        Assert.Equal(0, qingLi);
        Assert.True(Directory.Exists(Path.Combine(_root, feiNeiZhiMuKuai)), "非内置模块目录不应被清理");
        Assert.True(File.Exists(Path.Combine(_root, feiNeiZhiMuKuai, "留存.txt")));
    }

    [Fact]
    public void 新名目录不存在时不动旧名目录()
    {
        // 安全闸：新名目录还没被释放过（例如用户回退到老版本运行），
        // 此时旧目录仍是有效数据，删掉会变成空壳。
        BuiltInTemplateResource.QueBaoDaoChu(_root);
        // 只留旧目录，把新目录移走
        Directory.Move(Path.Combine(_root, "PlantingTemplate"), Path.Combine(_root, "ZhongCaiMoBan"));

        int qingLi = BuiltInTemplateResource.QingLiJiuMoKuaiMuLu(_root);

        Assert.Equal(0, qingLi);
        Assert.True(Directory.Exists(Path.Combine(_root, "ZhongCaiMoBan")), "新目录不存在时旧目录必须保留");
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(_root, "ZhongCaiMoBan"), "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void 按模块名比对时不看模块目录本身叫什么()
    {
        BuiltInTemplateResource.QueBaoDaoChu(_root);

        // 把新目录整个复制成旧名字：内容一致，应当判为「全是内置原件」
        CopyDir(Path.Combine(_root, "PlantingTemplate"), Path.Combine(_root, "ZhongCaiMoBan"));
        Assert.Null(BuiltInTemplateResource.ZhaoChuFeiNeiZhiWenJian(
            Path.Combine(_root, "ZhongCaiMoBan"), "PlantingTemplate"));

        // 掺一个用户文件后就应当被查出来
        File.WriteAllText(Path.Combine(_root, "ZhongCaiMoBan", "user.json"), "{}");
        Assert.NotNull(BuiltInTemplateResource.ZhaoChuFeiNeiZhiWenJian(
            Path.Combine(_root, "ZhongCaiMoBan"), "PlantingTemplate"));
    }

    private static void CopyDir(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (string dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(src, dst));
        foreach (string f in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            string target = f.Replace(src, dst);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(f, target, overwrite: true);
        }
    }
}

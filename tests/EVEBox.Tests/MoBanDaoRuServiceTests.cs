using EVEBox.OtherTools.MoBanDaoRu;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EVEBox.Tests;

public class MoBanDaoRuServiceTests : IDisposable
{
    private readonly string _root;

    public MoBanDaoRuServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_muban_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private MoBanDaoRuService CreateService(bool neiZhiWeiWenJianJia, string guoLv = "*.json")
        => new MoBanDaoRuService(
            Path.Combine(_root, "target"),
            guoLv,
            Path.Combine(_root, "builtin"),
            neiZhiWeiWenJianJia);

    private string Builtin => Path.Combine(_root, "builtin");

    private string Target => Path.Combine(_root, "target");

    [Fact]
    public void Constructor_CreatesTargetAndBuiltinFolders()
    {
        var service = CreateService(false);

        Assert.True(Directory.Exists(service.TargetFolder));
        Assert.True(Directory.Exists(service.BuiltinFolder));
    }

    [Fact]
    public void ListBuiltinItems_FolderMode_ListsSubFolders()
    {
        var service = CreateService(true);
        Directory.CreateDirectory(Path.Combine(Builtin, "模板包A"));
        Directory.CreateDirectory(Path.Combine(Builtin, "模板包B"));
        // 内置目录里的散装文件不应该出现在文件夹型列表里
        File.WriteAllText(Path.Combine(Builtin, "散装.json"), "{}");

        var items = service.ListBuiltinItems();

        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.True(x.ShiWenJianJia));
        Assert.Contains(items, x => x.MingCheng == "模板包A");
        Assert.Contains(items, x => x.MingCheng == "模板包B");
    }

    [Fact]
    public void ListBuiltinItems_FileMode_ListsMatchingFilesOnly()
    {
        var service = CreateService(false, "*.xml");
        File.WriteAllText(Path.Combine(Builtin, "装配1.xml"), "<x/>");
        File.WriteAllText(Path.Combine(Builtin, "不该出现.json"), "{}");
        Directory.CreateDirectory(Path.Combine(Builtin, "子目录"));

        var items = service.ListBuiltinItems();

        Assert.Single(items);
        Assert.Equal("装配1.xml", items[0].MingCheng);
        Assert.False(items[0].ShiWenJianJia);
    }

    [Fact]
    public void Import_FolderMode_CopiesAllJsonRecursively()
    {
        var service = CreateService(true);
        string bao = Path.Combine(Builtin, "模板包A");
        Directory.CreateDirectory(Path.Combine(bao, "子目录"));
        File.WriteAllText(Path.Combine(bao, "a.json"), "{\"a\":1}");
        File.WriteAllText(Path.Combine(bao, "b.json"), "{\"b\":1}");
        File.WriteAllText(Path.Combine(bao, "子目录", "c.json"), "{\"c\":1}");
        File.WriteAllText(Path.Combine(bao, "忽略.txt"), "x");

        int shu = service.Import(bao, null);

        Assert.Equal(3, shu);
        Assert.True(File.Exists(Path.Combine(Target, "a.json")));
        Assert.True(File.Exists(Path.Combine(Target, "b.json")));
        // 子目录里的模板被平铺复制到目标目录
        Assert.True(File.Exists(Path.Combine(Target, "c.json")));
        Assert.False(File.Exists(Path.Combine(Target, "忽略.txt")));
    }

    [Fact]
    public void Import_FileMode_CopiesSingleFile()
    {
        var service = CreateService(false, "*.xml");
        string file = Path.Combine(Builtin, "装配1.xml");
        File.WriteAllText(file, "<x/>");

        int shu = service.Import(file, null);

        Assert.Equal(1, shu);
        Assert.Equal("<x/>", File.ReadAllText(Path.Combine(Target, "装配1.xml")));
    }

    [Fact]
    public void Import_FolderMode_NoMatchingFile_ReturnsZero()
    {
        var service = CreateService(true);
        string bao = Path.Combine(Builtin, "空包");
        Directory.CreateDirectory(bao);
        File.WriteAllText(Path.Combine(bao, "只有文本.txt"), "x");

        Assert.Equal(0, service.Import(bao, null));
        Assert.Empty(Directory.GetFiles(Target));
    }

    [Fact]
    public void Import_FolderMode_OverwritesExistingTargetFile()
    {
        var service = CreateService(true);
        string bao = Path.Combine(Builtin, "模板包A");
        Directory.CreateDirectory(bao);
        File.WriteAllText(Path.Combine(bao, "a.json"), "新版");
        Directory.CreateDirectory(Target);
        File.WriteAllText(Path.Combine(Target, "a.json"), "旧版");

        service.Import(bao, null);

        Assert.Equal("新版", File.ReadAllText(Path.Combine(Target, "a.json")));
    }

    [Fact]
    public void ListTargetFiles_ReturnsImportedFileNames()
    {
        var service = CreateService(true);
        string bao = Path.Combine(Builtin, "模板包A");
        Directory.CreateDirectory(bao);
        File.WriteAllText(Path.Combine(bao, "a.json"), "{}");
        service.Import(bao, null);

        Assert.Equal(new[] { "a.json" }, service.ListTargetFiles().ToArray());
    }

    [Fact]
    public void Import_MissingSource_Throws()
    {
        var service = CreateService(false, "*.xml");

        Assert.Throws<FileNotFoundException>(
            () => service.Import(Path.Combine(Builtin, "不存在.xml"), null));
    }
}

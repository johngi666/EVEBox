using EVEBox.OtherTools.PeiZhiFangAnDaoRu;
using System;
using System.IO;
using Xunit;

namespace EVEBox.Tests;

public class PeiZhiFangAnDaoRuServiceTests : IDisposable
{
    private readonly string _root;

    public PeiZhiFangAnDaoRuServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_fangan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string FangAn => Path.Combine(_root, "fangan");

    private string MuBiao => Path.Combine(_root, "settings_Default");

    private PeiZhiFangAnDaoRuService CreateService()
    {
        Directory.CreateDirectory(MuBiao);
        return new PeiZhiFangAnDaoRuService(Path.Combine(_root, "builtin"));
    }

    private static void WriteFile(string path, string content, DateTime time)
    {
        File.WriteAllText(path, content);
        File.SetLastWriteTime(path, time);
    }

    [Fact]
    public void Constructor_CreatesBuiltinFolder()
    {
        var service = CreateService();

        Assert.True(Directory.Exists(service.NeiZhiFangAnMuLu));
    }

    [Fact]
    public void LieChuNeiZhiFangAn_ListsSubFoldersWithSummary()
    {
        var service = CreateService();
        string youXiao = Path.Combine(service.NeiZhiFangAnMuLu, "方案A");
        string kongDe = Path.Combine(service.NeiZhiFangAnMuLu, "方案B");
        Directory.CreateDirectory(youXiao);
        Directory.CreateDirectory(kongDe);
        File.WriteAllText(Path.Combine(youXiao, "core_char_1.dat"), "x");
        File.WriteAllText(Path.Combine(youXiao, "core_user_1.dat"), "y");

        var list = service.LieChuNeiZhiFangAn();

        Assert.Equal(2, list.Count);
        Assert.Equal("方案A", list[0].MingCheng);
        // 一个方案包 = 一个 char 文件 + 一个 user 文件
        Assert.Equal("char 文件 1 个 / user 文件 1 个", list[0].ZhaiYao);
        Assert.True(PeiZhiFangAnDaoRuService.ShiYouXiaoFangAn(youXiao));

        // 空文件夹仍然列出，但摘要为空，界面会显示成「非配置方案」
        Assert.Equal("", list[1].ZhaiYao);
        Assert.False(PeiZhiFangAnDaoRuService.ShiYouXiaoFangAn(kongDe));
    }

    [Fact]
    public void ZhaiYao_EmptyForMissingFolder()
    {
        Assert.Equal("", PeiZhiFangAnDaoRuService.ZhaiYao(null));
        Assert.Equal("", PeiZhiFangAnDaoRuService.ZhaiYao(Path.Combine(_root, "不存在")));
    }

    [Fact]
    public void YingYong_OverwritesAllSameTypeFilesInTarget()
    {
        var service = CreateService();
        Directory.CreateDirectory(FangAn);
        WriteFile(Path.Combine(FangAn, "core_char_99.dat"), "方案角色配置", new DateTime(2024, 1, 1));
        WriteFile(Path.Combine(FangAn, "core_user_99.dat"), "方案账号配置", new DateTime(2024, 1, 1));

        WriteFile(Path.Combine(MuBiao, "core_char_1.dat"), "旧角色1", new DateTime(2024, 2, 1));
        WriteFile(Path.Combine(MuBiao, "core_char_2.dat"), "旧角色2", new DateTime(2024, 2, 1));
        WriteFile(Path.Combine(MuBiao, "core_user_1.dat"), "旧账号", new DateTime(2024, 2, 1));
        File.WriteAllText(Path.Combine(MuBiao, "其他文件.txt"), "不该被动");

        var jieGuo = service.YingYong(FangAn, MuBiao, null);

        // 2 个角色文件 + 1 个用户文件被方案里最新的同类型文件覆盖
        Assert.Equal(3, jieGuo.TiHuanShu);
        Assert.Equal(0, jieGuo.XinZengShu);
        Assert.Equal(0, jieGuo.TiaoGuoShu);
        Assert.True(jieGuo.ChengGong);

        Assert.Equal("方案角色配置", File.ReadAllText(Path.Combine(MuBiao, "core_char_1.dat")));
        Assert.Equal("方案角色配置", File.ReadAllText(Path.Combine(MuBiao, "core_char_2.dat")));
        Assert.Equal("方案账号配置", File.ReadAllText(Path.Combine(MuBiao, "core_user_1.dat")));
        Assert.Equal("不该被动", File.ReadAllText(Path.Combine(MuBiao, "其他文件.txt")));

        // 目标里不引入方案里那个角色 id 的新文件
        Assert.False(File.Exists(Path.Combine(MuBiao, "core_char_99.dat")));
        Assert.False(File.Exists(Path.Combine(MuBiao, "core_user_99.dat")));
    }

    [Fact]
    public void YingYong_UsesNewestFileInScheme()
    {
        var service = CreateService();
        Directory.CreateDirectory(FangAn);
        WriteFile(Path.Combine(FangAn, "core_char_1.dat"), "方案里的旧角色", new DateTime(2024, 1, 1));
        WriteFile(Path.Combine(FangAn, "core_char_2.dat"), "方案里的新角色", new DateTime(2024, 3, 1));
        WriteFile(Path.Combine(MuBiao, "core_char_7.dat"), "目标角色", new DateTime(2024, 2, 1));

        service.YingYong(FangAn, MuBiao, null);

        Assert.Equal("方案里的新角色", File.ReadAllText(Path.Combine(MuBiao, "core_char_7.dat")));
    }

    [Fact]
    public void YingYong_AddsFileWhenTargetHasNoneOfThatType()
    {
        var service = CreateService();
        Directory.CreateDirectory(FangAn);
        WriteFile(Path.Combine(FangAn, "core_user_5.dat"), "方案账号配置", new DateTime(2024, 1, 1));

        var jieGuo = service.YingYong(FangAn, MuBiao, null);

        Assert.Equal(0, jieGuo.TiHuanShu);
        Assert.Equal(1, jieGuo.XinZengShu);
        Assert.True(jieGuo.ChengGong);
        Assert.Equal("方案账号配置", File.ReadAllText(Path.Combine(MuBiao, "core_user_5.dat")));
    }

    [Fact]
    public void YingYong_SkipsLockedTargetFile()
    {
        var service = CreateService();
        Directory.CreateDirectory(FangAn);
        WriteFile(Path.Combine(FangAn, "core_char_9.dat"), "方案角色配置", new DateTime(2024, 1, 1));

        string beiZhanYong = Path.Combine(MuBiao, "core_char_1.dat");
        WriteFile(beiZhanYong, "旧角色1", new DateTime(2024, 2, 1));
        WriteFile(Path.Combine(MuBiao, "core_char_2.dat"), "旧角色2", new DateTime(2024, 2, 1));

        DaoRuJieGuo jieGuo;
        using (File.Open(beiZhanYong, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            jieGuo = service.YingYong(FangAn, MuBiao, null);
        }

        Assert.Equal(1, jieGuo.TiHuanShu);
        Assert.Equal(1, jieGuo.TiaoGuoShu);
        Assert.Equal("旧角色1", File.ReadAllText(beiZhanYong));
        Assert.Equal("方案角色配置", File.ReadAllText(Path.Combine(MuBiao, "core_char_2.dat")));
    }

    [Fact]
    public void YingYong_RejectsMissingFolderAndSameFolder()
    {
        var service = CreateService();

        Assert.Throws<DirectoryNotFoundException>(
            () => service.YingYong(Path.Combine(_root, "不存在"), MuBiao, null));
        Assert.Throws<DirectoryNotFoundException>(
            () => service.YingYong(FangAn, Path.Combine(_root, "不存在"), null));

        Directory.CreateDirectory(FangAn);
        File.WriteAllText(Path.Combine(FangAn, "core_char_1.dat"), "x");

        Assert.Throws<InvalidOperationException>(() => service.YingYong(FangAn, FangAn, null));
    }

    [Fact]
    public void IsFileLocked_TrueWhileFileHeldOpen()
    {
        Directory.CreateDirectory(MuBiao);
        string wenJian = Path.Combine(MuBiao, "core_char_1.dat");
        File.WriteAllText(wenJian, "x");

        Assert.False(PeiZhiFangAnDaoRuService.IsFileLocked(wenJian));

        using (File.Open(wenJian, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.True(PeiZhiFangAnDaoRuService.IsFileLocked(wenJian));
        }

        Assert.False(PeiZhiFangAnDaoRuService.IsFileLocked(wenJian));
    }
}

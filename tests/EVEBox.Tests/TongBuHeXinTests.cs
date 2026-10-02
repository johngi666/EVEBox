using EVEBox.Features.PeiZhiTongBu;
using System;
using System.IO;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 配置同步的盘点与规则判断。
///
/// 这些规则原先埋在 TongBuService.SyncAllFilesAsync 里和弹窗混在一起，
/// 「什么情况下该拒绝覆盖」无法单独验证。抽出 TongBuHeXin 后可以逐条钉住。
/// </summary>
public class TongBuHeXinTests : IDisposable
{
    private readonly string _root;
    private readonly TongBuHeXin _core = new TongBuHeXin();

    public TongBuHeXinTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_tongbu_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private void Touch(string fileName, DateTime? time = null)
    {
        string full = Path.Combine(_root, fileName);
        File.WriteAllText(full, "x");
        if (time.HasValue) File.SetLastWriteTime(full, time.Value);
    }

    [Fact]
    public void MissingFolderCannotSync()
    {
        var result = _core.Inventory(Path.Combine(_root, "not_exist"));

        Assert.False(result.CanSync);
        Assert.Contains("配置文件夹", result.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyFolderCannotSync(string folder)
    {
        var result = _core.Inventory(folder);

        Assert.False(result.CanSync);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void SingleFileOfEachTypeIsNotEnough()
    {
        Touch("core_user_1.dat");
        Touch("core_char_1.dat");

        var result = _core.Inventory(_root);

        Assert.False(result.CanSync);
        Assert.Contains("至少需要2个文件", result.Message);
    }

    [Fact]
    public void TwoUserFilesAreEnoughToSync()
    {
        Touch("core_user_1.dat");
        Touch("core_user_2.dat");

        var result = _core.Inventory(_root);

        Assert.True(result.CanSync, result.Message);
        Assert.Equal(2, result.UserFiles.Count);
        Assert.Empty(result.CharFiles);
    }

    [Fact]
    public void TwoCharFilesAreEnoughToSync()
    {
        Touch("core_char_1.dat");
        Touch("core_char_2.dat");

        var result = _core.Inventory(_root);

        Assert.True(result.CanSync, result.Message);
        Assert.Equal(2, result.CharFiles.Count);
    }

    [Fact]
    public void TemplateFilesWithoutNumberAreExcluded()
    {
        // core_user_.dat / core_char_.dat 是模板文件，不参与同步
        Touch("core_user_.dat");
        Touch("core_char_.dat");
        Touch("core_user_1.dat");
        Touch("core_user_2.dat");

        var result = _core.Inventory(_root);

        Assert.DoesNotContain("core_user_.dat", result.UserFiles);
        Assert.DoesNotContain("core_char_.dat", result.CharFiles);
    }

    [Fact]
    public void AbnormalFileNamesAreExcluded()
    {
        Touch("core_user_1.dat");
        Touch("core_user_2.dat");
        // 混入一个异常名（含 ".."），应当被过滤掉
        Touch("core_user_3..dat");

        var result = _core.Inventory(_root);

        Assert.True(result.CanSync, result.Message);
        Assert.DoesNotContain("core_user_3..dat", result.UserFiles);
    }

    [Fact]
    public void LatestFileIsPickedByWriteTime()
    {
        var old = new DateTime(2026, 1, 1, 10, 0, 0);
        var recent = new DateTime(2026, 9, 1, 10, 0, 0);

        Touch("core_user_1.dat", old);
        Touch("core_user_2.dat", recent);

        var result = _core.Inventory(_root);

        Assert.True(result.CanSync, result.Message);
        Assert.Equal("core_user_2.dat", result.LatestUserFile);
    }

    [Fact]
    public void NonMatchingFilesAreIgnored()
    {
        Touch("core_user_1.dat");
        Touch("core_user_2.dat");
        Touch("settings_Default");
        Touch("readme.txt");

        var result = _core.Inventory(_root);

        Assert.Equal(2, result.UserFiles.Count);
    }

    [Theory]
    [InlineData("core_user_1.dat", false)]
    [InlineData("core_char_2.dat", false)]
    [InlineData("core_user_1..dat", true)]
    [InlineData("('user'1.dat", true)]
    [InlineData("('char'2.dat", true)]
    [InlineData("None.dat", true)]
    [InlineData("x.dat.dat", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    public void AbnormalNameDetection(string fileName, bool expected)
    {
        Assert.Equal(expected, _core.IsAbnormalFileName(fileName));
    }

    [Theory]
    [InlineData("该文件正被另一进程使用 used by another process", true)]
    [InlineData("文件被占用", true)]
    [InlineData("磁盘空间不足", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void FileInUseDetection(string message, bool expected)
    {
        Assert.Equal(expected, _core.IsFileInUseError(message));
    }
}

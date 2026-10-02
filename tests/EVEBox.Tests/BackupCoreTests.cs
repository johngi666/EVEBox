using EVEBox.Features.Backup;
using EVEBox.Features.ConfigSync;
using System;
using System.IO;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 备份/还原的纯业务逻辑。
///
/// 这些用例原先写不出来：逻辑和弹窗混在 BeiFenService 里，
/// 一跑就会弹 WinForms 对话框。抽出 BeiFenHeXin 后业务判断可以独立验证，
/// 界面层只剩「问一句 → 调这里 → 提示一句」。
/// </summary>
public class BackupCoreTests : IDisposable
{
    private readonly string _root;
    private readonly BackupCore _core = new BackupCore();

    public BackupCoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_beifen_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string PathOf(string name) => Path.Combine(_root, name);

    // ---------------- 整目录备份 ----------------

    [Fact]
    public void WholeFolderBackupReturnsBackupPath()
    {
        string source = PathOf("config");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "core_user_1.dat"), "数据");
        string backupRoot = PathOf("backup");

        var result = _core.BackupWholeFolder(
            source,
            basePath =>
            {
                string target = Path.Combine(basePath, "backup_20261002");
                Directory.CreateDirectory(target);
                foreach (string f in Directory.GetFiles(source))
                    File.Copy(f, Path.Combine(target, Path.GetFileName(f)), true);
                return target;
            },
            backupRoot);

        Assert.True(result.Success, result.Message);
        Assert.Null(result.Message);
        Assert.Equal(Path.Combine(backupRoot, "backup_20261002"), result.Path);
        Assert.True(File.Exists(Path.Combine(result.Path, "core_user_1.dat")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WholeFolderBackupFailsWithoutCurrentFolder(string currentFolder)
    {
        var result = _core.BackupWholeFolder(currentFolder, _ => PathOf("x"), PathOf("backup"));

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void WholeFolderBackupFailsWhenCurrentFolderMissing()
    {
        var result = _core.BackupWholeFolder(
            PathOf("not_exist"), _ => PathOf("x"), PathOf("backup"));

        Assert.False(result.Success);
    }

    [Fact]
    public void WholeFolderBackupTurnsExceptionIntoFailureResult()
    {
        string source = PathOf("config2");
        Directory.CreateDirectory(source);

        var result = _core.BackupWholeFolder(
            source,
            _ => throw new IOException("磁盘满了"),
            PathOf("backup"));

        Assert.False(result.Success);
        Assert.Contains("磁盘满了", result.Message);
    }

    [Fact]
    public void WholeFolderBackupFailsWhenActionReturnsNoPath()
    {
        string source = PathOf("config3");
        Directory.CreateDirectory(source);

        var result = _core.BackupWholeFolder(source, _ => null, PathOf("backup"));

        Assert.False(result.Success);
    }

    // ---------------- 单文件备份 ----------------

    [Fact]
    public void SingleFileBackupCreatesFolderAndOverwritesSameName()
    {
        string file = PathOf("core_user_7.dat");
        File.WriteAllText(file, "新内容");
        string backupRoot = PathOf("backup_single");

        // 预先放一个同名旧备份，验证是覆盖而不是报错
        Directory.CreateDirectory(backupRoot);
        File.WriteAllText(Path.Combine(backupRoot, "core_user_7.dat"), "旧内容");

        var result = _core.BackupSingleFile(file, backupRoot);

        Assert.True(result.Success, result.Message);
        Assert.Equal("新内容", File.ReadAllText(result.Path));
    }

    [Fact]
    public void SingleFileBackupRefreshesLastWriteTime()
    {
        string file = PathOf("core_user_8.dat");
        File.WriteAllText(file, "内容");
        File.SetLastWriteTime(file, DateTime.Now.AddDays(-30));

        var result = _core.BackupSingleFile(file, PathOf("backup_time"));

        Assert.True(result.Success, result.Message);
        // 备份时间应当接近现在，而不是沿用源文件的旧时间
        Assert.True((DateTime.Now - File.GetLastWriteTime(result.Path)).TotalMinutes < 5,
            "备份文件时间应当刷新为当前时间");
    }

    [Fact]
    public void SingleFileBackupFailsWhenSourceMissing()
    {
        var result = _core.BackupSingleFile(PathOf("missing.dat"), PathOf("backup"));

        Assert.False(result.Success);
        Assert.Contains("不存在", result.Message);
    }

    [Fact]
    public void SingleFileBackupCreatesMissingTargetFolder()
    {
        string file = PathOf("core_user_9.dat");
        File.WriteAllText(file, "内容");
        string newFolder = PathOf("auto_created");

        var result = _core.BackupSingleFile(file, newFolder);

        Assert.True(result.Success, result.Message);
        Assert.True(Directory.Exists(newFolder));
    }

    // ---------------- 还原 ----------------

    [Fact]
    public void RestoreSingleFileOverwritesTargetFile()
    {
        string backupFile = PathOf("backup_file.dat");
        File.WriteAllText(backupFile, "备份内容");
        string target = PathOf("target");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "backup_file.dat"), "被覆盖内容");

        BackupItem item = new BackupItem { Name = "backup_file.dat", Path = backupFile, IsFile = true };
        var result = _core.Restore(item, target, null);

        Assert.True(result.Success, result.Message);
        Assert.Equal("备份内容", File.ReadAllText(Path.Combine(target, "backup_file.dat")));
    }

    [Fact]
    public void RestoreFolderInvokesCopyDirectoryAction()
    {
        string target = PathOf("target_folder");
        Directory.CreateDirectory(target);
        bool invoked = false;

        BackupItem item = new BackupItem { Name = "backup_dir", Path = PathOf("backup_dir"), IsFile = false };
        var result = _core.Restore(item, target, (source, destination) =>
        {
            invoked = true;
            Assert.Equal(item.Path, source);
            Assert.Equal(target, destination);
        });

        Assert.True(result.Success, result.Message);
        Assert.True(invoked, "目录备份应当走目录复制动作");
    }

    [Fact]
    public void RestoreFailsWhenTargetFolderInvalid()
    {
        BackupItem item = new BackupItem { Name = "x.dat", Path = PathOf("x.dat"), IsFile = true };

        var result = _core.Restore(item, PathOf("not_exist"), null);

        Assert.False(result.Success);
    }

    [Fact]
    public void RestoreFailsWhenItemIsNull()
    {
        string target = PathOf("target2");
        Directory.CreateDirectory(target);

        var result = _core.Restore(null, target, null);

        Assert.False(result.Success);
    }

    [Fact]
    public void RestoreFolderFailsWhenCopyActionMissing()
    {
        string target = PathOf("target3");
        Directory.CreateDirectory(target);

        BackupItem item = new BackupItem { Name = "d", Path = PathOf("d"), IsFile = false };
        var result = _core.Restore(item, target, null);

        Assert.False(result.Success);
    }

    [Fact]
    public void RestoreTurnsCopyExceptionIntoFailureResult()
    {
        string target = PathOf("target4");
        Directory.CreateDirectory(target);

        BackupItem item = new BackupItem { Name = "d", Path = PathOf("d"), IsFile = false };
        var result = _core.Restore(item, target, (_, __) => throw new IOException("目标被占用"));

        Assert.False(result.Success);
        Assert.Contains("目标被占用", result.Message);
    }
}

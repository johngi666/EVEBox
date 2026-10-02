using EVEBox.Common.GongYong;
using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 删除必须走回收站（项目规则第 1 条）。
///
/// 这里不只验证「文件消失了」——永久删除同样会让文件消失，
/// 而是直接检查回收站目录里出现了对应的元数据记录，以此区分
/// 「送进回收站」和「被永久抹掉」两种行为。
/// 回收站条目按 当前用户 SID\$I&lt;随机&gt;（元数据）+ $R&lt;随机&gt;（内容）成对存放，
/// $I 文件内偏移 28 起是原始路径（UTF-16LE），据此比对。
/// </summary>
public class HuiShouZhanTests : IDisposable
{
    private readonly string _root;

    public HuiShouZhanTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_huishouzhan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        // 测试目录本身也可能已被送进回收站，存在才清
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void DeletedFileGoesToRecycleBinInsteadOfBeingErased()
    {
        string path = Path.Combine(_root, $"deleted_{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "回收站测试内容", Encoding.UTF8);

        bool succeeded = HuiShouZhan.ShanChu(path, out string error);

        Assert.True(succeeded, $"应当删除成功，实际失败：{error}");
        Assert.Null(error);
        Assert.False(File.Exists(path), "源文件应当已不在原位置");
        Assert.True(ExistsInRecycleBin(path), "源文件应当出现在回收站中（$I 元数据里记录了它的原路径）");
    }

    [Fact]
    public void DeletedDirectoryGoesToRecycleBinWithItsContents()
    {
        string folder = Path.Combine(_root, $"deleted_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "nested"));
        File.WriteAllText(Path.Combine(folder, "a.txt"), "甲");
        File.WriteAllText(Path.Combine(folder, "nested", "b.txt"), "乙");

        bool succeeded = HuiShouZhan.ShanChu(folder, out string error);

        Assert.True(succeeded, $"应当删除成功，实际失败：{error}");
        Assert.False(Directory.Exists(folder), "源目录应当已不在原位置");
        Assert.True(ExistsInRecycleBin(folder), "源目录应当出现在回收站中");
    }

    [Fact]
    public void MissingPathIsTreatedAsAlreadyDone()
    {
        string path = Path.Combine(_root, "not_exist_" + Guid.NewGuid().ToString("N") + ".txt");

        bool succeeded = HuiShouZhan.ShanChu(path, out string error);

        Assert.True(succeeded, "目标本就不存在，应当按已完成处理");
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPathIsRejected(string path)
    {
        bool succeeded = HuiShouZhan.ShanChu(path, out string error);

        Assert.False(succeeded, "空路径不应当被当成删除成功");
        Assert.False(string.IsNullOrWhiteSpace(error), "失败时应当给出原因");
    }

    /// <summary>
    /// 在回收站里查找某个原路径的记录。
    /// 读不到回收站目录时（权限/环境异常）返回 false，让用例如实失败而不是蒙混过关。
    /// </summary>
    private static bool ExistsInRecycleBin(string originalPath)
    {
        if (string.IsNullOrWhiteSpace(originalPath)) return false;

        string? driveRoot = Path.GetPathRoot(originalPath);

        if (string.IsNullOrEmpty(driveRoot)) return false;

        string? sid = WindowsIdentity.GetCurrent().User?.Value;

        if (string.IsNullOrEmpty(sid)) return false;

        string binRoot = Path.Combine(driveRoot, "$Recycle.Bin", sid);

        if (!Directory.Exists(binRoot)) return false;

        // $I 文件结构：8 字节头 + 8 字节文件大小 + 8 字节删除时间 + 4 字节路径长度，
        // 之后（偏移 28）才是原始路径（UTF-16LE，以 \0 结尾）
        const int PathOffset = 28;

        foreach (string metaFile in Directory.EnumerateFiles(binRoot, "$I*"))
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(metaFile);
            }
            catch (IOException)
            {
                continue; // 正被系统占用，跳过
            }

            if (bytes.Length <= PathOffset) continue;

            string recorded = Encoding.Unicode
                .GetString(bytes, PathOffset, bytes.Length - PathOffset)
                .TrimEnd('\0');

            if (string.Equals(
                    recorded.TrimEnd('\\'),
                    originalPath.TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

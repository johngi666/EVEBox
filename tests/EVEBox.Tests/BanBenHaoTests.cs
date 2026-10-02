using EVEBox.Common.GongYong;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 版本号比较：决定"要不要提示用户更新"。
/// 判错的后果是要么漏更新、要么把老版本推给用户，所以边界情况必须钉死。
/// </summary>
public class BanBenHaoTests
{
    [Theory]
    [InlineData("v6.16", "v6.15")]   // 常规升级
    [InlineData("v6.15.1", "v6.15")] // 多一段补丁号
    [InlineData("v7.0", "v6.99")]    // 主版本号优先，不能按字符串比大小
    [InlineData("v6.10", "v6.9")]    // 两位数段：v6.10 比 v6.9 新，字符串比较会判反
    [InlineData("V6.16", "v6.15")]   // 大小写 V
    [InlineData("6.16", "v6.15")]    // 远端不带 v
    [InlineData("v6.16", "6.15")]    // 本地不带 v
    [InlineData(" v6.16 ", "v6.15")] // 带空白
    [InlineData("v6.15.0.1", "v6.15.0")] // 多段递增
    public void NewerRemoteVersionIsDetected(string remote, string local)
    {
        Assert.True(BanBenHao.GengXin(remote, local),
            $"{remote} 应当比 {local} 新");
    }

    [Theory]
    [InlineData("v6.15", "v6.15")]     // 完全相同
    [InlineData("v6.15.0", "v6.15")]   // 缺失段按 0 处理，视为相同
    [InlineData("v6.15", "v6.15.0.0")] // 反过来也一样
    [InlineData("v6.14", "v6.15")]     // 远端更旧，绝不能提示更新
    [InlineData("v5.48", "v6.15")]     // 跨大版本回退
    [InlineData("v6.9", "v6.10")]      // 两位数段的回退
    public void SameOrOlderRemoteVersionIsNotNewer(string remote, string local)
    {
        Assert.False(BanBenHao.GengXin(remote, local),
            $"{remote} 不应当被判定为比 {local} 新");
    }

    [Theory]
    [InlineData(null, "v6.15")]
    [InlineData("", "v6.15")]
    [InlineData("   ", "v6.15")]
    [InlineData("v6.16", null)]
    [InlineData("v6.16", "")]
    [InlineData("abc", "v6.15")]   // 远端解析不出有效段
    [InlineData("v6.16", "xyz")]   // 本地解析不出有效段
    [InlineData("....", "v6.15")]  // 全是空段
    public void UnparsableVersionNeverTriggersUpdate(string remote, string local)
    {
        // 宁可漏一次提示，也不能把老版本推给用户
        Assert.False(BanBenHao.GengXin(remote, local));
    }

    [Theory]
    [InlineData("v6.x", "v6.1")]   // 段里混入非数字：整串视为无效版本号
    [InlineData("v6.1", "v6.x")]
    [InlineData("v6.15-rc1", "v6.14")] // 带后缀的预发布标记
    [InlineData("v6.15-beta", "v6.14")]
    public void VersionWithNonNumericSegmentIsTreatedAsInvalid(string remote, string local)
    {
        // 关键：本地版本号一旦异常，绝不能把任何远端版本都判成"有更新"
        Assert.False(BanBenHao.GengXin(remote, local));
    }
}

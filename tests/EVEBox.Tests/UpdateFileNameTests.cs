using EVEBox.App;
using EVEBox.Features.Update;
using System;
using System.IO;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 自动更新后的主程序文件名：
/// 安装名取包内主程序名（EVE BOX.exe），旧产品名的残留文件由启动自愈改名纠正。
/// </summary>
public class UpdateFileNameTests
{
    [Fact]
    public void JieXiAnZhuangMing_QuDiaoZanCunHouZhui()
    {
        Assert.Equal("EVE BOX.exe", UpdateDownloader.JieXiAnZhuangMing(@"C:\tools\EVE BOX.new.exe"));
    }

    [Fact]
    public void JieXiAnZhuangMing_BaoLiuYiYouMingZi()
    {
        Assert.Equal("EVE BOX.exe", UpdateDownloader.JieXiAnZhuangMing(@"C:\tools\EVE BOX.exe"));
    }

    [Fact]
    public void AnZhuangJiaoBen_ShanJiuExeBingGaiChengBaoNeiMingZi()
    {
        string script = UpdateDownloader.ShengChengAnZhuangJiaoBen(
            @"C:\tools\EVE配置管理工具.exe",
            @"C:\tools\EVE BOX.new.exe");

        Assert.Contains(@"del /f /q ""C:\tools\EVE配置管理工具.exe""", script);
        Assert.Contains(@"move /y ""C:\tools\EVE BOX.new.exe"" ""C:\tools\EVE BOX.exe""", script);
        Assert.Contains(@"start """" ""C:\tools\EVE BOX.exe""", script);
        Assert.DoesNotContain(@"move /y ""C:\tools\EVE BOX.new.exe"" ""C:\tools\EVE配置管理工具.exe""", script);
    }

    [Fact]
    public void XuYaoGaiMing_JiuChanPinMingXuYaoGai()
    {
        string muLu = XinJianLinShiMuLu();
        try
        {
            Assert.True(ExeRenameService.XuYaoGaiMing(
                Path.Combine(muLu, "EVE配置管理工具.exe"),
                Path.Combine(muLu, AppInfo.ExeFileName),
                Path.Combine(muLu, ExeRenameService.ShiBaiBiaoJiWenJian)));
        }
        finally
        {
            ShanChuLinShiMuLu(muLu);
        }
    }

    [Fact]
    public void XuYaoGaiMing_YiShiBiaoZhunMingZeBuDong()
    {
        string muLu = XinJianLinShiMuLu();
        try
        {
            string biaoZhun = Path.Combine(muLu, AppInfo.ExeFileName);
            Assert.False(ExeRenameService.XuYaoGaiMing(
                biaoZhun, biaoZhun, Path.Combine(muLu, ExeRenameService.ShiBaiBiaoJiWenJian)));
        }
        finally
        {
            ShanChuLinShiMuLu(muLu);
        }
    }

    [Fact]
    public void XuYaoGaiMing_BiaoZhunMingBeiZhanYongZeBuDong()
    {
        string muLu = XinJianLinShiMuLu();
        try
        {
            string biaoZhun = Path.Combine(muLu, AppInfo.ExeFileName);
            File.WriteAllText(biaoZhun, "已存在");

            Assert.False(ExeRenameService.XuYaoGaiMing(
                Path.Combine(muLu, "EVE配置管理工具.exe"),
                biaoZhun,
                Path.Combine(muLu, ExeRenameService.ShiBaiBiaoJiWenJian)));
        }
        finally
        {
            ShanChuLinShiMuLu(muLu);
        }
    }

    [Fact]
    public void XuYaoGaiMing_YongHuZiQiMingZiBuDong()
    {
        string muLu = XinJianLinShiMuLu();
        try
        {
            Assert.False(ExeRenameService.XuYaoGaiMing(
                Path.Combine(muLu, "我的EVE.exe"),
                Path.Combine(muLu, AppInfo.ExeFileName),
                Path.Combine(muLu, ExeRenameService.ShiBaiBiaoJiWenJian)));
        }
        finally
        {
            ShanChuLinShiMuLu(muLu);
        }
    }

    [Fact]
    public void XuYaoGaiMing_YouShiBaiBiaoJiZeBuChongShi()
    {
        string muLu = XinJianLinShiMuLu();
        try
        {
            string biaoJi = Path.Combine(muLu, ExeRenameService.ShiBaiBiaoJiWenJian);
            File.WriteAllText(biaoJi, "x");

            Assert.False(ExeRenameService.XuYaoGaiMing(
                Path.Combine(muLu, "EVE配置管理工具.exe"),
                Path.Combine(muLu, AppInfo.ExeFileName),
                biaoJi));
        }
        finally
        {
            ShanChuLinShiMuLu(muLu);
        }
    }

    [Fact]
    public void GaiMingJiaoBen_HanRenMingLingYuShiBaiBiaoJi()
    {
        string script = ExeRenameService.ShengChengGaiMingJiaoBen(
            @"C:\tools\EVE配置管理工具.exe",
            @"C:\tools\EVE BOX.exe",
            @"C:\tools\.evebox_rename_failed");

        Assert.Contains(@"ren ""C:\tools\EVE配置管理工具.exe"" ""EVE BOX.exe""", script);
        Assert.Contains(@"start """" ""C:\tools\EVE BOX.exe""", script);
        Assert.Contains(@":giveup", script);
        Assert.Contains(@"echo x > ""C:\tools\.evebox_rename_failed""", script);
    }

    [Fact]
    public void BiaoZhunMingYuChengXuJiMingChengBaoChiYiZhi()
    {
        // ExeFileName 必须跟 csproj 的 AssemblyName 一致，否则发布出来的 exe 名对不上
        Assert.Equal("EVE BOX", typeof(AppInfo).Assembly.GetName().Name);
        Assert.Equal("EVE BOX.exe", AppInfo.ExeFileName);
    }

    private static string XinJianLinShiMuLu()
    {
        string muLu = Path.Combine(Path.GetTempPath(), "evebox_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(muLu);
        return muLu;
    }

    private static void ShanChuLinShiMuLu(string muLu)
    {
        try { Directory.Delete(muLu, true); } catch { }
    }
}

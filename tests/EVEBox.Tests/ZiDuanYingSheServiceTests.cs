using EVEBox.Features.PeiZhiTongBu;
using System.Collections.Generic;
using Xunit;



namespace EVEBox.Tests;

public class ZiDuanYingSheServiceTests
{
    private static ZiDuanYingSheService CreateService() => new ZiDuanYingSheService();

    [Fact]
    public void IsPrivateChat_DetectsHalfWidthBracket()
    {
        var service = CreateService();
        Assert.True(service.IsPrivateChat("私聊(张三)"));
    }

    [Fact]
    public void IsPrivateChat_DetectsFullWidthBracket()
    {
        var service = CreateService();
        Assert.True(service.IsPrivateChat("私聊（张三）"));
    }

    [Fact]
    public void IsPrivateChat_RejectsNormalTitle()
    {
        var service = CreateService();
        Assert.False(service.IsPrivateChat("本地"));
    }

    [Fact]
    public void IsLocalChannel_DetectsLocal()
    {
        var service = CreateService();
        Assert.True(service.IsLocalChannel("本地"));
    }

    [Fact]
    public void IsGroupChat_DetectsGroup()
    {
        var service = CreateService();
        Assert.True(service.IsGroupChat("群聊(联盟频道)"));
    }

    [Fact]
    public void ExtractBaseName_RemovesNumberSuffix()
    {
        var service = CreateService();
        Assert.Equal("本地", service.ExtractBaseName("本地 [2]"));
    }

    [Fact]
    public void ExtractBaseName_NoSuffix_ReturnsOriginal()
    {
        var service = CreateService();
        Assert.Equal("本地", service.ExtractBaseName("本地"));
    }

    [Fact]
    public void ShouldOverrideWindowTitle_PrivateChat_NeverOverrides()
    {
        var service = CreateService();
        Assert.False(service.ShouldOverrideWindowTitle("k1", "私聊(张三)"));
    }

    [Fact]
    public void ShouldOverrideWindowTitle_LocalChannel_Overrides()
    {
        var service = CreateService();

        Assert.True(service.ShouldOverrideWindowTitle("k1", "本地"));
    }

    [Fact]
    public void ShouldOverrideWindowTitle_PublicChannel_Overrides()
    {
        var mapping = new YongHuZiDuanYingShe();
        mapping.BuildChatChannelMapping(new Dictionary<string, string> { { "1001", "联合势力" } });
        var service = CreateService();
        service.LoadUserMapping(mapping);

        Assert.True(service.ShouldOverrideWindowTitle("1001", "联合势力"));
    }

    [Fact]
    public void FilterWindowTitles_RemovesPrivateChat()
    {
        var source = new Dictionary<string, string>
        {
            { "k1", "本地" },
            { "k2", "私聊(张三)" },
            { "k3", "群聊(联盟频道)" }
        };
        var mapping = new Dictionary<string, string>
        {
            { "k1", "本地" },
            { "k3", "群聊(新名称)" }
        };
        var service = CreateService();

        var result = service.FilterWindowTitles(source, mapping);

        Assert.DoesNotContain(result, kvp => kvp.Value.Contains("私聊"));
        Assert.Equal("群聊(新名称)", result["k3"]);
    }

    [Fact]
    public void RefreshPublicChannelNames_LoadsFromMapping()
    {
        var mapping = new YongHuZiDuanYingShe();
        mapping.BuildChatChannelMapping(new Dictionary<string, string>
        {
            { "1001", "联合势力" },
            { "1002", "本地" }
        });
        var service = CreateService();
        service.LoadUserMapping(mapping);

        var names = service.GetPublicChannelNames();

        Assert.Contains("联合势力", names);
        Assert.Contains("本地", names);
    }

    [Fact]
    public void IsPublicChannel_RecognizesLoadedChannel()
    {
        var mapping = new YongHuZiDuanYingShe();
        mapping.BuildChatChannelMapping(new Dictionary<string, string> { { "1001", "联合势力" } });
        var service = CreateService();
        service.LoadUserMapping(mapping);

        Assert.True(service.IsPublicChannel("联合势力"));
        Assert.False(service.IsPublicChannel("不存在的频道"));
    }
}

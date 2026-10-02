using EVEBox.Features.ConfigSync;
using System.Collections.Generic;
using Xunit;



namespace EVEBox.Tests;

public class FieldMappingServiceTests
{
    private static FieldMappingService CreateService() => new FieldMappingService();

    [Fact]
    public void IsPrivateChat_DetectsHalfWidthBracket()
    {
        FieldMappingService service = CreateService();
        Assert.True(service.IsPrivateChat("私聊(张三)"));
    }

    [Fact]
    public void IsPrivateChat_DetectsFullWidthBracket()
    {
        FieldMappingService service = CreateService();
        Assert.True(service.IsPrivateChat("私聊（张三）"));
    }

    [Fact]
    public void IsPrivateChat_RejectsNormalTitle()
    {
        FieldMappingService service = CreateService();
        Assert.False(service.IsPrivateChat("本地"));
    }

    [Fact]
    public void IsLocalChannel_DetectsLocal()
    {
        FieldMappingService service = CreateService();
        Assert.True(service.IsLocalChannel("本地"));
    }

    [Fact]
    public void IsGroupChat_DetectsGroup()
    {
        FieldMappingService service = CreateService();
        Assert.True(service.IsGroupChat("群聊(联盟频道)"));
    }

    [Fact]
    public void ExtractBaseName_RemovesNumberSuffix()
    {
        FieldMappingService service = CreateService();
        Assert.Equal("本地", service.ExtractBaseName("本地 [2]"));
    }

    [Fact]
    public void ExtractBaseName_NoSuffix_ReturnsOriginal()
    {
        FieldMappingService service = CreateService();
        Assert.Equal("本地", service.ExtractBaseName("本地"));
    }

    [Fact]
    public void ShouldOverrideWindowTitle_PrivateChat_NeverOverrides()
    {
        FieldMappingService service = CreateService();
        Assert.False(service.ShouldOverrideWindowTitle("k1", "私聊(张三)"));
    }

    [Fact]
    public void ShouldOverrideWindowTitle_LocalChannel_Overrides()
    {
        FieldMappingService service = CreateService();

        Assert.True(service.ShouldOverrideWindowTitle("k1", "本地"));
    }

    [Fact]
    public void ShouldOverrideWindowTitle_PublicChannel_Overrides()
    {
        UserFieldMapping mapping = new UserFieldMapping();
        mapping.BuildChatChannelMapping(new Dictionary<string, string> { { "1001", "联合势力" } });
        FieldMappingService service = CreateService();
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
        FieldMappingService service = CreateService();

        var result = service.FilterWindowTitles(source, mapping);

        Assert.DoesNotContain(result, kvp => kvp.Value.Contains("私聊"));
        Assert.Equal("群聊(新名称)", result["k3"]);
    }

    [Fact]
    public void RefreshPublicChannelNames_LoadsFromMapping()
    {
        UserFieldMapping mapping = new UserFieldMapping();
        mapping.BuildChatChannelMapping(new Dictionary<string, string>
        {
            { "1001", "联合势力" },
            { "1002", "本地" }
        });
        FieldMappingService service = CreateService();
        service.LoadUserMapping(mapping);

        var names = service.GetPublicChannelNames();

        Assert.Contains("联合势力", names);
        Assert.Contains("本地", names);
    }

    [Fact]
    public void IsPublicChannel_RecognizesLoadedChannel()
    {
        UserFieldMapping mapping = new UserFieldMapping();
        mapping.BuildChatChannelMapping(new Dictionary<string, string> { { "1001", "联合势力" } });
        FieldMappingService service = CreateService();
        service.LoadUserMapping(mapping);

        Assert.True(service.IsPublicChannel("联合势力"));
        Assert.False(service.IsPublicChannel("不存在的频道"));
    }
}

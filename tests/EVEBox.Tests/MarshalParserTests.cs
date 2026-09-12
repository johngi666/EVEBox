using System.Text.Json;
using EVEBox.Core.Marshal;
using Xunit;

namespace EVEBox.Tests;

public class MarshalParserTests
{
    private static MarshalParser CreateParser() => new MarshalParser();

    [Fact]
    public void ExtractChatChannels_ObjectFormat_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:chatchannels": [ { "id": "1001", "name": "本地" } ] } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractChatChannels(doc);

        Assert.Single(result);
        Assert.Equal("本地", result["1001"]);
    }

    [Fact]
    public void ExtractChatChannels_FiltersPrivateChat()
    {
        var json = """
        { "ui": { "bytes:chatchannels": [ { "id": "1002", "name": "私聊(张三)" } ] } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractChatChannels(doc);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractChatChannels_ArrayFormat_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:chatchannels": [ [ "2001", "联合势力" ] ] } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractChatChannels(doc);

        Assert.Single(result);
        Assert.Equal("联合势力", result["2001"]);
    }

    [Fact]
    public void ExtractChatChannels_MissingSection_ReturnsEmpty()
    {
        var json = """{ "ui": {} }""";
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractChatChannels(doc);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractWindowTitles_ObjectFormat_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:tabgroups": { "k1": "本地" } } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractWindowTitles(doc);

        Assert.Single(result);
        Assert.Equal("本地", result["k1"]);
    }

    [Fact]
    public void ExtractWindowTitles_FiltersPrivateChat()
    {
        var json = """
        { "ui": { "bytes:tabgroups": { "k1": "私聊(李四)" } } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractWindowTitles(doc);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractOverviewTabs_ObjectFormat_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:tabsettings_new": { "tab1": "总览一" } } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractOverviewTabs(doc);

        Assert.Single(result);
        Assert.Equal("总览一", result["tab1"]);
    }

    [Fact]
    public void ExtractCustomCommands_ObjectFormat_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:customCmds": { "cmd1": "F1" } } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractCustomCommands(doc);

        Assert.Single(result);
        Assert.Equal("F1", result["cmd1"]);
    }

    [Fact]
    public void ExtractBookmarkFolders_PrefixMatch_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:bookmarkSubfolderWindow_1": "收藏夹一" } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractBookmarkFolders(doc);

        Assert.Single(result);
        Assert.Equal("收藏夹一", result["bytes:bookmarkSubfolderWindow_1"]);
    }

    [Fact]
    public void ExtractFittings_PrefixMatch_ReturnsMapping()
    {
        var json = """
        { "ui": { "bytes:Save_ViewFitting_1": "神装配置" } }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractFittings(doc);

        Assert.Single(result);
        Assert.Equal("神装配置", result["bytes:Save_ViewFitting_1"]);
    }

    [Fact]
    public void ExtractAll_ReturnsAllSixCategories()
    {
        var json = """
        {
          "ui": {
            "bytes:chatchannels": [ { "id": "1001", "name": "本地" } ],
            "bytes:tabgroups": { "k1": "本地" },
            "bytes:tabsettings_new": { "tab1": "总览一" },
            "bytes:customCmds": { "cmd1": "F1" },
            "bytes:bookmarkSubfolderWindow_1": "收藏夹一",
            "bytes:Save_ViewFitting_1": "神装配置"
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = CreateParser().ExtractAll(doc);

        Assert.Single(result.ChatChannels);
        Assert.Single(result.WindowTitles);
        Assert.Single(result.OverviewTabs);
        Assert.Single(result.CustomCommands);
        Assert.Single(result.BookmarkFolders);
        Assert.Single(result.Fittings);
    }
}

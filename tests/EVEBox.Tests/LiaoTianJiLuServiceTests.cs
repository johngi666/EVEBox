using EVEBox.OtherTools.LiaoTianJiLu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace EVEBox.Tests;

public class LiaoTianJiLuServiceTests : IDisposable
{
    private readonly string _root;

    public LiaoTianJiLuServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "evebox_liaotian_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    /// <summary>按国服客户端的写法造一个聊天日志：UTF-16LE + BOM，正文每条消息行前带一个 U+FEFF</summary>
    private string ChuangJian(string wenJianMing, string? listener, params string[] xiaoXi)
    {
        string luJing = Path.Combine(_root, wenJianMing + ".txt");

        var wenBen = new StringBuilder();
        wenBen.Append("\r\n\r\n");
        wenBen.Append("        ---------------------------------------------------------------\r\n");
        wenBen.Append("\r\n");
        wenBen.Append("          Channel ID:      player_test\r\n");
        wenBen.Append("          Channel Name:    测试频道\r\n");
        if (listener != null) wenBen.Append($"          Listener:        {listener}\r\n");
        wenBen.Append("          Session started: 2026.09.01 12:00:00\r\n");
        wenBen.Append("        ---------------------------------------------------------------\r\n");
        wenBen.Append("\r\n");
        foreach (string xiaoXiHang in xiaoXi)
            wenBen.Append("\uFEFF" + xiaoXiHang + "\r\n");

        File.WriteAllText(luJing, wenBen.ToString(), Encoding.Unicode);
        return luJing;
    }

    [Fact]
    public void SaoMiao_只按文件名建索引并按角色频道分类()
    {
        ChuangJian("本地_20260901_040005_111", "醉挽月", "[ 2026.09.01 12:00:05 ] 张三 > 你好");
        ChuangJian("本地_20260902_040005_111", "醉挽月", "[ 2026.09.02 12:00:05 ] 张三 > 又来");
        ChuangJian("军团_20260902_050000_222", "蕊珠闲", "[ 2026.09.02 13:00:00 ] 李四 > 在");
        ChuangJian("命名不符合规则", "某人", "[ 2026.09.03 12:00:00 ] 王五 > 忽略我");

        var jiaoSeLieBiao = new LiaoTianJiLuService(_root).SaoMiao();

        // 命名不匹配的文件不参与索引
        Assert.Equal(2, jiaoSeLieBiao.Count);

        var zuiWan = jiaoSeLieBiao.Single(x => x.Id == "111");
        Assert.Equal("醉挽月", zuiWan.MingCheng);         // 角色名读的是文件头 Listener
        Assert.Equal(2, zuiWan.WenJianShu);
        Assert.Single(zuiWan.PinDaoBiao);
        Assert.Equal(2, zuiWan.PinDaoBiao["本地"].Count);

        // 文件名是 UTC，BenDiShiJian 应当换算成本机时区；同时文件按时间升序
        DateTime yuQi = TimeZoneInfo.ConvertTimeFromUtc(
            new DateTime(2026, 9, 1, 4, 0, 5, DateTimeKind.Utc), TimeZoneInfo.Local);
        Assert.Equal(yuQi, zuiWan.PinDaoBiao["本地"][0].BenDiShiJian);
        Assert.Equal("本地_20260901_040005_111.txt", zuiWan.PinDaoBiao["本地"][0].WenJianMing);
    }

    [Fact]
    public void SaoMiao_读不到Listener就用角色ID顶上()
    {
        ChuangJian("本地_20260901_040005_333", null, "[ 2026.09.01 12:00:05 ] 张三 > 你好");

        var jiaoSe = new LiaoTianJiLuService(_root).SaoMiao().Single();

        Assert.Equal("333", jiaoSe.MingCheng);
    }

    [Fact]
    public void DuJiaoSeMing_读文件头的Listener字段()
    {
        string luJing = ChuangJian("本地_20260901_040005_111", "醉挽月", "[ 2026.09.01 12:00:05 ] 张三 > 你好");

        Assert.Equal("醉挽月", LiaoTianJiLuService.DuJiaoSeMing(luJing));
        Assert.Null(LiaoTianJiLuService.DuJiaoSeMing(Path.Combine(_root, "不存在.txt")));
    }

    [Fact]
    public void QuZhengWen_切掉文件头只留正文()
    {
        string luJing = ChuangJian("本地_20260901_040005_111", "醉挽月",
            "[ 2026.09.01 12:00:05 ] 张三 > 你好",
            "[ 2026.09.01 12:00:10 ] 李四 > 在的");

        byte[] zhengWen = LiaoTianJiLuService.QuZhengWen(luJing, out int tiaoShu);

        Assert.Equal(2, tiaoShu);

        string wenBen = Encoding.Unicode.GetString(zhengWen);
        Assert.DoesNotContain("Channel ID", wenBen);                 // 文件头被切掉
        Assert.StartsWith("\uFEFF[ 2026.09.01 12:00:05 ] 张三 > 你好", wenBen);
        Assert.Contains("李四 > 在的", wenBen);
    }

    [Fact]
    public void QuZhengWen_消息行没有零宽字符前缀也能切对()
    {
        string luJing = Path.Combine(_root, "本地_20260901_040005_444.txt");
        File.WriteAllText(luJing,
            "          Channel Name:    测试频道\r\n" +
            "[ 2026.09.01 12:00:05 ] 张三 > 你好\r\n",
            Encoding.Unicode);

        byte[] zhengWen = LiaoTianJiLuService.QuZhengWen(luJing, out int tiaoShu);

        Assert.Equal(1, tiaoShu);
        Assert.StartsWith("[ 2026.09.01 12:00:05 ] 张三 > 你好",
            Encoding.Unicode.GetString(zhengWen));
    }

    [Fact]
    public void GuoLu_按本地日期筛选并含首尾整天()
    {
        var quanBu = new List<LiaoTianWenJian>
        {
            new LiaoTianWenJian { BenDiShiJian = new DateTime(2026, 8, 31, 23, 0, 0) },
            new LiaoTianWenJian { BenDiShiJian = new DateTime(2026, 9, 1, 0, 5, 0) },
            new LiaoTianWenJian { BenDiShiJian = new DateTime(2026, 9, 2, 23, 59, 0) },
            new LiaoTianWenJian { BenDiShiJian = new DateTime(2026, 9, 3, 0, 0, 1) },
        };

        var jieGuo = LiaoTianJiLuService.GuoLu(quanBu, new DateTime(2026, 9, 1), new DateTime(2026, 9, 2));

        Assert.Equal(2, jieGuo.Count);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 5, 0), jieGuo[0].BenDiShiJian);
        Assert.Equal(new DateTime(2026, 9, 2, 23, 59, 0), jieGuo[1].BenDiShiJian);

        Assert.Empty(LiaoTianJiLuService.GuoLu(quanBu, new DateTime(2026, 10, 1), new DateTime(2026, 10, 2)));
        Assert.Empty(LiaoTianJiLuService.GuoLu(null, DateTime.Today, DateTime.Today));
    }

    [Fact]
    public void HuiZong_输出UTF16LE带表头且不含文件头()
    {
        ChuangJian("本地_20260901_040005_111", "醉挽月",
            "[ 2026.09.01 12:00:05 ] 张三 > 你好",
            "[ 2026.09.01 12:01:00 ] 李四 > 在的");

        var fuWu = new LiaoTianJiLuService(_root);
        var jiaoSe = fuWu.SaoMiao().Single();
        var wenJianBiao = jiaoSe.PinDaoBiao["本地"];

        string shuChuLuJing = Path.Combine(_root, "导出.txt");
        var jieGuo = fuWu.HuiZong(shuChuLuJing, jiaoSe, "本地",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 1), wenJianBiao);

        Assert.Equal(1, jieGuo.WenJianShu);
        Assert.Equal(2, jieGuo.TiaoShu);
        Assert.Equal(shuChuLuJing, jieGuo.ShuChuLuJing);

        byte[] ziJie = File.ReadAllBytes(shuChuLuJing);
        Assert.Equal(0xFF, ziJie[0]);       // UTF-16LE BOM，记事本才不会乱码
        Assert.Equal(0xFE, ziJie[1]);

        string wenBen = File.ReadAllText(shuChuLuJing, Encoding.Unicode);
        Assert.Contains("EVE BOX 聊天记录汇总", wenBen);
        Assert.Contains("醉挽月", wenBen);
        Assert.Contains("频道：本地", wenBen);
        Assert.Contains("张三 > 你好", wenBen);
        Assert.Contains("李四 > 在的", wenBen);
        Assert.DoesNotContain("Channel ID", wenBen);      // 文件头不重复出现
    }

    [Fact]
    public void HuiZong_输出文件已存在时抛错不覆盖()
    {
        ChuangJian("本地_20260901_040005_111", "醉挽月", "[ 2026.09.01 12:00:05 ] 张三 > 你好");
        var fuWu = new LiaoTianJiLuService(_root);
        var jiaoSe = fuWu.SaoMiao().Single();

        string shuChuLuJing = Path.Combine(_root, "已存在.txt");
        File.WriteAllText(shuChuLuJing, "原有内容");

        Assert.ThrowsAny<IOException>(() => fuWu.HuiZong(shuChuLuJing, jiaoSe, "本地",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 1), jiaoSe.PinDaoBiao["本地"]));

        Assert.Equal("原有内容", File.ReadAllText(shuChuLuJing));
    }

    [Fact]
    public void QuZhengWen_空会话不贡献内容()
    {
        // 进了频道没说话的会话：只有文件头，没有消息行
        string luJing = ChuangJian("奇趣蛋_20260824_050444_111", "醉挽月");

        byte[] zhengWen = LiaoTianJiLuService.QuZhengWen(luJing, out int tiaoShu);

        Assert.Empty(zhengWen);
        Assert.Equal(0, tiaoShu);
    }

    [Fact]
    public void HuiZong_跳过空会话()
    {
        ChuangJian("本地_20260901_040005_111", "醉挽月", "[ 2026.09.01 12:00:05 ] 张三 > 你好");
        ChuangJian("本地_20260902_040005_111", "醉挽月");      // 空会话

        var fuWu = new LiaoTianJiLuService(_root);
        var jiaoSe = fuWu.SaoMiao().Single();
        var wenJianBiao = jiaoSe.PinDaoBiao["本地"];

        string shuChuLuJing = Path.Combine(_root, "导出2.txt");
        var jieGuo = fuWu.HuiZong(shuChuLuJing, jiaoSe, "本地",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 2), wenJianBiao);

        Assert.Equal(2, wenJianBiao.Count);     // 匹配到 2 个文件
        Assert.Equal(1, jieGuo.WenJianShu);     // 实际合并进去的只有 1 个
        Assert.Equal(1, jieGuo.TiaoShu);
    }

    [Fact]
    public void AnQuanWenJianMing_去掉文件名非法字符()
    {
        Assert.Equal("醉挽月_20260901-20260902",
            LiaoTianJiLuService.AnQuanWenJianMing("醉挽月_20260901-20260902"));

        string jingHua = LiaoTianJiLuService.AnQuanWenJianMing("a:b*c?d\"e");
        Assert.DoesNotContain(":", jingHua);
        Assert.DoesNotContain("*", jingHua);
        Assert.DoesNotContain("?", jingHua);
        Assert.DoesNotContain("\"", jingHua);

        Assert.Equal("聊天记录", LiaoTianJiLuService.AnQuanWenJianMing(""));
        Assert.Equal("聊天记录", LiaoTianJiLuService.AnQuanWenJianMing(null));
    }
}

using EVEBox.OtherTools.SystemDistance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 拼音首字母检索（用户反馈：输入 jt 找不到「吉他」、sbsj 找不到「索巴色基」）。
///
/// 根因是 PinyinHelper 的边界表把 **GB2312 码位**当成 **Unicode 码位**比较，
/// 两套编码错位，99% 以上的汉字首字母都算错，于是拼音候选整体失效。
/// 现在统一走「汉字 → GB2312 顺序号 → 对照各字母起始序号」。
/// </summary>
public class PinyinHelperTests
{
    [Theory]
    [InlineData("吉", 'j')]
    [InlineData("他", 't')]
    [InlineData("索", 's')]
    [InlineData("巴", 'b')]
    [InlineData("色", 's')]
    [InlineData("基", 'j')]
    [InlineData("坦", 't')]
    [InlineData("欧", 'o')]
    [InlineData("米", 'm')]
    [InlineData("多", 'd')]
    [InlineData("伊", 'y')]
    [InlineData("阿", 'a')]
    [InlineData("佩", 'p')]
    [InlineData("苏", 's')]
    [InlineData("加", 'j')]
    [InlineData("沙", 's')]
    [InlineData("尔", 'e')]
    [InlineData("拉", 'l')]
    [InlineData("克", 'k')]
    [InlineData("实", 's')]
    [InlineData("验", 'y')]
    [InlineData("乱", 'l')]
    [InlineData("斗", 'd')]
    public void 单个汉字的拼音首字母正确(string hanZi, char qiDai)
    {
        Assert.Equal(qiDai, PinyinHelper.QuShouZiMu(hanZi[0]));
    }

    [Theory]
    [InlineData("吉他", "jt")]
    [InlineData("索巴色基", "sbsj")]
    [InlineData("坦欧", "to")]
    [InlineData("米玛塔", "mmt")]
    [InlineData("佩克", "pk")]
    [InlineData("多迪克", "ddk")]
    [InlineData("伊卡", "yk")]
    [InlineData("阿沙尔", "ase")]
    [InlineData("实验星域", "syxy")]
    [InlineData("乱斗星域", "ldxy")]
    public void 星系名的拼音首字母串正确(string xingXiMing, string qiDai)
    {
        Assert.Equal(qiDai, PinyinHelper.QuShouZiMu(xingXiMing));
    }

    [Fact]
    public void 非汉字原样保留为小写()
    {
        // 欧服/宁静服是英文名，拼音列不需要转换，但也不该被改坏
        Assert.Equal("jita", PinyinHelper.QuShouZiMu("Jita"));
        Assert.Equal("j123456", PinyinHelper.QuShouZiMu("J123456"));
    }

    [Fact]
    public void 空白字符被忽略()
    {
        Assert.Equal("jt", PinyinHelper.QuShouZiMu("吉 他"));
    }

    [Fact]
    public void 空串与null返回空串()
    {
        Assert.Equal(string.Empty, PinyinHelper.QuShouZiMu(""));
        Assert.Equal(string.Empty, PinyinHelper.QuShouZiMu((string?)null));
    }

    [Theory]
    [InlineData("伽克勒姆", "jklm")]
    [InlineData("伽哈", "jh")]
    [InlineData("忒什卡特", "tskt")]
    [InlineData("讷赫吉亚", "nhjy")]
    [InlineData("缪勒坦", "mlt")]
    [InlineData("黛", "d")]
    [InlineData("铎", "d")]
    [InlineData("滕", "t")]
    [InlineData("斐", "f")]
    [InlineData("迦", "j")]
    [InlineData("逖", "t")]
    [InlineData("珥", "e")]
    [InlineData("姗", "s")]
    [InlineData("娅", "y")]
    public void 一级字库之外的生僻字由补充表覆盖(string xingXiMing, string qiDai)
    {
        // 这 13 个字不在 GB2312 一级字库（3755 常用字）里，
        // 但曙光服星系名确实用到；早期兜底是原样返回，导致这些名字拼音检索失效
        Assert.Equal(qiDai, PinyinHelper.QuShouZiMu(xingXiMing));
    }

    [Fact]
    public void 不在GB2312里的生僻字有兜底不抛异常()
    {
        // 扩展区汉字（GB2312 收不下）与替换字符应原样返回，而不是抛异常或返回乱码
        foreach (char c in new[] { '\u9FA5', '\u3400', '\uFFFD' })
        {
            char r = PinyinHelper.QuShouZiMu(c);
            Assert.True(r == char.ToLowerInvariant(c) || (r >= 'a' && r <= 'z'));
        }
    }

    // ---------------------------------------------------------------
    // 搜索：用真实内置数据复现用户报的两个例子
    // ---------------------------------------------------------------

    private static async Task<IReadOnlyList<SolarSystem>> JiaZaiAsync(string fuWuQiMing)
    {
        using var http = new HttpClient();
        var yuan = new C3qSystemDataSource(SystemServer.AnMingCheng(fuWuQiMing), http);
        await yuan.LoadAsync(_ => { });
        return yuan.Systems;
    }

    [Theory]
    [InlineData("jt", "吉他")]
    [InlineData("sbsj", "索巴色基")]
    public async Task 拼音首字母能在真实数据里搜到中文星系(string shouZiMu, string qiDaiXingXi)
    {
        var systems = await JiaZaiAsync("曙光服");

        var jieGuo = SystemSearch.Search(systems, shouZiMu, 15);

        Assert.True(jieGuo.Count > 0, $"输入「{shouZiMu}」没有任何候选");
        Assert.Contains(jieGuo, s => s.Name == qiDaiXingXi);
    }

    [Fact]
    public async Task 拼音搜到的目标应排在候选前部()
    {
        var systems = await JiaZaiAsync("曙光服");

        var jieGuo = SystemSearch.Search(systems, "sbsj", 15);

        int wei = jieGuo.FindIndex(s => s.Name == "索巴色基");
        Assert.True(wei >= 0, "候选里没有「索巴色基」");
        Assert.True(wei < 5, $"「索巴色基」排在第 {wei + 1} 位，太靠后");
    }

    [Fact]
    public void 拼音前缀优先于拼音包含()
    {
        var systems = new List<SolarSystem>
        {
            new SolarSystem { Name = "甲", PinyinInitial = "xjia" },   // 包含
            new SolarSystem { Name = "乙", PinyinInitial = "jia" },    // 前缀
        };

        var jieGuo = SystemSearch.Search(systems, "jia");

        Assert.Equal("乙", jieGuo[0].Name);
    }

    [Fact]
    public void 中文名首字直接输入也能命中()
    {
        var systems = new List<SolarSystem>
        {
            new SolarSystem { Name = "吉他", PinyinInitial = "jt" },
        };

        var jieGuo = SystemSearch.Search(systems, "吉");

        Assert.Single(jieGuo);
        Assert.Equal("吉他", jieGuo[0].Name);
    }

    [Fact]
    public void 空输入与空列表返回空候选()
    {
        var systems = new List<SolarSystem> { new SolarSystem { Name = "吉他", PinyinInitial = "jt" } };

        Assert.Empty(SystemSearch.Search(systems, ""));
        Assert.Empty(SystemSearch.Search(systems, "   "));
        Assert.Empty(SystemSearch.Search(new List<SolarSystem>(), "jt"));
        Assert.Empty(SystemSearch.Search(null, "jt"));
    }

    [Fact]
    public void 大小写不敏感()
    {
        var systems = new List<SolarSystem> { new SolarSystem { Name = "吉他", PinyinInitial = "jt" } };

        Assert.Single(SystemSearch.Search(systems, "JT"));
        Assert.Single(SystemSearch.Search(systems, "Jt"));
    }
}

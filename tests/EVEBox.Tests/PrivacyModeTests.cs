using EVEBox.Common.Shared;
using System;
using Xunit;

namespace EVEBox.Tests;

/// <summary>
/// 隐私模式：截图/录屏时遮住可能暴露个人信息的内容。
/// 只影响显示，不动任何文件与配置数据。
/// </summary>
public class PrivacyModeTests : IDisposable
{
    public PrivacyModeTests() => PrivacyMode.SetEnabled(false);

    // 每个用例结束后恢复关闭，避免静态状态串到别的测试
    public void Dispose() => PrivacyMode.SetEnabled(false);

    [Fact]
    public void 关闭时ID原样返回()
    {
        PrivacyMode.SetEnabled(false);
        Assert.Equal("1234567890", PrivacyMode.MaskId("1234567890"));
    }

    [Fact]
    public void 开启时ID只保留前四位()
    {
        PrivacyMode.SetEnabled(true);
        Assert.Equal("1234******", PrivacyMode.MaskId("1234567890"));
    }

    [Fact]
    public void 开启时短ID全部遮蔽()
    {
        PrivacyMode.SetEnabled(true);
        Assert.Equal("****", PrivacyMode.MaskId("1234"));
        Assert.Equal("***", PrivacyMode.MaskId("123"));
    }

    [Fact]
    public void 开启时文本整段遮蔽且长度不变()
    {
        PrivacyMode.SetEnabled(true);
        Assert.Equal("****", PrivacyMode.MaskText("大号仓库"));
        Assert.Equal("******", PrivacyMode.MaskText("abcd中文"));
    }

    [Fact]
    public void 关闭时文本原样返回()
    {
        PrivacyMode.SetEnabled(false);
        Assert.Equal("大号仓库", PrivacyMode.MaskText("大号仓库"));
    }

    [Fact]
    public void 空白文本不会被填成星号()
    {
        PrivacyMode.SetEnabled(true);
        Assert.Null(PrivacyMode.MaskText(null));
        Assert.Equal("", PrivacyMode.MaskText(""));
    }

    [Fact]
    public void 空ID不会抛异常()
    {
        PrivacyMode.SetEnabled(true);
        Assert.Null(PrivacyMode.MaskId(null));
        Assert.Equal("", PrivacyMode.MaskId(""));
    }

    [Fact]
    public void 用户列有备注时遮蔽备注_无备注时遮蔽ID()
    {
        PrivacyMode.SetEnabled(true);
        Assert.Equal("****", PrivacyMode.MaskUserLabel("大号仓库", "1234567890"));
        Assert.Equal("1234******", PrivacyMode.MaskUserLabel(null, "1234567890"));
        Assert.Equal("1234******", PrivacyMode.MaskUserLabel("   ", "1234567890"));
    }

    [Fact]
    public void 关闭时用户列照旧显示()
    {
        PrivacyMode.SetEnabled(false);
        Assert.Equal("大号仓库", PrivacyMode.MaskUserLabel("大号仓库", "1234567890"));
        Assert.Equal("1234567890", PrivacyMode.MaskUserLabel(null, "1234567890"));
    }

    [Fact]
    public void 开关状态变化会触发事件()
    {
        PrivacyMode.SetEnabled(false);
        bool? shouDao = null;
        Action<bool> handler = v => shouDao = v;

        PrivacyMode.EnabledChanged += handler;
        try
        {
            PrivacyMode.SetEnabled(true);
            Assert.True(shouDao);
            Assert.True(PrivacyMode.Enabled);

            PrivacyMode.Toggle();
            Assert.False(shouDao);
            Assert.False(PrivacyMode.Enabled);
        }
        finally
        {
            PrivacyMode.EnabledChanged -= handler;
        }
    }

    [Fact]
    public void 设置相同状态不重复触发事件()
    {
        PrivacyMode.SetEnabled(false);
        int ciShu = 0;
        Action<bool> handler = _ => ciShu++;

        PrivacyMode.EnabledChanged += handler;
        try
        {
            PrivacyMode.SetEnabled(false);   // 与当前相同，不应触发
            Assert.Equal(0, ciShu);

            PrivacyMode.SetEnabled(true);
            Assert.Equal(1, ciShu);
        }
        finally
        {
            PrivacyMode.EnabledChanged -= handler;
        }
    }
}

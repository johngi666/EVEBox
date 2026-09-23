using System;
using System.Collections.Generic;
using System.Linq;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// 星系数据服务器描述：三个服的接口路径与文件格式完全一致，
    /// 只是域名/路径前缀不同，因此共用同一个数据源实现（见 C3qXingXiShuJuYuan）。
    /// </summary>
    public sealed class XingXiFuWuQi
    {
        /// <summary>服务器代号（sg / gf / tq，用于接口路径与缓存键）</summary>
        public string Key { get; }

        /// <summary>服务器中文名（界面显示、配置持久化都用它）</summary>
        public string Name { get; }

        /// <summary>数据文件根地址</summary>
        public string BaseUrl { get; }

        /// <summary>请求头 Referer（接口要求同源来源）</summary>
        public string Referer { get; }

        private XingXiFuWuQi(string key, string name, string baseUrl)
        {
            Key = key;
            Name = name;
            BaseUrl = baseUrl;
            Referer = $"https://eve.c3q.cc/jump/{key}/";
        }

        /// <summary>曙光服（网易重构的独立宇宙，坐标与另两服不同）</summary>
        public static readonly XingXiFuWuQi ShuGuang =
            new XingXiFuWuQi("sg", "曙光服", "https://cn.c3q.cc:20080/eve/market-sg/data/");

        /// <summary>晨曦服</summary>
        public static readonly XingXiFuWuQi ChenXi =
            new XingXiFuWuQi("gf", "晨曦服", "https://cn.c3q.cc:20080/eve/market/data/");

        /// <summary>宁静服</summary>
        public static readonly XingXiFuWuQi NingJing =
            new XingXiFuWuQi("tq", "宁静服", "https://eve.c3q.cc/market/data/");

        /// <summary>全部服务器，顺序即界面下拉顺序；默认取第一个（曙光服）</summary>
        public static readonly IReadOnlyList<XingXiFuWuQi> QuanBu = new[] { ShuGuang, ChenXi, NingJing };

        /// <summary>按下拉序号取服务器（越界时回落到默认服）</summary>
        public static XingXiFuWuQi AnSuoYin(int index)
        {
            return index >= 0 && index < QuanBu.Count ? QuanBu[index] : ShuGuang;
        }

        /// <summary>按中文名取服务器；名字为空或已失效时回落到默认服</summary>
        public static XingXiFuWuQi AnMingCheng(string name)
        {
            return QuanBu.FirstOrDefault(f => f.Name == name) ?? ShuGuang;
        }

        public override string ToString() => Name;
    }
}

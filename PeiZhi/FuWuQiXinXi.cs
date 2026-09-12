using EVEBox.WenJianJia;
using System.Collections.Generic;
using System.Linq;



namespace EVEBox.PeiZhi
{
    /// <summary>
    /// 服务器信息集中定义（名称、文件夹关键字、ESI 数据源、API 地址）
    /// 新增服务器只需在此添加一条记录
    /// </summary>
    public class FuWuQiXinXi
    {
        public string DisplayName { get; }   // 下拉框显示名，如 "曙光服 (Infinity)"
        public string StatusName { get; }    // 状态标签短名，如 "曙光服"
        public string Keyword { get; }       // 文件夹查找关键字，如 "infinity"
        public string DataSource { get; }    // ESI datasource 参数，如 "infinity"/"tranquility"
        public string EsiBaseUrl { get; }    // ESI API 根地址
        public string StatusUrl { get; }     // 服务器状态查询地址

        private FuWuQiXinXi(string displayName, string statusName, string keyword, string dataSource, string esiBaseUrl, string statusUrl)
        {
            DisplayName = displayName;
            StatusName = statusName;
            Keyword = keyword;
            DataSource = dataSource;
            EsiBaseUrl = esiBaseUrl;
            StatusUrl = statusUrl;
        }

        public static readonly FuWuQiXinXi Infinity = new(
            "曙光服 (Infinity)", "曙光服", "infinity", "infinity",
            "https://ali-esi.evepc.163.com",
            "https://ali-esi.evepc.163.com/latest/status/?datasource=infinity");

        public static readonly FuWuQiXinXi Serenity = new(
            "晨曦服 (Serenity)", "晨曦服", "serenity", "serenity",
            "https://ali-esi.evepc.163.com",
            "https://ali-esi.evepc.163.com/latest/status/?datasource=serenity");

        public static readonly FuWuQiXinXi Tranquility = new(
            "国际服 (Tranquility)", "国际服", "tranquility", "tranquility",
            "https://esi.evetech.net",
            "https://esi.evetech.net/latest/status/");

        public static readonly FuWuQiXinXi[] All = { Infinity, Serenity, Tranquility };

        /// <summary>
        /// 按显示名查找服务器（未知显示名回退到曙光服）
        /// </summary>
        public static FuWuQiXinXi GetByDisplayName(string displayName)
        {
            return All.FirstOrDefault(s => s.DisplayName == displayName) ?? Infinity;
        }

        /// <summary>
        /// 按数据源 key 查找服务器（infinity/serenity/tranquility，未知回退到曙光服）
        /// </summary>
        public static FuWuQiXinXi GetByDataSource(string dataSource)
        {
            return All.FirstOrDefault(s => s.DataSource == dataSource) ?? Infinity;
        }

        /// <summary>
        /// 显示名 → 文件夹关键字 映射（供 WenJianJiaFinder 使用）
        /// </summary>
        public static Dictionary<string, string> ToKeywordMap()
        {
            return All.ToDictionary(s => s.DisplayName, s => s.Keyword);
        }
    }
}

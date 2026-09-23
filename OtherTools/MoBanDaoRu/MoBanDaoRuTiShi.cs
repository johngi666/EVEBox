namespace EVEBox.OtherTools.MoBanDaoRu
{
    /// <summary>
    /// 模板导入类面板共用的提示文案（种菜 / 装配 / 总览 / 配置方案导入四处用同一句）
    /// </summary>
    public static class MoBanDaoRuTiShi
    {
        /// <summary>跨服务器导入内置模板的风险提示</summary>
        public const string KuaFuTiShi =
            "内置模板是以曙光服为标准，仅供参考。跨服务器导入模板可能引发异常错误，请谨慎操作！";

        /// <summary>提示文字的显示颜色（琥珀色，白底上对比度足够，且明显区别于正文灰）</summary>
        public static System.Drawing.Color TiShiYanSe => System.Drawing.Color.FromArgb(180, 83, 9);
    }
}

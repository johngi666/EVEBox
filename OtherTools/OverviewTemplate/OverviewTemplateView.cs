using System;
using System.IO;
using EVEBox.OtherTools.TemplateImport;

namespace EVEBox.OtherTools.OverviewTemplate
{
    /// <summary>
    /// 总览模板导入：把总览（Overview）模板送入 Documents\EVE\Overview。
    /// 游戏不会自动启用，导入后需在游戏内手动添加。
    /// 内置模板源码在本模块的 MoBan 目录下，编译时编进 exe，运行时释放到
    /// Documents\EVE\templates\OverviewTemplate。
    /// </summary>
    public class OverviewTemplateView : TemplateImportBaseView
    {
        /// <summary>
        /// 模块名：既用于本模块的内嵌资源前缀（csproj 的 LogicalName），
        /// 也用于 文档\EVE\templates 下的释放目录，两处必须一致。
        /// </summary>
        public const string MoKuaiMing = "OverviewTemplate";

        /// <summary>本模块内置模板所在的目录</summary>
        public static string NeiZhiMoBanMuLu => BuiltInTemplateResource.MoBanMuLu(MoKuaiMing);

        public OverviewTemplateView() : base(JianPeiZhi()) { }

        private static TemplateImportConfig JianPeiZhi()
        {
            string eveWenDang = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");

            return new TemplateImportConfig
            {
                BiaoTi = "总览模板导入",
                ShuoMing = "总览（YAML）模板导入到 Documents\\EVE\\Overview，"
                         + "游戏不会自动套用，导入后需在游戏内手动添加该总览。",
                MuBiaoWenJianJia = Path.Combine(eveWenDang, "Overview"),
                NeiZhiMoBanMuLu = NeiZhiMoBanMuLu,
                WenJianGuoLv = "*.yaml",
                NeiZhiWeiWenJianJia = false,
                WenJianMiaoShu = "YAML",
                DaoRuHouTiShi = "导入后请在游戏内手动添加该总览（总览设置 → 导入）。"
            };
        }
    }
}

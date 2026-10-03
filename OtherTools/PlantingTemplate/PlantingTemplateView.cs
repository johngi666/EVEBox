using System;
using System.IO;
using EVEBox.OtherTools.TemplateImport;

namespace EVEBox.OtherTools.PlantingTemplate
{
    /// <summary>
    /// 种菜模板导入：把行星开发模板送入 Documents\EVE\PlanetaryInteractionTemplates。
    /// 与其他两个不同，内置模板是「一整个文件夹」——一个模板文件夹里可以放多个 json，
    /// 导入时把文件夹内所有 json（含子目录）一起复制过去。
    /// 内置模板源码在本模块的 MoBan 目录下，编译时编进 exe，运行时释放到
    /// Documents\EVE\templates\PlantingTemplate。
    /// </summary>
    public class PlantingTemplateView : TemplateImportBaseView
    {
        /// <summary>
        /// 模块名：既用于本模块的内嵌资源前缀（csproj 的 LogicalName），
        /// 也用于 文档\EVE\templates 下的释放目录，两处必须一致。
        /// </summary>
        public const string MoKuaiMing = "PlantingTemplate";

        /// <summary>本模块内置模板所在的目录</summary>
        public static string NeiZhiMoBanMuLu => BuiltInTemplateResource.MoBanMuLu(MoKuaiMing);

        public PlantingTemplateView() : base(JianPeiZhi()) { }

        private static TemplateImportConfig JianPeiZhi()
        {
            string eveWenDang = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");

            return new TemplateImportConfig
            {
                BiaoTi = "种菜模板导入",
                ShuoMing = "行星开发（种菜）模板导入到 Documents\\EVE\\PlanetaryInteractionTemplates。"
                         + "内置模板为文件夹形式：一个文件夹内可放多个 json，导入时整包复制。",
                MuBiaoWenJianJia = Path.Combine(eveWenDang, "PlanetaryInteractionTemplates"),
                NeiZhiMoBanMuLu = NeiZhiMoBanMuLu,
                WenJianGuoLv = "*.json",
                NeiZhiWeiWenJianJia = true,
                WenJianMiaoShu = "JSON",
                DaoRuHouTiShi = "导入后请在游戏内打开行星开发界面刷新。"
            };
        }
    }
}

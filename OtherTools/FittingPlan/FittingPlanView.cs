using System;
using System.IO;
using EVEBox.OtherTools.TemplateImport;

namespace EVEBox.OtherTools.FittingPlan
{
    /// <summary>
    /// 装配方案导入：把装配（fittings）方案送入 Documents\EVE\fittings。
    /// 内置模板源码在本模块的 MoBan 目录下，编译时编进 exe，运行时释放到
    /// Documents\EVE\templates\FittingPlan。
    /// </summary>
    public class FittingPlanView : TemplateImportBaseView
    {
        /// <summary>
        /// 模块名：既用于本模块的内嵌资源前缀（csproj 的 LogicalName），
        /// 也用于 文档\EVE\templates 下的释放目录，两处必须一致。
        /// 英文化时这里漏改成旧名 ZhuangPeiFangAn，导致内置模板加载不出来。
        /// </summary>
        public const string MoKuaiMing = "FittingPlan";

        /// <summary>本模块内置模板所在的目录</summary>
        public static string NeiZhiMoBanMuLu => BuiltInTemplateResource.MoBanMuLu(MoKuaiMing);

        public FittingPlanView() : base(JianPeiZhi()) { }

        private static TemplateImportConfig JianPeiZhi()
        {
            string eveWenDang = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");

            return new TemplateImportConfig
            {
                BiaoTi = "装配方案导入",
                ShuoMing = "装配方案（XML）导入到 Documents\\EVE\\fittings，"
                         + "导入后在游戏内装配窗口即可选用。",
                MuBiaoWenJianJia = Path.Combine(eveWenDang, "fittings"),
                NeiZhiMoBanMuLu = NeiZhiMoBanMuLu,
                WenJianGuoLv = "*.xml",
                NeiZhiWeiWenJianJia = false,
                WenJianMiaoShu = "XML",
                DaoRuHouTiShi = "导入后请在游戏内打开装配窗口刷新。"
            };
        }
    }
}

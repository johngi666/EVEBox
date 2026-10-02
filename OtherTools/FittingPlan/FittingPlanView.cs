using System;
using System.IO;
using EVEBox.OtherTools.TemplateImport;

namespace EVEBox.OtherTools.FittingPlan
{
    /// <summary>
    /// 装配方案导入：把装配（fittings）方案送入 Documents\EVE\fittings。
    /// 内置模板源码在本模块的 MoBan 目录下，编译时编进 exe，运行时释放到
    /// Documents\EVE\templates\ZhuangPeiFangAn。
    /// </summary>
    public class FittingPlanView : TemplateImportBaseView
    {
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
                NeiZhiMoBanMuLu = BuiltInTemplateResource.MoBanMuLu("ZhuangPeiFangAn"),
                WenJianGuoLv = "*.xml",
                NeiZhiWeiWenJianJia = false,
                WenJianMiaoShu = "XML",
                DaoRuHouTiShi = "导入后请在游戏内打开装配窗口刷新。"
            };
        }
    }
}

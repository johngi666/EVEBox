using System;
using System.IO;
using EVEBox.OtherTools.MoBanDaoRu;

namespace EVEBox.OtherTools.ZhuangPeiFangAn
{
    /// <summary>
    /// 装配方案导入：把装配（fittings）方案送入 Documents\EVE\fittings。
    /// 内置模板放在本模块的 MoBan 目录下，编译时复制到输出目录。
    /// </summary>
    public class ZhuangPeiFangAnView : MoBanDaoRuBaseView
    {
        public ZhuangPeiFangAnView() : base(JianPeiZhi()) { }

        private static MoBanDaoRuPeiZhi JianPeiZhi()
        {
            string eveWenDang = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");
            string neiZhiGen = Path.Combine(AppContext.BaseDirectory, "templates");

            return new MoBanDaoRuPeiZhi
            {
                BiaoTi = "装配方案导入",
                ShuoMing = "装配方案（XML）导入到 Documents\\EVE\\fittings，"
                         + "导入后在游戏内装配窗口即可选用。",
                MuBiaoWenJianJia = Path.Combine(eveWenDang, "fittings"),
                NeiZhiMoBanMuLu = Path.Combine(neiZhiGen, "ZhuangPeiFangAn"),
                WenJianGuoLv = "*.xml",
                NeiZhiWeiWenJianJia = false,
                WenJianMiaoShu = "XML",
                DaoRuHouTiShi = "导入后请在游戏内打开装配窗口刷新。"
            };
        }
    }
}

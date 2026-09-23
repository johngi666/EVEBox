using System;
using System.IO;
using EVEBox.OtherTools.MoBanDaoRu;

namespace EVEBox.OtherTools.ZongLanMoBan
{
    /// <summary>
    /// 总览模板导入：把总览（Overview）模板送入 Documents\EVE\Overview。
    /// 游戏不会自动启用，导入后需在游戏内手动添加。
    /// 内置模板放在本模块的 MoBan 目录下，编译时复制到输出目录。
    /// </summary>
    public class ZongLanMoBanView : MoBanDaoRuBaseView
    {
        public ZongLanMoBanView() : base(JianPeiZhi()) { }

        private static MoBanDaoRuPeiZhi JianPeiZhi()
        {
            string eveWenDang = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");
            string neiZhiGen = Path.Combine(AppContext.BaseDirectory, "templates");

            return new MoBanDaoRuPeiZhi
            {
                BiaoTi = "总览模板导入",
                ShuoMing = "总览（YAML）模板导入到 Documents\\EVE\\Overview，"
                         + "游戏不会自动套用，导入后需在游戏内手动添加该总览。",
                MuBiaoWenJianJia = Path.Combine(eveWenDang, "Overview"),
                NeiZhiMoBanMuLu = Path.Combine(neiZhiGen, "ZongLanMoBan"),
                WenJianGuoLv = "*.yaml",
                NeiZhiWeiWenJianJia = false,
                WenJianMiaoShu = "YAML",
                DaoRuHouTiShi = "导入后请在游戏内手动添加该总览（总览设置 → 导入）。"
            };
        }
    }
}

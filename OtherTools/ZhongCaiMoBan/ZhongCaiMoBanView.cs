using System;
using System.IO;
using EVEBox.OtherTools.MoBanDaoRu;

namespace EVEBox.OtherTools.ZhongCaiMoBan
{
    /// <summary>
    /// 种菜模板导入：把行星开发模板送入 Documents\EVE\PlanetaryInteractionTemplates。
    /// 与其他两个不同，内置模板是「一整个文件夹」——一个模板文件夹里可以放多个 json，
    /// 导入时把文件夹内所有 json（含子目录）一起复制过去。
    /// 内置模板放在本模块的 MoBan 目录下，编译时复制到输出目录。
    /// </summary>
    public class ZhongCaiMoBanView : MoBanDaoRuBaseView
    {
        public ZhongCaiMoBanView() : base(JianPeiZhi()) { }

        private static MoBanDaoRuPeiZhi JianPeiZhi()
        {
            string eveWenDang = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");
            string neiZhiGen = Path.Combine(AppContext.BaseDirectory, "templates");

            return new MoBanDaoRuPeiZhi
            {
                BiaoTi = "种菜模板导入",
                ShuoMing = "行星开发（种菜）模板导入到 Documents\\EVE\\PlanetaryInteractionTemplates。"
                         + "内置模板为文件夹形式：一个文件夹内可放多个 json，导入时整包复制。",
                MuBiaoWenJianJia = Path.Combine(eveWenDang, "PlanetaryInteractionTemplates"),
                NeiZhiMoBanMuLu = Path.Combine(neiZhiGen, "ZhongCaiMoBan"),
                WenJianGuoLv = "*.json",
                NeiZhiWeiWenJianJia = true,
                WenJianMiaoShu = "JSON",
                DaoRuHouTiShi = "导入后请在游戏内打开行星开发界面刷新。"
            };
        }
    }
}

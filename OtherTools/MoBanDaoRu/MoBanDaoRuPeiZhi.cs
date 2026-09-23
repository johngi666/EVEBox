namespace EVEBox.OtherTools.MoBanDaoRu
{
    /// <summary>
    /// 模板导入面板配置：种菜模板、装配方案、总览模板各自的目标目录、内置模板目录、
    /// 文件类型都不同，由具体功能模块构造本对象传入基类，避免把差异硬编码在公共界面里。
    /// </summary>
    public class MoBanDaoRuPeiZhi
    {
        /// <summary>面板标题</summary>
        public string BiaoTi { get; set; } = "";

        /// <summary>功能说明（面板顶部，说明导入到哪、导入后做什么）</summary>
        public string ShuoMing { get; set; } = "";

        /// <summary>游戏内目标文件夹，如 Documents\EVE\fittings</summary>
        public string MuBiaoWenJianJia { get; set; } = "";

        /// <summary>
        /// 内置模板目录（运行时路径）。源码里每个功能模块自己有一个 MoBan 文件夹，
        /// 编译时复制到输出目录的 templates\&lt;模块&gt; 下，这里指向的就是输出目录里的那一份。
        /// </summary>
        public string NeiZhiMoBanMuLu { get; set; } = "";

        /// <summary>模板文件筛选，如 *.json / *.xml / *.yaml</summary>
        public string WenJianGuoLv { get; set; } = "*.*";

        /// <summary>
        /// 内置模板是否为文件夹：种菜模板是一整个文件夹，
        /// 导入时把文件夹内所有模板文件（含子目录）复制到目标文件夹。
        /// </summary>
        public bool NeiZhiWeiWenJianJia { get; set; }

        /// <summary>文件类型描述，用于文件选择对话框，如 JSON / XML / YAML</summary>
        public string WenJianMiaoShu { get; set; } = "模板";

        /// <summary>导入成功后的后续操作提示</summary>
        public string DaoRuHouTiShi { get; set; } = "";
    }
}

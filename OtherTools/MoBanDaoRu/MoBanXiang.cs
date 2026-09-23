namespace EVEBox.OtherTools.MoBanDaoRu
{
    /// <summary>
    /// 内置模板项：可以是一个模板文件（装配方案、总览模板），
    /// 也可以是一整个模板文件夹（种菜模板，导入时把文件夹里的模板全部复制过去）。
    /// </summary>
    public class MoBanXiang
    {
        /// <summary>显示名称（文件名或文件夹名）</summary>
        public string MingCheng { get; set; } = "";

        /// <summary>完整路径</summary>
        public string LuJing { get; set; } = "";

        /// <summary>是否为模板文件夹</summary>
        public bool ShiWenJianJia { get; set; }

        public override string ToString() => ShiWenJianJia ? MingCheng + "（文件夹）" : MingCheng;
    }
}

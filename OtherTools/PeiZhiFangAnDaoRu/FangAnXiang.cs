namespace EVEBox.OtherTools.PeiZhiFangAnDaoRu
{
    /// <summary>
    /// 配置方案项：一个方案文件夹 = 一套配置，里面放 core_char_*.dat / core_user_*.dat。
    /// </summary>
    public class FangAnXiang
    {
        /// <summary>方案名（文件夹名）</summary>
        public string MingCheng { get; set; } = "";

        /// <summary>方案文件夹完整路径</summary>
        public string LuJing { get; set; } = "";

        /// <summary>内容摘要，如「角色文件 2 个 / 用户文件 1 个」；为空串表示不是有效方案</summary>
        public string ZhaiYao { get; set; } = "";

        public override string ToString()
            => string.IsNullOrEmpty(ZhaiYao) ? MingCheng + "（非配置方案）" : MingCheng + "（" + ZhaiYao + "）";
    }
}


namespace EVEBox.App
{
    /// <summary>
    /// 应用版本信息和更新配置
    /// </summary>
    public static class YingYongXinXi
    {
        /// <summary>
        /// 当前版本号（发布时修改此处即可）
        /// </summary>
        public const string Version = "v6.11";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 项目更名为 EVE BOX，界面与目录结构重构\n" +
            "   - 其他工具新增：查看聊天记录、配置方案导入、种菜/装配/总览模板导入、星系间距查询\n" +
            "   - 更换程序图标，清理无用设置与代码";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年9月23日";

        /// <summary>
        /// 项目主页（Gitee）
        /// </summary>
        public const string GiteeUrl = "https://gitee.com/minisangel/EVEBox";

        /// <summary>
        /// 项目主页（GitHub）
        /// </summary>
        public const string GithubUrl = "https://github.com/johngi666/EVEBox";

        /// <summary>
        /// 远端版本检查地址列表（按顺序尝试，哪个能访问用哪个）
        /// 前三组是仓库改名后的正式地址；每组末尾那条旧名 EVESyncTool 是过渡备用，
        /// 等线上仓库确认改名完成后可以删掉。
        /// </summary>
        public static readonly string[] UpdateCheckUrls =
        {
            // 1. gitee.com（国内最稳定，主源）
            "https://gitee.com/minisangel/EVEBox/raw/main/version.json",
            "https://gitee.com/minisangel/EVESyncTool/raw/main/version.json",
            // 2. cdn.jsdelivr.net（GitHub 的 CDN 镜像，国内通常可达）
            "https://cdn.jsdelivr.net/gh/johngi666/EVEBox@main/version.json",
            "https://cdn.jsdelivr.net/gh/johngi666/EVESyncTool@main/version.json",
            // 3. GitHub 原始文件与主站路径（国内不稳定）
            "https://raw.githubusercontent.com/johngi666/EVEBox/main/version.json",
            "https://github.com/johngi666/EVEBox/raw/main/version.json"
        };

        /// <summary>
        /// 发布页面 URL
        /// </summary>
        public const string ReleasesUrl = GithubUrl + "/releases/latest";
    }
}

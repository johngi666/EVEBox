
namespace EVEBox.App
{
    /// <summary>
    /// 应用版本信息和更新配置
    /// </summary>
    public static class AppInfo
    {
        /// <summary>
        /// 当前版本号（发布时修改此处即可）
        /// </summary>
        public const string Version = "v6.16";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 修复星系间距查询的拼音首字母检索：此前输入 jt、sbsj 等首字母匹配不到中文星系名，现已可正常检索\n" +
            "   - 修复「装配方案导入」「配置方案导入」的内置模板加载不出来（模板列表空白）\n" +
            "   - 启动流程优化：去掉重复执行的操作，操作日志不再记录每次开机的过程噪音\n" +
            "   - 内置模板、星系数据在升级后会自动整理，不再残留旧目录";

        /// <summary>
        /// 主程序标准文件名（与 csproj 的 AssemblyName 保持一致）
        /// </summary>
        public const string ExeFileName = "EVE BOX.exe";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年10月7日";

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
        /// 每组末尾那条旧名 EVESyncTool 是仓库改名期间留的备用，确认稳定后可删。
        /// </summary>
        public static readonly string[] UpdateCheckUrls =
        {
            // 1. gitee.com（国内最稳定，主源）
            "https://gitee.com/minisangel/EVEBox/raw/main/version.json",
            "https://gitee.com/minisangel/EVESyncTool/raw/main/version.json",
            // 2. cdn.jsdelivr.net（GitHub 的 CDN 镜像，国内通常可达）
            "https://cdn.jsdelivr.net/gh/johngi666/EVEBox@main/version.json",
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

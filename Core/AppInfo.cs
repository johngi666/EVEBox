namespace EVESyncTool.Core
{
    /// <summary>
    /// 应用版本信息和更新配置
    /// </summary>
    public static class AppInfo
    {
        /// <summary>
        /// 当前版本号（发布时修改此处即可）
        /// </summary>
        public const string Version = "v5.52";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 修复备份管理计数不更新（备份/删除后立即显示真实数量）\n" +
            "   - 备份还原后自动刷新文件列表（用户/角色表格立即反映还原结果）";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年8月17日";

        /// <summary>
        /// 远端版本检查地址列表（按顺序尝试，哪个能访问用哪个）
        /// 1. gitee.com（国内最稳定，主源）
        /// 2. cdn.jsdelivr.net（GitHub 的 CDN 镜像，国内通常可达）
        /// 3. raw.githubusercontent.com（GitHub 原始文件，国内不稳定）
        /// 4. github.com/raw（GitHub 主站路径，有时可用）
        /// </summary>
        public static readonly string[] UpdateCheckUrls =
        {
            "https://gitee.com/minisangel/EVESyncTool/raw/main/version.json",
            "https://cdn.jsdelivr.net/gh/johngi666/EVESyncTool@main/version.json",
            "https://raw.githubusercontent.com/johngi666/EVESyncTool/main/version.json",
            "https://github.com/johngi666/EVESyncTool/raw/main/version.json"
        };

        /// <summary>
        /// 发布页面 URL
        /// </summary>
        public const string ReleasesUrl =
            "https://github.com/johngi666/EVESyncTool/releases/latest";
    }
}

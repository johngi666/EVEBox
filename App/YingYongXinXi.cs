

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
        public const string Version = "v6.00";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - UI 界面大更新，代码重构";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年8月27日";

        /// <summary>
        /// 远端版本检查地址列表（按顺序尝试，哪个能访问用哪个）
        /// 1. gitee.com（国内最稳定，主源）
        /// 2. cdn.jsdelivr.net（GitHub 的 CDN 镜像，国内通常可达）
        /// 3. raw.githubusercontent.com（GitHub 原始文件，国内不稳定）
        /// 4. github.com/raw（GitHub 主站路径，有时可用）
        /// </summary>
        public static readonly string[] UpdateCheckUrls =
        {
            "https://gitee.com/minisangel/EVEBox/raw/main/version.json",
            "https://cdn.jsdelivr.net/gh/johngi666/EVEBox@main/version.json",
            "https://raw.githubusercontent.com/johngi666/EVEBox/main/version.json",
            "https://github.com/johngi666/EVEBox/raw/main/version.json"
        };

        /// <summary>
        /// 发布页面 URL
        /// </summary>
        public const string ReleasesUrl =
            "https://github.com/johngi666/EVEBox/releases/latest";
    }
}

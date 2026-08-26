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
        public const string Version = "v5.54";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 新增全局舰船标签显示开关（prefs.ini 一键切换，切换文件夹/服务器自动检测状态）\n" +
            "   - 新增 EVE 客户端运行检测：同步/切换方案/还原等修改文件操作前，检测到客户端时\n" +
            "     弹窗可一键关闭所有客户端进程（选择\"否\"则取消当前操作，确认关闭后延迟1秒执行）\n" +
            "   - 左侧界面优化（服务器状态区紧凑布局）";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年8月26日";

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

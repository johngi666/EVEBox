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
        public const string Version = "v5.49";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 标题栏按钮位置调整（右移100px），最小宽度1050防重叠\n" +
            "   - 操作日志不再记录服务器在线人数（避免刷屏）\n" +
            "   - 修复加载配置文件时日志重复记录\n" +
            "   - 角色名查询批次大小 10→20（查询间隔不变）\n" +
            "   - 版本检查不再写入日志，每分钟最多检测10次后停止\n" +
            "   - 项目添加 MIT 开源许可证";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年8月8日";

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

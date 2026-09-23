using System;
using System.IO;

namespace EVEBox.OtherTools.LiaoTianJiLu
{
    /// <summary>
    /// 一个聊天日志文件（= 一次登录里某个频道的一段会话）。
    /// 频道名 / 角色 ID / 会话时间全部来自文件名，扫描阶段不打开文件。
    /// </summary>
    public class LiaoTianWenJian
    {
        /// <summary>完整路径</summary>
        public string LuJing { get; set; } = "";

        /// <summary>频道名（文件名第一段，如 本地 / 军团 / 722）</summary>
        public string PinDao { get; set; } = "";

        /// <summary>监听者角色 ID（文件名最后一段）</summary>
        public string JiaoSeId { get; set; } = "";

        /// <summary>会话开始时间：文件名里写的是 UTC</summary>
        public DateTime UtcShiJian { get; set; }

        /// <summary>换算成本机时区后的会话开始时间（文件头和正文里的时间也是本机时间）</summary>
        public DateTime BenDiShiJian { get; set; }

        /// <summary>文件大小（字节）</summary>
        public long DaXiao { get; set; }

        public string WenJianMing => Path.GetFileName(LuJing);

        /// <summary>列表显示：文件名 + 大小</summary>
        public string XianShi => $"{WenJianMing}　　{DaXiao / 1024.0:0.#} KB";
    }
}

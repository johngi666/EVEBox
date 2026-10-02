using System.Collections.Generic;
using System.Linq;

namespace EVEBox.OtherTools.ChatLog
{
    /// <summary>
    /// 一个监听角色：先按角色名分类，角色下面再按频道名分类，每个频道挂它的一批日志文件。
    /// </summary>
    public class ChatCharacter
    {
        /// <summary>角色 ID（文件名最后一段）</summary>
        public string Id { get; set; } = "";

        /// <summary>角色名（读文件头的 Listener 字段得到；读不到时先用 ID 顶上）</summary>
        public string MingCheng { get; set; } = "";

        /// <summary>频道名 → 该频道的日志文件（按时间升序）</summary>
        public Dictionary<string, List<ChatFile>> PinDaoBiao { get; } =
            new Dictionary<string, List<ChatFile>>();

        /// <summary>该角色的日志文件总数</summary>
        public int WenJianShu => PinDaoBiao.Values.Sum(x => x.Count);

        public IEnumerable<ChatFile> SuoYouWenJian() => PinDaoBiao.Values.SelectMany(x => x);

        public override string ToString() => $"{MingCheng}（{Id}）";
    }
}

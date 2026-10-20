using System;

namespace EVEBox.Common.Shared
{
    /// <summary>
    /// 隐私模式：对外展示的截图/录屏场景下，遮住可能暴露个人信息的内容。
    ///
    /// 开启后：
    ///   - 隐藏「配置同步」整个界面
    ///   - 用户ID / 角色ID 只保留前 4 位，其余用 * 代替
    ///   - 用户备注、角色名整段用 * 代替
    ///
    /// 只影响**显示**，不改动任何文件、配置或实际数据。
    /// </summary>
    public static class PrivacyMode
    {
        /// <summary>ID 保留的前缀位数</summary>
        private const int IdKeJianWeiShu = 4;

        /// <summary>当前是否开启</summary>
        public static bool Enabled { get; private set; }

        /// <summary>开关状态变化时触发（参数为新状态）</summary>
        public static event Action<bool> EnabledChanged;

        public static void SetEnabled(bool enabled)
        {
            if (Enabled == enabled) return;
            Enabled = enabled;
            EnabledChanged?.Invoke(enabled);
        }

        public static void Toggle() => SetEnabled(!Enabled);

        /// <summary>
        /// 按当前开关状态返回应显示的 ID：开启时只留前 4 位（如 1234******），关闭时原样。
        /// </summary>
        public static string MaskId(string id)
        {
            if (!Enabled || string.IsNullOrEmpty(id)) return id;
            if (id.Length <= IdKeJianWeiShu) return new string('*', id.Length);
            return id.Substring(0, IdKeJianWeiShu) + new string('*', id.Length - IdKeJianWeiShu);
        }

        /// <summary>
        /// 按当前开关状态返回应显示的文本（备注、角色名等）：开启时整段替换为等长 *。
        /// 空白文本原样返回，避免出现一串没意义的星号。
        /// </summary>
        public static string MaskText(string text)
        {
            if (!Enabled || string.IsNullOrEmpty(text)) return text;
            return new string('*', text.Length);
        }

        /// <summary>
        /// 用户列的组合显示：有关键备注显示备注，否则显示 ID；两种都按隐私模式处理。
        /// </summary>
        public static string MaskUserLabel(string remark, string userId)
        {
            if (!Enabled) return string.IsNullOrWhiteSpace(remark) ? userId : remark;
            return string.IsNullOrWhiteSpace(remark) ? MaskId(userId) : MaskText(remark);
        }
    }
}

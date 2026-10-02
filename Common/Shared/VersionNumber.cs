using System;

namespace EVEBox.Common.Shared
{
    /// <summary>
    /// 版本号比较。
    ///
    /// 从 GengXinService 抽出来单独放，是因为这段逻辑决定了"要不要提示用户更新"，
    /// 判断错了要么漏更新、要么把老版本推给用户，属于高风险路径。
    /// 抽成纯函数后可以直接写单元测试覆盖边界情况。
    /// </summary>
    public static class VersionNumber
    {
        /// <summary>
        /// 远端版本是否比本地版本更新。
        /// 形如 "v6.15" / "V6.15" / "6.15"；段数不同按缺失段记 0 处理（v6.15 == v6.15.0）。
        /// 任何一边解析不出有效版本号时一律返回 false——宁可漏一次提示，也不能把旧版本推给用户。
        /// </summary>
        /// <param name="remote">远端版本号</param>
        /// <param name="local">本地版本号</param>
        public static bool GengXin(string remote, string local)
        {
            if (string.IsNullOrWhiteSpace(remote) || string.IsNullOrWhiteSpace(local))
                return false;

            if (!TryParse(remote, out int[] remoteParts) ||
                !TryParse(local, out int[] localParts))
                return false;

            int length = Math.Max(remoteParts.Length, localParts.Length);
            for (int i = 0; i < length; i++)
            {
                int r = i < remoteParts.Length ? remoteParts[i] : 0;
                int l = i < localParts.Length ? localParts[i] : 0;
                if (r != l) return r > l;
            }

            return false; // 完全相同
        }

        /// <summary>
        /// 严格解析 "v6.15" 这类版本号为整数数组。
        /// 只要有任意一段不是纯数字（或整个串没有有效段），就判定为「不是合法版本号」并返回 false。
        /// 这里刻意不把畸形段当成 0：否则本地版本号一旦异常，任何远端版本都会被判成"有更新"。
        /// </summary>
        private static bool TryParse(string version, out int[] parts)
        {
            parts = Array.Empty<int>();

            string trimmed = version.Trim().TrimStart('v', 'V');
            if (trimmed.Length == 0) return false;

            string[] segments = trimmed.Split('.');
            if (segments.Length == 0) return false;

            var parsed = new int[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                if (!int.TryParse(segments[i].Trim(), out int n)) return false;
                parsed[i] = n;
            }

            parts = parsed;
            return true;
        }
    }
}

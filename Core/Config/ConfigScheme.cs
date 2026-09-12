using System;

namespace EVEBox.Core.Config
{
    /// <summary>
    /// 配置方案数据模型
    /// </summary>
    public class ConfigScheme
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime LastUsedTime { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

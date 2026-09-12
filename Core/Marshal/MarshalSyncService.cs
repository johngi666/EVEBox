using EVEBox.Core.Mapping;
using System;
using System.IO;
using System.Text.Json;

namespace EVEBox.Core.Marshal
{
    /// <summary>
    /// Marshal 数据同步服务
    /// 统一对外入口，整合读取、解析、编码功能
    /// </summary>
    public class MarshalSyncService
    {
        private readonly MarshalReader _reader;
        private readonly MarshalParser _parser;

        public MarshalSyncService()
        {
            _reader = new MarshalReader();
            _parser = new MarshalParser();
        }

        /// <summary>
        /// 读取 .dat 文件，返回完整的 Marshal 数据对象
        /// </summary>
        public MarshalData ReadDatFile(string datPath)
        {
            if (!File.Exists(datPath))
                throw new FileNotFoundException($"文件不存在: {datPath}");

            return _reader.Read(datPath);
        }

        /// <summary>
        /// 读取 .dat 文件，返回原始 JSON 字符串
        /// </summary>
        public string ReadDatAsJson(string datPath)
        {
            if (!File.Exists(datPath))
                throw new FileNotFoundException($"文件不存在: {datPath}");

            return _reader.ReadToJsonString(datPath);
        }

        /// <summary>
        /// 将 JSON 字符串编码为 .dat 文件
        /// </summary>
        public void WriteJsonAsDat(string jsonString, string datPath)
        {
            if (string.IsNullOrEmpty(jsonString))
                throw new ArgumentException("JSON 内容不能为空");

            _reader.WriteFromJsonString(jsonString, datPath);
        }

        /// <summary>
        /// 直接解码 .dat → .json 文件
        /// </summary>
        public void DecodeToFile(string datPath, string jsonPath)
        {
            _reader.DecodeToFile(datPath, jsonPath);
        }

        /// <summary>
        /// 直接编码 .json → .dat 文件
        /// </summary>
        public void EncodeFromFile(string jsonPath, string datPath)
        {
            _reader.EncodeFromFile(jsonPath, datPath);
        }

        /// <summary>
        /// 读取 .dat 文件，提取并构建用户字段映射（返回实例，不再写全局静态状态）
        /// </summary>
        public UserFieldMapping LoadUserMappingsFromDat(string datPath)
        {
            var data = ReadDatFile(datPath);
            var mapping = new UserFieldMapping();
            mapping.BuildAll(
                data.WindowTitles,
                data.ChatChannels,
                data.OverviewTabs,
                data.CustomCommands,
                data.BookmarkFolders
            );
            return mapping;
        }

        /// <summary>
        /// 从 JSON 字符串加载用户字段映射
        /// </summary>
        public UserFieldMapping LoadUserMappingsFromJson(string jsonString)
        {
            using var document = JsonDocument.Parse(jsonString);
            var data = _parser.ExtractAll(document);
            var mapping = new UserFieldMapping();
            mapping.BuildAll(
                data.WindowTitles,
                data.ChatChannels,
                data.OverviewTabs,
                data.CustomCommands,
                data.BookmarkFolders
            );
            return mapping;
        }

        /// <summary>
        /// 获取错误码说明
        /// </summary>
        public string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                0 => "成功",
                -1 => "输入路径无效",
                -2 => "输出路径无效",
                -3 => "读取文件失败",
                -4 => "Marshal 解码失败",
                -5 => "JSON 序列化/反序列化失败",
                -6 => "写入文件失败",
                -7 => "Marshal 编码失败",
                _ => $"未知错误码: {errorCode}"
            };
        }
    }
}

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace EVEBox.Core.Marshal
{
    /// <summary>
    /// Marshal 文件读取器
    /// 负责调用 marshal_ffi.dll 将 .dat 文件解码为 JSON，并提供数据访问
    /// </summary>
    public class MarshalReader
    {
        private const string DllName = "marshal_ffi.dll";
        private static bool _dllAvailable;
        private static bool _dllChecked;
        private static readonly object _dllLock = new object();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int marshal_decode_to_json(string inputPath, string outputPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int marshal_encode_from_json(string inputPath, string outputPath);

        private readonly MarshalParser _parser;

        public MarshalReader()
        {
            _parser = new MarshalParser();
        }

        /// <summary>
        /// 检查 marshal_ffi.dll 是否可用（线程安全，只检查一次）
        /// </summary>
        public static bool IsDllAvailable()
        {
            lock (_dllLock)
            {
                if (!_dllChecked)
                {
                    _dllChecked = true;
                    try
                    {
                        // 尝试调用一个无害的操作来验证 DLL 是否可加载
                        string tempIn = System.IO.Path.GetTempFileName();
                        string tempOut = tempIn + ".json";
                        try
                        {
                            System.IO.File.WriteAllText(tempIn, "");
                            // 预期会返回错误码，但只要不抛 DllNotFoundException 就是好的
                            marshal_decode_to_json(tempIn, tempOut);
                            _dllAvailable = true;
                        }
                        catch (DllNotFoundException)
                        {
                            _dllAvailable = false;
                        }
                        catch (BadImageFormatException)
                        {
                            _dllAvailable = false;
                        }
                        catch (Exception)
                        {
                            // 其他异常（如文件格式无效）说明 DLL 本身在，但操作失败了
                            _dllAvailable = true;
                        }
                        finally
                        {
                            try { System.IO.File.Delete(tempIn); } catch { }
                            try { System.IO.File.Delete(tempOut); } catch { }
                        }
                    }
                    catch
                    {
                        _dllAvailable = false;
                    }
                }
                return _dllAvailable;
            }
        }

        /// <summary>
        /// 读取 .dat 文件并解析为 MarshalData
        /// </summary>
        public MarshalData Read(string datPath)
        {
            if (!IsDllAvailable())
                throw new InvalidOperationException("marshal_ffi.dll 不可用，无法解码 .dat 文件");

            if (!File.Exists(datPath))
                throw new FileNotFoundException($"文件不存在: {datPath}");

            string tempJsonPath = Path.GetTempFileName() + ".json";

            try
            {
                int result = marshal_decode_to_json(datPath, tempJsonPath);
                if (result != 0)
                    throw new Exception($"解码失败，错误码: {result}");

                string jsonString = File.ReadAllText(tempJsonPath);
                using var document = JsonDocument.Parse(jsonString);
                return _parser.ExtractAll(document);
            }
            finally
            {
                if (File.Exists(tempJsonPath))
                {
                    try { File.Delete(tempJsonPath); } catch { }
                }
            }
        }

        /// <summary>
        /// 读取 .dat 文件，返回原始 JSON 字符串
        /// </summary>
        public string ReadToJsonString(string datPath)
        {
            if (!IsDllAvailable())
                throw new InvalidOperationException("marshal_ffi.dll 不可用，无法解码 .dat 文件");

            if (!File.Exists(datPath))
                throw new FileNotFoundException($"文件不存在: {datPath}");

            string tempJsonPath = Path.GetTempFileName() + ".json";

            try
            {
                int result = marshal_decode_to_json(datPath, tempJsonPath);
                if (result != 0)
                    throw new Exception($"解码失败，错误码: {result}");

                return File.ReadAllText(tempJsonPath);
            }
            finally
            {
                if (File.Exists(tempJsonPath))
                {
                    try { File.Delete(tempJsonPath); } catch { }
                }
            }
        }

        /// <summary>
        /// 将 JSON 字符串写回 .dat 文件
        /// </summary>
        public void WriteFromJsonString(string jsonString, string datPath)
        {
            if (!IsDllAvailable())
                throw new InvalidOperationException("marshal_ffi.dll 不可用，无法编码 .dat 文件");

            if (string.IsNullOrEmpty(jsonString))
                throw new ArgumentException("JSON 内容不能为空");

            string tempJsonPath = Path.GetTempFileName() + ".json";

            try
            {
                File.WriteAllText(tempJsonPath, jsonString);
                int result = marshal_encode_from_json(tempJsonPath, datPath);
                if (result != 0)
                    throw new Exception($"编码失败，错误码: {result}");
            }
            finally
            {
                if (File.Exists(tempJsonPath))
                {
                    try { File.Delete(tempJsonPath); } catch { }
                }
            }
        }

        /// <summary>
        /// 直接解码 .dat 文件到指定路径的 .json 文件
        /// </summary>
        public void DecodeToFile(string datPath, string jsonPath)
        {
            if (!IsDllAvailable())
                throw new InvalidOperationException("marshal_ffi.dll 不可用，无法解码 .dat 文件");

            if (!File.Exists(datPath))
                throw new FileNotFoundException($"文件不存在: {datPath}");

            int result = marshal_decode_to_json(datPath, jsonPath);
            if (result != 0)
                throw new Exception($"解码失败，错误码: {result}");
        }

        /// <summary>
        /// 直接编码 .json 文件到指定路径的 .dat 文件
        /// </summary>
        public void EncodeFromFile(string jsonPath, string datPath)
        {
            if (!IsDllAvailable())
                throw new InvalidOperationException("marshal_ffi.dll 不可用，无法编码 .dat 文件");

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"文件不存在: {jsonPath}");

            int result = marshal_encode_from_json(jsonPath, datPath);
            if (result != 0)
                throw new Exception($"编码失败，错误码: {result}");
        }
    }
}
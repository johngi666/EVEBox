using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EVEBox.OtherTools.MoBanDaoRu
{
    /// <summary>
    /// 模板导入服务：把内置模板或用户自定义模板复制到目标文件夹。
    /// 用于种菜模板、装配方案、总览模板等（都是"复制文件到 Documents\EVE 对应目录"）。
    /// </summary>
    public class MoBanDaoRuService
    {
        private readonly string _targetFolder;
        private readonly string _fileFilter;              // 如 "*.json" / "*.xml" / "*.yaml"
        private readonly string _builtinFolder;           // 内置模板目录
        private readonly bool _neiZhiWeiWenJianJia;       // 内置模板是否为文件夹

        public MoBanDaoRuService(string targetFolder, string fileFilter, string builtinFolder, bool neiZhiWeiWenJianJia)
        {
            _targetFolder = targetFolder;
            _fileFilter = fileFilter;
            _builtinFolder = builtinFolder;
            _neiZhiWeiWenJianJia = neiZhiWeiWenJianJia;

            // 部分用户可能没有这几个文件夹，自动新建缺失的目录
            Directory.CreateDirectory(_targetFolder);
            // 内置模板目录也一并建好，用户可以直接往里放自己的模板
            Directory.CreateDirectory(_builtinFolder);
        }

        public string TargetFolder => _targetFolder;

        public string BuiltinFolder => _builtinFolder;

        public bool TargetExists => Directory.Exists(_targetFolder);

        /// <summary>目标文件夹里已有的文件（仅文件名）</summary>
        public List<string> ListTargetFiles()
        {
            if (!Directory.Exists(_targetFolder)) return new List<string>();
            return Directory.GetFiles(_targetFolder, _fileFilter)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>内置模板项：文件型列出模板文件，文件夹型列出模板文件夹</summary>
        public List<MoBanXiang> ListBuiltinItems()
        {
            var list = new List<MoBanXiang>();
            if (!Directory.Exists(_builtinFolder)) return list;

            if (_neiZhiWeiWenJianJia)
            {
                foreach (var dir in Directory.GetDirectories(_builtinFolder)
                                             .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(new MoBanXiang
                    {
                        MingCheng = Path.GetFileName(dir),
                        LuJing = dir,
                        ShiWenJianJia = true
                    });
                }
            }
            else
            {
                foreach (var file in Directory.GetFiles(_builtinFolder, _fileFilter)
                                              .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(new MoBanXiang
                    {
                        MingCheng = Path.GetFileName(file),
                        LuJing = file,
                        ShiWenJianJia = false
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// 导入：单个模板文件直接复制；模板文件夹则把其中所有模板文件（含子目录）复制到目标文件夹。
        /// 返回实际复制进去的文件数。
        /// </summary>
        public int Import(string source, Action<string> log)
        {
            if (string.IsNullOrEmpty(source))
                throw new FileNotFoundException("模板不存在", "");

            Directory.CreateDirectory(_targetFolder);

            // 模板文件夹：整包导入
            if (Directory.Exists(source))
            {
                var files = Directory.GetFiles(source, _fileFilter, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(_targetFolder, name), true);
                    log?.Invoke($"已导入：{name}");
                }
                return files.Length;
            }

            if (!File.Exists(source))
                throw new FileNotFoundException("模板文件不存在", source);

            string fileName = Path.GetFileName(source);
            File.Copy(source, Path.Combine(_targetFolder, fileName), true);
            log?.Invoke($"已导入：{fileName}");
            return 1;
        }

        /// <summary>打开目标文件夹（资源管理器）</summary>
        public void OpenTargetFolder()
        {
            if (!Directory.Exists(_targetFolder)) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_targetFolder) { UseShellExecute = true });
        }
    }
}

using System;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// 星系数据模型（两个服务器统一的标准内部结构）
    /// </summary>
    public class XingXi
    {
        /// <summary>星系ID</summary>
        public int Id { get; set; }

        /// <summary>星系名称（曙光服为中文名，欧服为英文名）</summary>
        public string Name { get; set; }

        /// <summary>坐标 X</summary>
        public double X { get; set; }

        /// <summary>坐标 Y</summary>
        public double Y { get; set; }

        /// <summary>坐标 Z</summary>
        public double Z { get; set; }

        /// <summary>安全等级</summary>
        public double Security { get; set; }

        /// <summary>名称的拼音首字母（用于模糊搜索，加载时缓存）</summary>
        public string PinyinInitial { get; set; }

        public override string ToString() => Name;
    }
}

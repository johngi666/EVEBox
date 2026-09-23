using System;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// 星系间距计算器：欧几里得直线距离 + 米/光年换算。
    /// 两个服务器共用同一套计算逻辑。
    /// </summary>
    public static class XingXiJuLiJiSuanQi
    {
        /// <summary>1 光年 = 9.46e15 米</summary>
        public const double GuangNianHuanMi = 9_460_000_000_000_000.0;

        /// <summary>
        /// 计算两个星系间的直线距离（返回光年）。
        /// </summary>
        public static double JiSuanGuangNian(XingXi from, XingXi to)
        {
            double dx = from.X - to.X;
            double dy = from.Y - to.Y;
            double dz = from.Z - to.Z;
            double mi = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return mi / GuangNianHuanMi;
        }

        /// <summary>
        /// 计算两个星系间的直线距离（返回米）。
        /// </summary>
        public static double JiSuanMi(XingXi from, XingXi to)
        {
            double dx = from.X - to.X;
            double dy = from.Y - to.Y;
            double dz = from.Z - to.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}

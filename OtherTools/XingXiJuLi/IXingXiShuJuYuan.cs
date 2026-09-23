using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// 星系数据源接口：每个服务器一个独立实现，统一输出标准 XingXi 结构。
    /// 逻辑复用、数据隔离——切换服务器时必须重新加载对应数据。
    /// </summary>
    public interface IXingXiShuJuYuan
    {
        /// <summary>服务器名称（用于界面显示）</summary>
        string ServerName { get; }

        /// <summary>加载星系数据（幂等：重复调用会清空重载）</summary>
        Task LoadAsync(Action<string> log);

        /// <summary>已加载的全部星系</summary>
        IReadOnlyList<XingXi> Systems { get; }
    }
}

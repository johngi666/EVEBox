

# EVESyncTool

EVE Online 多服同步工具 - 轻松同步游戏配置、备份数据

## 📋 项目简介

EVESyncTool 是一款专为 EVE Online 玩家设计的桌面同步工具，支持曙光服（Infinity）、国服（Serenity）和国际服（Tranquility）之间的游戏设置同步。该工具采用 C# WinForms 开发，提供直观的图形界面，让玩家可以轻松管理和同步自己的游戏配置。

## ✨ 主要功能

### 🎮 多服支持
- **曙光服 (Infinity)** - 默认服务器
- **国服 (Serenity)** - 中国服务器
- **国际服 (Tranquility)** - 全球官方服务器

### 📁 文件同步
- **用户文件同步** - 同步所有用户的全局设置
- **角色文件同步** - 按角色同步个性化配置
- **完整同步** - 一键同步所有配置
- **部分同步** - 选择性覆盖特定设置项

### 🔧 可同步设置类型
- 聊天窗口配置
- 公共频道名称
- 群聊窗口标题
- 概览标签页
- 自定义命令
- 书签文件夹
- 舰船装配配置

### 💾 备份与恢复
- 手动备份 - 随时创建配置快照
- 自动备份 - 同步前自动备份
- 快速恢复 - 一键还原备份
- 多版本管理 - 保留多个备份版本

### 🖥️ 智能功能
- **自动查找** - 智能检测游戏文件夹位置
- **深度搜索** - 全盘搜索游戏目录
- **服务器状态** - 实时显示各服务器在线状态
- **暗色模式** - 保护眼睛的深色主题
- **自动更新** - 保持工具最新版本
- **备注功能** - 为用户添加自定义备注

## 🚀 快速开始

### 环境要求
- Windows 10/11 操作系统
- .NET 6.0 或更高版本
- EVE Online 游戏客户端

### 安装步骤

1. **下载程序**
   - 从 [Gitee Releases](https://gitee.com/minisangel/EVESyncTool/releases) 下载最新版本
   - 或从 [GitHub Releases](https://github.com/minisangel/EVESyncTool/releases) 下载

2. **解压运行**
   ```bash
   # 解压下载的 ZIP 文件
   unzip EVESyncTool-v版本号.zip
   
   # 运行程序
   EVESyncTool.exe
   ```

3. **首次配置**
   - 选择所在服务器
   - 程序会自动搜索游戏文件夹
   - 如未找到，可手动选择目录

### 使用指南

#### 同步设置

1. **完整同步**
   - 点击"同步"按钮
   - 选择要同步的目标位置
   - 确认即可完成同步

2. **选择性同步**
   - 右键点击要同步的文件
   - 选择"选择性同步"
   - 勾选需要同步的设置项
   - 确认同步

#### 备份管理

1. **创建备份**
   - 点击"备份"按钮
   - 程序将自动备份当前配置

2. **恢复备份**
   - 在备份列表中选择要恢复的备份
   - 点击"还原"按钮
   - 确认恢复操作

3. **删除备份**
   - 选中要删除的备份
   - 点击"删除"按钮

#### 版本管理

- 管理多个配置方案
- 快速切换不同配置
- 备份/还原特定版本

## ⚙️ 配置说明

### 同步设置选项

| 设置项 | 说明 | 默认值 |
|--------|------|--------|
| 覆盖聊天配置 | 同步时覆盖聊天窗口设置 | ✓ |
| 覆盖公共频道名称 | 同步公共频道命名 | ✓ |
| 覆盖群聊标题 | 同步群聊窗口标题 | ✓ |
| 覆盖其他窗口标题 | 同步其他类型窗口标题 | ✓ |
| 覆盖概览标签 | 同步概览配置 | ✓ |
| 覆盖自定义命令 | 同步自定义命令设置 | ✓ |
| 覆盖书签文件夹 | 同步书签结构 | ✓ |
| 覆盖装配名称 | 同步舰船装配命名 | ✓ |

### 文件存放位置

- **本地缓存**: `AppData\Local\EVESyncTool\`
- **备份目录**: `Documents\EVESyncTool\Backup\`
- **配置文件**: `AppData\Local\EVESyncTool\config.json`

## 🛠️ 开发者指南

### 构建项目

```bash
# 克隆仓库
git clone https://gitee.com/minisangel/EVESyncTool.git

# 进入项目目录
cd EVESyncTool

# 还原依赖
dotnet restore

# 构建项目
dotnet build --configuration Release
```

### 项目结构

```
EVESyncTool/
├── Core/                    # 核心功能模块
│   ├── AppInfo.cs          # 应用信息常量
│   ├── Config/             # 配置管理
│   ├── Mapping/            # 字段映射
│   ├── Marshal/            # 数据解析
│   ├── ServerInfo.cs       # 服务器信息
│   ├── Services/           # 业务服务
│   └── UI/                 # UI 构建器
├── Dialogs/                # 对话框
│   ├── Common/             # 通用对话框
│   ├── Config/             # 配置对话框
│   ├── Info/               # 信息对话框
│   ├── Progress/           # 进度对话框
│   └── Sync/               # 同步对话框
├── tests/                  # 单元测试
└── Program.cs              # 程序入口
```

### 运行测试

```bash
dotnet test
```

## 📝 更新日志

### v1.0.0
- 初始版本发布
- 支持三大服务器
- 实现基础同步功能
- 添加备份恢复功能
- 支持暗色主题
- 集成自动更新

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🔗 相关链接

- **项目主页**: https://gitee.com/minisangel/EVESyncTool
- **GitHub 镜像**: https://github.com/minisangel/EVESyncTool
- **问题反馈**: https://gitee.com/minisangel/EVESyncTool/issues
- **EVE 中文社区**: https://www.evebbs.com/

## 📧 联系方式

- 作者: MinisAngel
- Gitee: https://gitee.com/minisangel
- GitHub: https://github.com/minisangel

---

**注意**: 使用本工具前，请务必备份您的游戏配置文件，以防数据丢失。本工具不会删除您的原始文件，但同步操作可能会覆盖目标位置的配置。
# EVE BOX

EVE Online 多服配置管理工具 —— 配置同步、备份、配置方案，外加一整套聊天记录 / 模板 / 数据查询小工具。

## 项目简介

EVE BOX 是给 EVE Online 玩家用的桌面工具，C# / WinForms 写成，单文件发布、解压即用。

它主要解决一件事：**多个账号、多个角色、三个服务器之间的游戏配置怎么统一**。游戏把配置存在
`core_user_*.dat`（账号）和 `core_char_*.dat`（角色）里，手动统一要一个个文件复制；EVE BOX 把它变成一次点击，
顺带把备份、方案版本、聊天记录归档这些周边工作一起做了。

## 功能一览

### 主界面（左侧七个标签页）

| 标签页 | 能做什么 |
|---|---|
| 配置同步 | 自动定位所选服务器的配置文件夹，把最后修改的账号配置一键覆盖到其他账号 / 角色 |
| 备份管理 | 把当前配置备份到指定目录（默认 `桌面\EVE配置备份`），随时还原、清理历史备份 |
| 配置方案管理 | 把配置存成方案（方案文件夹与 `settings_Default` 平级），一键切换 / 更新 / 单独备份还原 / 移除 |
| 其他工具 | 七个子工具，见下方表格 |
| 使用说明 | 程序内置的使用手册 |
| 操作日志 | 所有操作的明细记录，出问题先看这里 |
| 更新 | 检查并下载新版本 |

左侧底部还有：夜间模式开关、全局舰船标签开关、三服在线状态实时显示。

### 其他工具（七项）

| 工具 | 说明 | 状态 |
|---|---|---|
| 查看聊天记录 | 扫描 `文档\EVE\logs\Chatlogs`，按 **角色 → 频道 → 时间段** 提取，合并成一条时间线生成 txt，并自动用记事本打开 | 可用 |
| 配置方案导入 | 把配置方案包（一个 char 文件 + 一个 user 文件）克隆替换到所选服务器的 `settings_Default`，替换后该服务器所有角色共用这套配置 | 可用 |
| 种菜模板导入 | 行星开发模板 → `文档\EVE\PlanetaryInteractionTemplates`（`*.json`） | 可用 |
| 装配方案导入 | 装配方案 → `文档\EVE\fittings`（`*.xml`） | 可用 |
| 总览模板导入 | 总览模板 → `文档\EVE\Overview`（`*.yaml`） | 可用 |
| 星系间距查询 | 输入星系名（支持拼音首字母模糊匹配），查询两个星系的直线距离 | 可用 |
| 舰载扫描 | — | 开发中 |

四个「导入」类工具是同一套做法：

- **内置模板**已经打包进主程序，首次运行自动释放到程序目录 `templates\<模块>\`，进面板即可看到、选中就能导入；
- 也可以用**自定义导入**，从任意位置选文件或文件夹；
- 执行前会检测 EVE 客户端，游戏运行中会提示先关闭。

## 快速开始

### 环境要求

- Windows 10 / 11
- 发布版是**单文件自包含**的，无需另外安装 .NET 运行时
- 已安装并能正常登录 EVE Online

### 使用

1. 从 [Gitee Releases](https://gitee.com/minisangel/EVEBox/releases) 或 [GitHub Releases](https://github.com/johngi666/EVEBox/releases) 下载最新版压缩包；
2. 解压后双击 `EVE BOX.exe`；
3. 首次运行选好所在服务器，程序会自动搜索游戏配置文件夹；找不到时按提示手动选择或全盘深度搜索。

### 典型流程：把一套配置推给所有角色

1. 只保留一个主账号客户端，把它调成你想要的样子，然后关掉它；
2. 打开 EVE BOX，在「配置同步」里确认服务器正确、配置文件夹已定位；
3. 先点「备份」，把当前配置存一份；
4. 点「快捷覆盖」，确认后完成 —— 所有账号 / 角色就都套用这套配置了。

> 覆盖操作不可逆，**请务必先备份**。

## 数据与文件位置

| 内容 | 位置 |
|---|---|
| 程序配置 | `evesync_config.json`（与程序同目录） |
| 备份目录 | 默认 `桌面\EVE配置备份`，可在配置文件里改 |
| 游戏配置 | `%LOCALAPPDATA%\CCP\EVE\...\settings_Default`（自动查找，也可手动指定） |
| 聊天记录 | `文档\EVE\logs\Chatlogs`（需在游戏内开启聊天记录保存） |
| 聊天记录导出 | 桌面，文件名 `角色名_起止日期.txt`；同名不覆盖，自动加 `(2)`、`(3)` |
| 内置模板 | 源码在 `OtherTools\<模块>\MoBan\`，编译时打包进 exe，首次运行释放到程序目录 `templates\<模块>\` |

## 开发者指南

### 构建与测试

```bash
git clone https://gitee.com/minisangel/EVEBox.git
cd EVEBox
dotnet build --configuration Release
dotnet test
```

打包发布（单文件）：

```bash
dotnet publish -c Release
```

- 目标框架：`net8.0-windows`
- 发布配置：`PublishSingleFile` + `SelfContained` + `PublishReadyToRun`，输出到 `publish\`

### 项目结构

```
EVEBox/
├── App/                      程序壳与界面框架（主窗体、标题栏、左右侧面板、主题）
├── Common/                   通用支撑
│   ├── PeiZhi/               统一配置管理、服务器信息、角色缓存
│   ├── WenJianJia/           游戏配置文件夹查找与校验
│   ├── FuWuQiZhuangTai/      服务器在线状态
│   └── GongYong/             通用弹窗、帮助文本
├── Features/                 业务功能
│   ├── PeiZhiTongBu/         配置同步（文件同步、字段映射、marshal 解析）
│   ├── BeiFen/               备份
│   ├── PeiZhiFangAn/         配置方案
│   ├── JianChuanBiaoQian/    舰船标签
│   ├── RiZhi/                日志
│   └── GengXin/              更新
├── OtherTools/               其他工具
│   ├── MoBanDaoRu/           模板导入公共框架（种菜 / 装配 / 总览共用）
│   ├── ZhongCaiMoBan/        种菜模板导入
│   ├── ZhuangPeiFangAn/      装配方案导入
│   ├── ZongLanMoBan/         总览模板导入
│   ├── PeiZhiFangAnDaoRu/    配置方案导入
│   ├── LiaoTianJiLu/         查看聊天记录
│   ├── XingXiJuLi/           星系间距查询
│   └── EveKeHuDuanGuard.cs   EVE 客户端运行检测
├── tests/EVEBox.Tests/       单元测试（xUnit）
├── EVEBox.sln
└── EVEBox.csproj
```

### 命名约定

这个项目的命名规则比较特别，改代码前先了解：

- **类型名、命名空间、文件名用中文拼音**（可带英文后缀），例如 `ZhuChuangTi`（主窗体）、`PeiZhiManager`（配置管理）、`WenJianTongBuManager`（文件同步）；
- **成员变量、局部变量保留英文**；
- 每个功能模块的**业务代码和它的界面代码放在同一个目录**下；
- 拼音统一用 `MoBan` 表示「模板」，不要写成 `MuBan`。

## 更新日志

### v6.12（2026 年 9 月 23 日）

- 内置模板改为打包进主程序，首次运行自动释放到 templates 目录，自动更新不再丢模板

### v6.11（2026 年 9 月 23 日）

- 项目更名为 EVE BOX，界面与目录结构重构
- 其他工具新增：查看聊天记录、配置方案导入、种菜 / 装配 / 总览模板导入、星系间距查询
- 更换程序图标，清理无用设置与代码

### v6.00（2026 年 8 月 27 日）

- UI 界面大更新，代码重构

历史版本见 [Releases](https://github.com/johngi666/EVEBox/releases)。

## 相关链接

- 项目主页（Gitee）：https://gitee.com/minisangel/EVEBox
- GitHub 镜像：https://github.com/johngi666/EVEBox
- 问题反馈：https://gitee.com/minisangel/EVEBox/issues

## 许可证

MIT License，详见 [LICENSE](LICENSE)。

---

**提醒**：使用前请先备份游戏配置文件。本工具不会删除你的原始文件，但同步 / 覆盖类操作会覆盖目标位置的配置。

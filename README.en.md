# EVE BOX

A multi-server configuration manager for EVE Online — config sync, backups and scheme versions, plus a set of chat-log / template / data lookup tools.

---

## Introduction

EVE BOX is a desktop tool for EVE Online players, written in C# / WinForms. It ships as a single self-contained executable — unzip and run.

It mainly solves one problem: **how to keep game settings consistent across multiple accounts, multiple characters and three servers** (Infinity / Serenity / Tranquility). The game stores settings in `core_user_*.dat` (account) and `core_char_*.dat` (character) files, so applying one configuration everywhere normally means copying files by hand. EVE BOX turns that into a single click, and takes care of the surrounding work — backups, scheme versions and chat-log archiving.

## Features

### Main window (seven tabs on the left)

| Tab | What it does |
|---|---|
| Config Sync | Locates the settings folder of the selected server and overwrites the other accounts / characters with the most recently modified configuration |
| Backup | Backs up the current configuration to a folder (default: `Desktop\EVE配置备份`), restores or cleans up old backups |
| Config Schemes | Stores configurations as named schemes (folders next to `settings_Default`) and lets you switch / update / back up / restore / remove them |
| Other Tools | Seven sub-tools, see below |
| Help | The built-in user manual |
| Operation Log | Detailed log of everything the tool did — check here first when something goes wrong |
| Update | Checks for and downloads new versions |

The bottom of the left panel also shows: dark-mode toggle, a global ship-tag toggle, and live server status for all three servers.

### Other Tools (seven items)

| Tool | Description | Status |
|---|---|---|
| Chat Log Viewer | Scans `Documents\EVE\logs\Chatlogs`, extracts by character → channel → date range, merges everything into a single timeline, writes a `.txt` and opens it in Notepad | Ready |
| Config Scheme Import | Clones a scheme package (one char file + one user file) over the `settings_Default` of the selected server, so every character on that server shares the configuration | Ready |
| Planetary Interaction Templates | Imports PI templates into `Documents\EVE\PlanetaryInteractionTemplates` (`*.json`) | Ready |
| Fitting Import | Imports fittings into `Documents\EVE\fittings` (`*.xml`) | Ready |
| Overview Template Import | Imports overview templates into `Documents\EVE\Overview` (`*.yaml`) | Ready |
| System Distance | Looks up the straight-line distance between two systems, with pinyin / initial-letter fuzzy matching | Ready |
| Shipboard Scan | — | In development |

All four import tools work the same way:

- **Built-in templates** are packed into the executable and extracted to `templates\<module>\` on first run — they show up in the panel, pick one and import;
- **Custom import** lets you pick any file or folder from anywhere on disk;
- Before importing, the tool checks whether an EVE client is running and asks you to close the game first.

## Quick Start

### Requirements

- Windows 10 / 11
- The release build is **single-file and self-contained** — no separate .NET runtime needed
- A working EVE Online installation

### Usage

1. Download the latest archive from [Gitee Releases](https://gitee.com/minisangel/EVEBox/releases) or [GitHub Releases](https://github.com/johngi666/EVEBox/releases);
2. Unzip and run `EVE BOX.exe`;
3. On first run pick your server — the tool searches for the game settings folder automatically. If it fails, select the folder manually or run a full-disk deep search.

### Typical flow: push one configuration to every character

1. Keep a single main client, tune it the way you want, then close it;
2. Open EVE BOX, make sure the server and settings folder are correct on the *Config Sync* tab;
3. Click **Backup** first and keep a copy of the current configuration;
4. Click **Quick Overwrite** and confirm — every account and character now uses that configuration.

> Overwriting cannot be undone — **always back up first**.

## Data and File Locations

| Item | Location |
|---|---|
| Tool configuration | `evesync_config.json` (next to the executable) |
| Backup folder | Default `Desktop\EVE配置备份`, changeable in the config file |
| Game settings | `%LOCALAPPDATA%\CCP\EVE\...\settings_Default` (auto-detected, can be set manually) |
| Chat logs | `Documents\EVE\logs\Chatlogs` (chat logging must be enabled in game) |
| Built-in templates | Source in `OtherTools\<module>\MoBan\`, packed into the executable and extracted to `templates\<module>\` on first run |
| Exported chat logs | Desktop, named `<character>_<from>-<to>.txt` (never overwrites — adds `(2)` if it exists) |

## Developer Guide

### Build and test

```bash
git clone https://gitee.com/minisangel/EVEBox.git
cd EVEBox

dotnet build --configuration Release
dotnet test
```

Publish the single-file build:

```bash
dotnet publish -c Release
```

- Target framework: `net8.0-windows`
- Publish settings: `PublishSingleFile` + `SelfContained` + `PublishReadyToRun`, output goes to `publish\`

### Project structure

```
EVEBox/
├── App/                      Application shell and UI framework (main form, title bar, panels, theme)
├── Common/                   Shared infrastructure
│   ├── PeiZhi/               Unified configuration, server info, character cache
│   ├── WenJianJia/           Locating and validating game settings folders
│   ├── FuWuQiZhuangTai/      Server online status
│   └── GongYong/             Shared dialogs and help text
├── Features/                 Business features
│   ├── PeiZhiTongBu/         Config sync (file sync, field mapping, marshal parsing)
│   ├── BeiFen/               Backups
│   ├── PeiZhiFangAn/         Configuration schemes
│   ├── JianChuanBiaoQian/    Ship tags
│   ├── RiZhi/                Logging
│   └── GengXin/              Auto update
├── OtherTools/               Other tools
│   ├── MoBanDaoRu/           Shared template-import framework (PI / fitting / overview)
│   ├── ZhongCaiMoBan/        Planetary interaction template import
│   ├── ZhuangPeiFangAn/      Fitting import
│   ├── ZongLanMoBan/         Overview template import
│   ├── PeiZhiFangAnDaoRu/    Config scheme import
│   ├── LiaoTianJiLu/         Chat log viewer
│   ├── XingXiJuLi/           System distance lookup
│   └── EveKeHuDuanGuard.cs   EVE client running check
├── tests/EVEBox.Tests/       Unit tests (xUnit)
├── EVEBox.sln
└── EVEBox.csproj
```

### Naming convention

The naming rules are unusual, so read this before changing code:

- **Type names, namespaces and file names use Chinese pinyin** (optionally with an English suffix), e.g. `ZhuChuangTi` (main form), `PeiZhiManager` (config manager), `WenJianTongBuManager` (file sync manager);
- **Member variables, local variables and the semantics of public APIs stay in English**;
- A feature module keeps its **business code and its UI code in the same directory**;
- "Template" is always spelled `MoBan`, never `MuBan`.

## Changelog

### v6.12 (September 23, 2026)
- Built-in templates are now packed into the executable and extracted on first run, so auto-update no longer loses them

### v6.11 (September 23, 2026)
- Renamed to EVE BOX; UI and folder structure refactored
- New Other Tools: chat log viewer, config scheme import, PI / fitting / overview template import, system distance
- New app icon; removed unused settings and dead code

### v6.00 (August 27, 2026)
- Major UI overhaul and code refactoring

> Older versions are listed under [Releases](https://github.com/johngi666/EVEBox/releases).

## Links

- Project home (Gitee): https://gitee.com/minisangel/EVEBox
- GitHub mirror: https://github.com/johngi666/EVEBox
- Issues: https://gitee.com/minisangel/EVEBox/issues

## License

MIT License — see [LICENSE](LICENSE).

---

**Note**: Always back up your game configuration files before using this tool. It never deletes your original files, but sync / overwrite operations will overwrite the configuration at the target location.

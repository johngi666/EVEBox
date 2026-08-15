# EVESyncTool

EVE Online Multi-Server Sync Tool - Easily sync game configurations, backup data

## 📋 Project Introduction

EVESyncTool is a desktop synchronization tool designed specifically for EVE Online players, supporting game setting synchronization between the Infinity Server (Infinity), Serenity Server (Serenity), and Tranquility Server (Tranquility). Developed using C# WinForms, this tool provides an intuitive graphical interface, allowing players to easily manage and synchronize their game configurations.

## ✨ Main Features

### 🎮 Multi-Server Support
- **Infinity Server (Infinity)** - Default Server
- **Serenity Server (Serenity)** - China Server
- **Tranquility Server (Tranquility)** - Global Official Server

### 📁 File Synchronization
- **User File Sync** - Synchronize global settings for all users
- **Character File Sync** - Synchronize personalized configurations by character
- **Full Sync** - One-click synchronization of all configurations
- **Partial Sync** - Selectively overwrite specific setting items

### 🔧 Synchronizable Setting Types
- Chat window configuration
- Public channel names
- Group chat window titles
- Overview tabs
- Custom commands
- Bookmark folders
- Ship fitting configurations

### 💾 Backup & Recovery
- **Manual Backup** - Create configuration snapshots anytime
- **Automatic Backup** - Automatic backup before synchronization
- **Quick Restore** - One-click restoration of backups
- **Multi-Version Management** - Retain multiple backup versions

### 🖥️ Smart Features
- **Auto-Detection** - Intelligently detect game folder locations
- **Deep Search** - Full disk search of game directories
- **Server Status** - Real-time display of online status for each server
- **Dark Mode** - Eye-friendly dark theme
- **Auto-Update** - Keep the tool at the latest version
- **Notes Feature** - Add custom notes for users

## 🚀 Quick Start

### Environment Requirements
- Windows 10/11 Operating System
- .NET 6.0 or higher
- EVE Online Game Client

### Installation Steps

1. **Download Program**
   - Download the latest version from [Gitee Releases](https://gitee.com/minisangel/EVESyncTool/releases)
   - Or download from [GitHub Releases](https://github.com/minisangel/EVESyncTool/releases)

2. **Extract and Run**
   ```bash
   # Unzip the downloaded ZIP file
   unzip EVESyncTool-vVersionNumber.zip
   
   # Run the program
   EVESyncTool.exe
   ```

3. **First-Time Configuration**
   - Select your current server
   - The program will automatically search for the game folder
   - If not found, you can manually select the directory

### Usage Guide

#### Sync Settings

1. **Full Synchronization**
   - Click the "Sync" button
   - Select the target location for synchronization
   - Confirm to complete synchronization

2. **Selective Synchronization**
   - Right-click the file to synchronize
   - Select "Selective Sync"
   - Check the setting items to synchronize
   - Confirm synchronization

#### Backup Management

1. **Create Backup**
   - Click the "Backup" button
   - The program will automatically backup the current configuration

2. **Restore Backup**
   - Select the backup to restore from the backup list
   - Click the "Restore" button
   - Confirm the restore operation

3. **Delete Backup**
   - Select the backup to delete
   - Click the "Delete" button

#### Version Management

- Manage multiple configuration schemes
- Quickly switch between different configurations
- Backup/Restore specific versions

## ⚙️ Configuration Instructions

### Sync Settings Options

| Setting | Description | Default |
|--------|------|--------|
| Overwrite Chat Config | Overwrite chat window settings during sync | ✓ |
| Overwrite Public Channel Names | Synchronize public channel naming | ✓ |
| Overwrite Group Chat Titles | Synchronize group chat window titles | ✓ |
| Overwrite Other Window Titles | Synchronize other type window titles | ✓ |
| Overwrite Overview Tabs | Synchronize overview configuration | ✓ |
| Overwrite Custom Commands | Synchronize custom command settings | ✓ |
| Overwrite Bookmark Folders | Synchronize bookmark structure | ✓ |
| Overwrite Fitting Names | Synchronize ship fitting naming | ✓ |

### File Storage Locations

- **Local Cache**: `AppData\Local\EVESyncTool\`
- **Backup Directory**: `Documents\EVESyncTool\Backup\`
- **Configuration File**: `AppData\Local\EVESyncTool\config.json`

## 🛠️ Developer Guide

### Build Project

```bash
# Clone repository
git clone https://gitee.com/minisangel/EVESyncTool.git

# Enter project directory
cd EVESyncTool

# Restore dependencies
dotnet restore

# Build project
dotnet build --configuration Release
```

### Project Structure

```
EVESyncTool/
├── Core/                    # Core Function Modules
│   ├── AppInfo.cs          # Application Info Constants
│   ├── Config/             # Configuration Management
│   ├── Mapping/            # Field Mapping
│   ├── Marshal/            # Data Parsing
│   ├── ServerInfo.cs       # Server Information
│   ├── Services/           # Business Services
│   └── UI/                 # UI Builder
├── Dialogs/                # Dialogs
│   ├── Common/             # Common Dialogs
│   ├── Config/             # Configuration Dialogs
│   ├── Info/               # Info Dialogs
│   ├── Progress/           # Progress Dialogs
│   └── Sync/               # Sync Dialogs
├── tests/                  # Unit Tests
└── Program.cs              # Program Entry Point
```

### Run Tests

```bash
dotnet test
```

## 📝 Changelog

### v1.0.0
- Initial version release
- Support for three major servers
- Implemented basic sync functionality
- Added backup and restore functionality
- Support for dark theme
- Integrated auto-update

## 🤝 Contributing

Welcome to submit Issues and Pull Requests!

1. Fork this repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Create a Pull Request

## 📄 License

This project uses the MIT License - See [LICENSE](LICENSE) file for details

## 🔗 Related Links

- **Project Homepage**: https://gitee.com/minisangel/EVESyncTool
- **GitHub Mirror**: https://github.com/minisangel/EVESyncTool
- **Issue Feedback**: https://gitee.com/minisangel/EVESyncTool/issues
- **EVE Chinese Community**: https://www.evebbs.com/

## 📧 Contact Info

- Author: MinisAngel
- Gitee: https://gitee.com/minisangel
- GitHub: https://github.com/minisangel

---

**Note**: Please make sure to backup your game configuration files before using this tool to prevent data loss. This tool will not delete your original files, but synchronization operations may overwrite configurations at the target location.
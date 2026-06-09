# SXXI - Steam Account Manager

A local Windows desktop application for managing multiple Steam accounts. Import, organize, and switch between Steam accounts with ease.

## Features

- **Account Import** - Import accounts via text input, file drag-drop, or clipboard paste
- **One-Click Login** - Switch between Steam accounts instantly
- **Account Management** - Add, remove, and organize your Steam accounts
- **Config Backup/Restore** - Save and restore Steam configuration
- **Profile Viewer** - Open Steam profiles directly in browser
- **Export** - Export selected accounts to text file

## Requirements

- Windows 10/11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Steam installed on your system

## Build from Source

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Steps

```bash
# Clone the repository
git clone <repo-url>
cd nfa

# Build (Debug)
dotnet build sxxi.sln

# Build (Release)
dotnet build sxxi.sln -c Release

# Publish as single executable
dotnet publish sxxi/sxxi.csproj -c Release
```

The output will be in:
- Debug: `sxxi/bin/Debug/net8.0-windows/win-x64/`
- Release: `sxxi/bin/Release/net8.0-windows/win-x64/`
- Published: `sxxi/bin/Release/net8.0-windows/win-x64/publish/`

## Usage Tutorial

### 1. Launch the Application

Run `sxxi.exe` from the build output folder.

### 2. Import Accounts

You can import accounts in **3 ways**:

#### Method A: Manual Input
1. Click **"Import Token"** button
2. Enter account data in the format:
   ```
   username----token
   ```
   One account per line. Example:
   ```
   myaccount1----eyJhbGciOiJSUzI1NiIs...
   myaccount2----eyJhbGciOiJSUzI1NiIs...
   ```
3. Click **"Import"**

#### Method B: Drag & Drop File
1. Create a `.txt` file with account data (same format as above)
2. Drag the file directly onto the accounts table

#### Method C: Clipboard Paste
1. Copy account data to clipboard (Ctrl+C)
2. Click on the accounts table
3. Press **Ctrl+V** to paste and import

### 3. Login to an Account

1. **Right-click** on the account row
2. Select **"Login with this account"**
3. Steam will launch automatically with the selected account

### 4. Open Profile in Browser

1. **Right-click** on the account row
2. Select **"Open profile in browser"**
3. The Steam community profile will open in your default browser

### 5. Remove Accounts

1. Select one or more account rows
2. **Right-click** and select **"Remove selected"**

### 6. Export Accounts

1. Select the accounts you want to export
2. Click **"Export Selected..."**
3. Choose a location and filename
4. Accounts will be saved in `username----token` format

### 7. Steam Controls

| Button | Function |
|--------|----------|
| **Reset Steam** | Clear Steam config and userdata, then restart Steam |
| **Launch Steam** | Start Steam normally |
| **Kill Steam Process** | Force close Steam and SteamWebHelper |
| **Save Config** | Backup current Steam config to `backup/` folder |
| **Restore Config** | Restore Steam config from backup |

### 8. Check Login Status

The status panel on the right shows the currently logged-in Steam account.

## Data Storage

- Accounts are stored locally in `accounts.json` (same folder as executable)
- Config backups are stored in `backup/` folder
- **No data is sent to any external server**

## Disclaimer

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.**

- This tool is for **educational and research purposes only**
- The author is **not responsible** for any misuse, account restrictions, or legal consequences
- Users are **solely responsible** for complying with Steam's Terms of Service
- This project is **not affiliated with Valve Corporation** in any way

## License

All Rights Reserved. No license is granted for use, modification, or distribution.

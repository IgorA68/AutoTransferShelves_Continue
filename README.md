# 📦 AutoTransferShelves — Build & Deployment Automation

This repository contains the source code and automation scripts for the AutoTransferShelves mod for RimWorld 1.6. It includes a streamlined build system that compiles the mod, cleans up unnecessary files, and automatically updates deployment paths for local testing.

## 🛠 Requirements

- RimWorld 1.6 (Steam or GOG)
- .NET SDK 6.0+
- Windows OS with cmd.exe and PowerShell available

## 🚀 What build.bat Does

- Sets the RimWorld installation path via a variable
- Generates Directory.Build.props with the correct <RimWorldPath> for compilation
- Compiles the mod using dotnet build (Release configuration)
- Cleans the Assemblies folder, leaving only the final AutoTransferShelves.dll
- Updates the copy_to_mod.bat script with the correct mod deployment path
- Pauses at the end so you can review the output

## ⚙️ Configuration

Open build.bat and set the correct path to your RimWorld installation:

set "RIMWORLD_PATH=drive:\path" 
for example: 
set "RIMWORLD_PATH=C:\Rimworld" 

This path will be used both for compilation and for copying the mod to the Mods folder.

## 📤 Deploying the Mod

After building, run:

copy_to_mod.bat

This script will:

- Delete the existing mod folder in RimWorld\Mods\AutoTransferShelves
- Copy all relevant subfolders (About, Assemblies, Defs, etc.) into the game’s Mods directory
- Only copies folders that exist — safe for modular development

## 🧪 Example Workflow

1. Edit your source code in Source/
2. Run build.bat
3. If compilation succeeds, run copy_to_mod.bat
4. Launch RimWorld and test your mod

## ⚡ Transfer Optimization

The core item transfer logic has been optimized for performance and clarity:

- Slot groups and item lists are cached to avoid redundant access
- LINQ queries are minimized and simplified
- Transfer conditions and failure flags are streamlined
- All behavior remains identical to the original version

This ensures smoother performance during gameplay, especially when multiple shelves are active.

## 🌐 Localization Support

This mod includes built-in support for localization. A full Russian translation is provided and automatically deployed if the Languages/Russian/ folder is present.

- All in-game messages, interface elements, and notifications are translated
- C# code uses translation keys via Translate() for dynamic language switching
- Folder structure supports modular language packs
- Automation scripts are encoding-safe and compatible with Cyrillic characters

## 📝 Notes

- Directory.Build.props is auto-generated every time you build — no need to edit it manually
- copy_to_mod.bat is automatically updated with the correct path — no need to hardcode anything
- The system is designed for modular, error-free, and repeatable development
- This mod is based on the original Auto-Transfer Shelves by SilkCircuit:  
  https://steamcommunity.com/sharedfiles/filedetails/?id=3399298304

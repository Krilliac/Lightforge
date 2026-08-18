# Project System

How Lightforge projects work, from creation to daily use.

## Creating a Project

**File > New Project** (or Ctrl+N) prompts for:

1. **Project name** -- Used as the folder name and display title
2. **Parent directory** -- Where the project folder will be created
3. **WoW client path** (optional) -- Path to the WoW installation for this project
4. **Target expansion** -- Which WoW version this project targets (defaults to toolbar selection)

Lightforge creates a directory structure:

```
ProjectName/
  lightforge.project       # Project metadata (JSON)
  Maps/                    # ADT terrain files
  DBCs/                    # DBC/DB2 database files
  Models/                  # M2 and WMO model files
  Textures/                # BLP texture files
  Patches/                 # MPQ/CASC patch files for output
  SQL/                     # Database SQL scripts
  Lua/                     # Lua addon/script files
  Exports/                 # Exported assets and conversions
  Config/                  # Configuration files
```

## Project File Format

The `lightforge.project` file is a JSON file:

```json
{
  "Name": "MyProject",
  "ClientPath": "C:\\Games\\WoW335",
  "Expansion": "WotLK 3.3.5a",
  "Created": "2026-08-17T20:00:00",
  "LastOpened": "2026-08-17T21:30:00"
}
```

| Field | Description |
|-------|-------------|
| Name | Display name |
| ClientPath | Path to WoW game client (used by some tools) |
| Expansion | Target expansion display string |
| Created | Creation timestamp |
| LastOpened | Last time the project was opened (auto-updated) |

## Opening a Project

Three ways to open:

1. **File > Open Project** (Ctrl+O) -- Browse for a `lightforge.project` file
2. **Recent Projects** -- Click a project name on the welcome screen or in File > Open Recent
3. **Drag and drop** -- Drop a `.project` file onto the Lightforge window

When opened, the project's expansion setting syncs with the toolbar version selector, and the tool panel filters accordingly.

## Workspace Layout

Once a project is loaded, the workspace has three panels:

### File Tree (left)
- Shows the project directory structure
- Double-click files to open them in the editor
- Right-click for context menu: Open, Rename, Delete, Copy Path, Open in Explorer, Open with Default App
- Drag files from Windows Explorer to import them
- Refresh button and collapse button in the header
- Auto-refreshes when files change on disk (FileSystemWatcher)

### Editor (center)
- Tabbed file editor supporting multiple open files
- Syntax highlighting for: SQL, Lua, XML, GLSL, JSON, INI/TOML/YAML, WoW config files
- Ctrl+S saves the active file
- Ctrl+W closes the active tab
- Click a tab to switch, middle sections show the file path

### Output Log (bottom)
- Shows tool launch output, build results, and status messages
- Clear button to reset
- Auto-scrolls to latest entry

## File Operations

### Editing Files
Double-click any text file in the project tree to open it in the built-in editor. Supported formats include `.sql`, `.lua`, `.xml`, `.glsl`, `.json`, `.ini`, `.toml`, `.yaml`, `.conf`, `.wtf`, `.toc`, and plain text.

### Find in Files
**View > Find in Files** (Ctrl+Shift+F) searches all text files in the project directory. Results show file path and matching line with context.

### Project Statistics
**View > Project Statistics** shows a breakdown of file counts and sizes by type across the project directory.

### Export as ZIP
**File > Export Project as ZIP** creates a compressed archive of the entire project folder.

## Building

**Build Patch** (Ctrl+B or toolbar button) zips the project's `Patches/` directory contents into a distributable archive. The output log shows progress and the final path.

## Recent Projects

Lightforge tracks the 8 most recently opened projects, stored in:

```
%AppData%/Lightforge/recent.json
```

Recent projects appear on the welcome screen and in File > Open Recent. Each entry stores the project name and path. Non-existent paths are skipped.

## Version Switching

The toolbar dropdown shows all supported WoW versions. Changing it:

1. Updates the tool panel filter (shows only compatible tools)
2. If a project is open, saves the new expansion to the project file
3. Persists across sessions (saved in the project metadata)

The "All Versions" option shows every tool regardless of compatibility, useful for browsing the full catalog.

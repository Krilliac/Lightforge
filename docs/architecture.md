# Architecture

Lightforge is a .NET 8 WinForms application that serves as a unified launcher and workspace for World of Warcraft modding tools.

## Source Files

### Mainform.cs (~2800 lines)

The main application window. Handles:

- **UI construction** -- Dark-themed toolbar, menu bar, welcome screen, and workspace panel all built in code (no Designer file). Uses `Theme.cs` constants throughout.
- **Welcome screen** -- Centered panel with Create/Open buttons and recent project list. Transitions to workspace on project load.
- **Workspace layout** -- Three-panel split:
  - Left: project file tree (TreeView with drag-drop, context menus, file watching)
  - Center: tabbed file editor with syntax highlighting
  - Bottom: output log panel
- **Tool panel** -- Right sidebar showing tool cards filtered by the selected WoW version. Each card displays name, description, category, status badge, and version tags.
- **Editor** -- RichTextBox-based tabbed editor with syntax highlighting for SQL, Lua, XML, GLSL, JSON, and config files. Supports multiple open files, Ctrl+S save, and Ctrl+W close.
- **File tree** -- Recursive directory browser with context menus (open, rename, delete, copy path, open in explorer). Watches the project directory for changes via FileSystemWatcher.
- **Build system** -- "Build Patch" zips the entire project directory for distribution.
- **Menus** -- File (new/open/recent/export/settings), Tools (quick-launch for common tools, validate project), View (refresh, statistics, find-in-files, toggle output), Help (sources, shortcuts, about).

### WowToolRegistry.cs (~500 lines)

Central tool catalog. Defines:

- **WowVersion enum** -- Vanilla through BfA plus an All sentinel.
- **WowVersionInfo** -- Display names, build numbers, and MPQ/CASC classification for each version.
- **ToolEntry record** -- Name, description, relative executable path, category, compatible version array, source URL, and maintenance status.
- **Version arrays** -- Reusable arrays like `AllMpq`, `AllCasc`, `ClassicTrio`, `CataPlus`, `AllVersions` to avoid repetition.
- **GetAllTools()** -- Returns the complete tool list. Used by the tool panel, menus, and validation.
- **IsCompatible()** -- Checks if a tool works with a given WoW version.

### Theme.cs (~50 lines)

Static color and font definitions for the dark UI theme:

- **Colors** -- Deep blacks (21-42 range) for backgrounds, blue accent (#007ACC), gold (#CEAF69) for branding, semantic colors for success/warning/error, and five text brightness levels.
- **Fonts** -- Segoe UI at various weights for UI text, Cascadia Code for the editor.
- **ApplyTo()** -- Applies the theme to any WinForms control.

### LightforgeProject.cs (~100 lines)

Project model and persistence:

- **Properties** -- Name, ClientPath (WoW installation), Expansion (display string), Created/LastOpened timestamps.
- **Folder structure** -- Projects create 9 standard subdirectories: Maps, DBCs, Models, Textures, Patches, SQL, Lua, Exports, Config.
- **Serialization** -- JSON via System.Text.Json, saved as `lightforge.project` in the project root.
- **RecentProjects** -- Tracks last 8 opened projects in `%AppData%/Lightforge/recent.json`.

## Design Decisions

**No Designer files.** The entire UI is built in code. This makes theming consistent and avoids the fragility of Designer-generated layout code.

**Static registry, not plugin discovery.** Tools are hardcoded in `WowToolRegistry.cs` rather than discovered at runtime. This ensures every tool has correct version metadata and status information.

**Version filtering at the UI layer.** The registry stores which versions each tool supports. The UI reads the selected version from the toolbar dropdown and calls `IsCompatible()` to filter. No separate configuration files.

**Status-based coloring.** Tool cards are color-coded by maintenance status: Active (green), Maintained/Stable (blue), Stale/Dormant (amber), Archived/Dead (red). This helps users avoid investing time in abandoned tools.

## Runtime Requirements

- .NET 8 runtime (Windows)
- Windows 10 or later (WinForms dependency)
- Tool executables placed under `Toolset Binaries/` adjacent to the Lightforge executable

# Lightforge Documentation

## Guides

- [Architecture](architecture.md) -- Codebase structure, source files, and design decisions
- [Tool Catalog](tool-catalog.md) -- Complete reference for all 63 registered tools
- [Version Compatibility](version-compatibility.md) -- WoW file format differences by expansion (ADT, M2, DBC, WMO)
- [Adding Tools](adding-tools.md) -- How to register a new tool in the launcher
- [Project System](project-system.md) -- Creating projects, workspace layout, file operations

## Quick Reference

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New project |
| Ctrl+O | Open project |
| Ctrl+S | Save active file |
| Ctrl+W | Close active editor tab |
| Ctrl+B | Build patch (ZIP export) |
| Ctrl+Shift+F | Find in files |
| F5 | Refresh file tree |

### WoW Version Coverage

| Era | Versions | Archive | Key Tools |
|-----|----------|---------|-----------|
| Classic | Vanilla, TBC, WotLK | MPQ | Noggit, Spell Editor, M2Mod, Keira3 |
| Transition | Cata, MoP | MPQ | WDBXEditor, WoW Database Editor |
| Modern | WoD, Legion, BfA | CASC | Arctium, CASCExplorer, wow-patcher |
| Universal | All | Both | WMVx, wow.export, WowPacketParser |

### Project Folder Structure

```
ProjectName/
  lightforge.project    # Project metadata
  Maps/                 # ADT terrain files
  DBCs/                 # Database files
  Models/               # M2/WMO models
  Textures/             # BLP textures
  Patches/              # Output patch files
  SQL/                  # Database scripts
  Lua/                  # Addon/script files
  Exports/              # Converted assets
  Config/               # Configuration
```

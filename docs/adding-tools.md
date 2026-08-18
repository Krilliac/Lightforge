# Adding Tools to Lightforge

How to register a new tool in the Lightforge launcher.

## Step 1: Add the Tool Entry

Open `Lightforge/WowToolRegistry.cs` and add a new `ToolEntry` to the array returned by `GetAllTools()`.

```csharp
new("Tool Name", "Short description of what it does",
    @"ToolFolder\Subfolder", "CATEGORY", VersionArray,
    "https://github.com/author/repo", "Active"),
```

### Fields

| Field | Type | Description |
|-------|------|-------------|
| Name | string | Display name shown on the tool card |
| Description | string | One-line description shown below the name |
| RelativePath | string | Path to the executable, relative to `Toolset Binaries/` |
| Category | string | Tool category (see list below) |
| Compatible | WowVersion[] | Array of supported WoW versions |
| Source | string | Source repository URL (shown in context menu) |
| Status | string | Maintenance status (see list below) |

### Categories

Place the tool in the section that best matches its purpose:

- `WORLD EDITING` -- Map, terrain, and environment editors
- `DATA EDITING` -- DBC/DB2 database and table editors
- `MODEL TOOLS` -- M2/WMO model converters and editors
- `VIEWERS` -- 3D model and asset viewers
- `GENERATORS` -- Procedural content and batch generators
- `PACKAGING` -- MPQ/CASC archive tools
- `NETWORK` -- Packet capture and analysis
- `CLIENT PATCHING` -- Client binary modification tools
- `RETROPORTING` -- Asset conversion between versions
- `SERVER ADMIN` -- Server management tools
- `LIBRARIES` -- Programming libraries and CLI tools
- `ADT CLI` -- Command-line ADT utilities

### Version Arrays

Use the predefined arrays for common version sets:

```csharp
WotlkOnly       // [WotLK]
ClassicTrio     // [Vanilla, TBC, WotLK]
WotlkCata       // [WotLK, Cata]
AllMpq          // [Vanilla, TBC, WotLK, Cata, MoP]
AllCasc         // [WoD, Legion, BfA]
CataPlus        // [Cata, MoP, WoD, Legion, BfA]
AllVersions     // [Vanilla, TBC, WotLK, Cata, MoP, WoD, Legion, BfA]
```

Or create a custom array:

```csharp
[WowVersion.WotLK, WowVersion.Cata, WowVersion.MoP]
```

### Status Values

| Status | When to use | Card color |
|--------|------------|------------|
| `"Active"` | Recent commits, actively maintained | Green |
| `"Maintained"` | Working, occasional updates | Blue |
| `"Stable"` | Feature-complete, no changes needed | Blue |
| `"Stale"` | No recent activity, may still work | Amber |
| `"Dormant"` | Abandoned but functional | Amber |
| `"Archived"` | Read-only archive, no development | Red |
| `"Complete"` | Finished tool, does its job | Green |

## Step 2: Place the Executable

Place the tool's executable and dependencies under the `Toolset Binaries` directory, matching the `RelativePath` you specified:

```
Toolset Binaries/
  ToolFolder/
    Subfolder/
      ToolName.exe
      dependencies.dll
      ...
```

Lightforge looks for an `.exe` file in the specified path. It tries the folder path first, then looks for any executable inside it.

## Step 3: Update SOURCES.md

Add an entry to `SOURCES.md` in the appropriate category table:

```markdown
| **Tool Name** | https://github.com/author/repo | Active | Vanilla-WotLK |
```

## Step 4: Test

1. Build and run Lightforge
2. Select the appropriate WoW version in the toolbar dropdown
3. Verify the tool card appears in the tool panel
4. Right-click the card and check:
   - "Launch" opens the tool
   - "Open Source Repo" opens the correct URL
   - "Open Folder" navigates to the tool directory
5. Switch to a version the tool doesn't support and verify it disappears

## Example: Adding a Hypothetical Tool

```csharp
new("MyDBCTool", "Custom DBC editor with merge support",
    @"MyDBCTool\MyDBCTool", "DATA EDITING",
    [WowVersion.Vanilla, WowVersion.TBC, WowVersion.WotLK],
    "https://github.com/user/my-dbc-tool", "Active"),
```

This adds a DBC editor that appears when Vanilla, TBC, or WotLK is selected, with an Active (green) status badge.

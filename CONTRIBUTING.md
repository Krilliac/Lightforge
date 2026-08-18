# Contributing to Lightforge

Thanks for your interest in contributing. This guide covers the basics.

## Getting Started

1. Fork and clone the repository
2. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
3. Build: `dotnet build Lightforge.sln`
4. Run the GUI: `dotnet run --project Lightforge/Lightforge.csproj`
5. Run the CLI: `dotnet run --project Lightforge.CLI/Lightforge.CLI.csproj`

## Project Layout

```
Lightforge/              GUI (WinForms)
  Mainform.cs            Main window, workspace, editor, menus
  Theme.cs               Dark theme colors and fonts
  WowToolRegistry.cs     Tool catalog with version compatibility
  LightforgeProject.cs   Project file create/open/save

Lightforge.CLI/          CLI tool
  Program.cs             Command routing and launcher commands
  DbcTool.cs             DBC/DB2 binary parser and diff
  BlpTool.cs             BLP texture header reader
  AdtTool.cs             ADT chunk parser
  ListfileTool.cs        Community listfile search
  SqlGenTool.cs          SQL template generator

Lightforge.Tests/        Unit tests
```

## Adding a Tool

The tool catalog lives in `WowToolRegistry.cs`. Each tool is a `ToolEntry` record:

```csharp
new ToolEntry(
    "Tool Name",
    "Short description",
    @"RelativePath\to\executable",
    "CATEGORY",
    new[] { WowVersion.WotLK },
    "https://github.com/source-repo",
    "Active"  // Active, Maintained, Stable, Stale, Archived
)
```

See [docs/adding-tools.md](docs/adding-tools.md) for the full guide with version arrays and category list.

## Adding a CLI Command

1. Create a new `XyzTool.cs` in `Lightforge.CLI/`
2. Add a static class with a static method returning `int`
3. Route it in `Program.cs` switch expression
4. Add it to the usage output in `PrintUsage()`

## Adding a SQL Template

Add a new static method in `SqlGenTool.cs` that returns the SQL string, then add the routing in the `Generate` method's switch expression.

## Code Style

- No Designer files. All UI is built in code (Mainform constructor).
- Dark theme colors come from `Theme.cs`. Don't hardcode colors in Mainform.
- Version compatibility arrays are defined at the top of `WowToolRegistry.cs` (`AllMpq`, `ClassicTrio`, etc.).
- The CLI shares source files from the GUI project via MSBuild `<Compile Include>` links, not code duplication.
- No comments unless the WHY is non-obvious.

## Pull Requests

- Keep PRs focused on one thing
- Build must pass: `dotnet build Lightforge.sln -c Release`
- Tests must pass: `dotnet test`
- Test UI changes visually before submitting

## Patches

The `patches/` directory contains source patches for upstream tools that extend their version support. If you've patched a community tool to work with a new WoW version, consider contributing it. See [patches/README.md](patches/README.md) for the format.

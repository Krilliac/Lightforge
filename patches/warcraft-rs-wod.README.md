# warcraft-rs WoD Extension Patch

## What This Is

A patch specification and diff for extending
[warcraft-rs](https://github.com/wowemulation-dev/warcraft-rs) (v0.7.0)
from its current Vanilla--MoP (1.x--5.x) support toward WoD (6.x).

## Key Finding

**Most WoD file formats already parse without any code changes.** The
binary structures for BLP, M2, WMO, WDT, and ADT did not change between
MoP (5.x) and WoD (6.x) in ways that break the existing parsers.

## What Already Works (No Patch Needed)

| Format | Why It Works |
|--------|-------------|
| **BLP** | BLP2 magic is version-agnostic. Same binary layout TBC through WoD. |
| **M2** | WoD uses MD20 magic + header version 272, identical to Cata/MoP. |
| **WMO** | `from_raw(18)` already maps to `WmoVersion::Wod`. Parser accepts it. |
| **WDT** | `WowVersion::WoD` variant exists. Major version 6 maps correctly. |
| **ADT** | Detected as MoP (same chunks). Parses identically. |

## What the Patch Does

Three small changes for correctness and API completeness:

1. **M2 version fix** -- Changes `to_header_version(WoD)` from the
   theoretical `275` to the correct `272`. Updates comment to acknowledge
   that version 272 spans Cata/MoP/WoD.

2. **ADT WoD variant** -- Adds `AdtVersion::WoD` to the enum with
   `from_expansion_name("wod")` support. Does NOT change auto-detection
   (WoD ADTs are structurally identical to MoP and correctly parse as such).

3. **WDB6 recognition** -- Adds `WDB6` to `DbcVersion` enum and `detect()`
   method. Files are recognized with a clear error message instead of
   "Unknown DBC version". Full WDB6 parsing is deferred (needs column
   compression support).

## What Still Needs Implementation

### WDB6 Parser (Medium effort, ~200-400 lines)

WoD introduced the WDB6 database format for some tables. It extends WDB5
with per-column compression (common data columns, pallet-compressed columns).
The patch adds recognition but not parsing. Many WoD tables also ship as
WDB5, which already works.

### CASC Archive Reader (Large effort, ~2000-5000 lines)

This is the **fundamental blocker**. WoD replaced MPQ archives with CASC
(Content Addressable Storage Container). Without a CASC reader, WoD game
data cannot be extracted from a client installation. Users must pre-extract
files using an external tool (CascLib, CascViewer).

A `wow-casc` crate would be a natural addition alongside `wow-mpq` in
`file-formats/archives/`.

## Files

- `warcraft-rs-wod.spec.md` -- Detailed format-by-format analysis with
  exact code references, version mappings, and rationale
- `warcraft-rs-wod.patch` -- Unified diff for the three concrete changes

## How to Apply

The `.patch` file uses placeholder line numbers (the source was read via
GitHub raw URLs, not a local checkout). To apply:

1. Clone warcraft-rs
2. Manually apply the changes described in the diff, using the function
   names and surrounding context to locate the correct insertion points
3. Run `cargo check` to verify compilation
4. Run `cargo test` to verify no regressions

Or use the `.spec.md` as a reference and make the changes by hand -- each
change is small and self-contained.

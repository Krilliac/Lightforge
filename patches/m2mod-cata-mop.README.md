# m2mod-cata-mop.patch

Extends **M2Mod** (https://github.com/M2Mod/m2mod) to correctly support
Cataclysm (M2 version 272) and Mists of Pandaria (M2 versions 272-274),
and lowers the version floor to 256 so Classic/Vanilla M2 files are accepted.

## What the patch does

### 1. Fixes `GetExpansion()` version-to-expansion mapping (M2Lib/M2.cpp)

The original mapping was wrong for versions 272+:

| Version | Before (wrong)      | After (correct)      |
|---------|---------------------|----------------------|
| < 260   | BurningCrusade      | **Classic**          |
| 260-263 | BurningCrusade      | BurningCrusade       |
| 264     | WrathOfTheLichKing  | WrathOfTheLichKing   |
| 265-271 | Cataclysm (*)       | WrathOfTheLichKing   |
| 272     | WarlordsOfDraenor   | **Cataclysm**        |
| 273     | WarlordsOfDraenor   | **MistsOfPandaria**  |
| 274     | Legion              | **MistsOfPandaria**  |

(*) Versions 265-271 do not exist in any WoW release.

> **Note:** Both Cataclysm and some early MoP files use version 272.
> The default mapping assigns 272 to Cataclysm.  Use `ForceExpansion`
> (in the GUI's Settings or via `ExportSettings`) to override to MoP
> when working with a known-MoP file that reports version 272.

### 2. Extends accepted version range (M2Lib/M2.cpp)

The version-gate in `M2::Load()` changes from `[263, 274]` to `[256, 274]`,
matching the full span from Classic through MoP.

Header loading is also hardened: a `memset` + bounded `memcpy` prevents
reading past the model-chunk buffer for files whose header region is
shorter than `sizeof(CM2Header)` (a safety net for very early Classic M2s).

### 3. Adds TXID (Texture File Data ID) chunk support (M2Lib/M2Chunk.h, M2Lib/M2Chunk.cpp, M2Lib/M2.cpp)

Cata/MoP M2 files extracted from CASC archives may carry a **TXID** chunk
that maps each texture slot to a file-data-ID instead of a file path.  The
patch adds `EChunk::Texture = 'TXID'` and a `TXIDChunk` class that loads
and round-trips the chunk, preventing it from being dropped or
misinterpreted as an unknown raw chunk on save.

## M2 version reference

| M2 Version | Expansion                  | Notes                                         |
|------------|----------------------------|-----------------------------------------------|
| 256-259    | Classic (Vanilla)          | Earliest M2 format                            |
| 260-263    | The Burning Crusade        |                                                |
| 264        | Wrath of the Lich King     | Most widely modded version                    |
| 272        | Cataclysm                  | Chunked MD21 format introduced; camera FoV    |
|            |                            | moves to AnimationBlock; SFID/AFID/TXID chunks|
| 272-274    | Mists of Pandaria          | Version 272 shared with Cata; 273-274 MoP-only|

## Files modified

| File               | Changes                                                  |
|--------------------|----------------------------------------------------------|
| `M2Lib/M2.cpp`     | `GetExpansion()` mapping fix; version range `256-274`;    |
|                    | safe header copy; TXID chunk case in `Load()` switch     |
| `M2Lib/M2Chunk.h`  | `Texture = 'TXID'` added to `EChunk`; `TXIDChunk` class  |
| `M2Lib/M2Chunk.cpp`| `TXIDChunk::Load()` and `TXIDChunk::Save()` added        |

## How to apply

```bash
cd m2mod                         # root of the M2Mod repository
git apply ../m2mod-cata-mop.patch

# If offsets have shifted, try fuzzy matching:
git apply --3way ../m2mod-cata-mop.patch
# or:
patch -p1 --fuzz=3 < ../m2mod-cata-mop.patch
```

## What already worked before this patch

The M2Mod codebase already had significant Cata/MoP infrastructure:

- **Chunked format** (MD21 wrapper + sub-chunks) was fully supported
- **SFID** (skin file data IDs), **AFID** (animation file data IDs),
  **BFID** (bone file data IDs), **PFID** (physics file data IDs) chunks
  were loaded, round-tripped, and saved
- **Camera format** branching (`CElement_Camera` vs `CElement_Camera_PreCata`)
  was implemented for Cata+ camera FoV-as-AnimationBlock
- **Skin header** size was expansion-aware (48 bytes pre-Cata, full struct Cata+)
- **Long header** flag (0x08) for the extra Unknown1 element pair was handled
- The **version gate** already accepted 263-274, so Cata/MoP files could load --
  they just got the wrong expansion label, which was cosmetically wrong but
  functionally harmless since all code paths branch on `>= Cataclysm`

This patch corrects the labeling, widens the floor, and adds the missing
TXID chunk type.

## Known limitations

1. **Classic/Vanilla M2 editing (version 256-259) is untested.**  The header
   structure is the same size, but Vanilla M2 animation blocks use a
   different internal referencing scheme (array-of-arrays per animation
   sequence vs. WotLK's flat layout).  Mesh geometry editing (vertices,
   bones, textures) should work because element data is preserved as raw
   bytes, but animation offset fixup during import may produce incorrect
   results for pre-WotLK files.  Use `ForceExpansion` and test carefully.

2. **External .anim files are not loaded or edited.**  Cata/MoP models
   store some animation data in separate `.anim` files referenced by the
   AFID chunk.  M2Mod preserves these references but does not read or
   modify the `.anim` file contents.  This is fine for mesh editing but
   means animation-level changes are not supported for Cata/MoP models.

3. **TXID chunk is preserved but not updated on texture add/remove.**
   If you add or remove textures via M2I import, the TXID file-data-ID
   list is not automatically resized.  For CASC-extracted files where
   textures are referenced by data ID, you may need to manually adjust
   the TXID entries or remove the chunk.

4. **No SKID (Skeleton File Data ID) chunk support.**  Some MoP models
   reference a shared skeleton via a SKID chunk.  This patch does not add
   explicit SKID handling; such chunks are preserved as raw data (`RawChunk`)
   and round-tripped on save, but are not interpreted.

5. **Particle emitter struct size.**  The `CElement_ParticleEmitter`
   `ASSERT_SIZE` is 492 bytes (WotLK/Cata).  MoP may append additional
   fields.  Because element data is sized by offset arithmetic (not by
   `sizeof`), extra trailing bytes are preserved through load/save, but
   they are not interpreted or accessible via struct fields.

## Testing checklist

- [ ] Load a WotLK (264) M2 -- baseline regression check
- [ ] Load a Cata (272) M2 extracted from MPQ (flat MD20 format)
- [ ] Load a Cata (272) M2 extracted from CASC (chunked MD21 + SFID/AFID/TXID)
- [ ] Load a MoP (274) M2
- [ ] Export to M2I from a Cata/MoP M2
- [ ] Import M2I back into a Cata/MoP M2 and verify the saved file
- [ ] Verify TXID chunk is preserved after round-trip
- [ ] Verify SFID/AFID chunks are preserved after round-trip
- [ ] Verify camera FoV is correctly exported/imported for Cata+ cameras
- [ ] Load a Classic (256) M2 -- expect geometry to work, animations untested

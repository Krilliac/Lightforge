# AdtTools -- Vanilla Support Patch

**Target repo:** https://github.com/kelno/AdtTools
**Patch file:** `adttools-vanilla.patch`

## Summary

Extends AdtTools from TBC/WotLK (2.4.3 and 3.3.5) to also support
Vanilla (1.12.x) ADT files.  The repo README already noted "I think this
will work for 1.12 too" -- this patch makes that official by fixing the one
real compatibility bug and making two defensive improvements.

## What the patch fixes

### 1. MCCV false-read bug on Vanilla ADTs (MCNK.cpp) -- **the real fix**

The MCNK header field at offset `0x74` has different meanings by version:
- **Vanilla/TBC**: `textureId` -- may contain a non-zero integer
- **WotLK**: `ofsMCCV` -- offset to MCCV vertex-color sub-chunk

The original code reads MCCV whenever `ofsMCCV != 0`:
```cpp
if (entries[i].header.ofsMCCV)
    entries[i].mccv = std::make_unique<MCCV>(...);
```

On a Vanilla ADT where `textureId` happens to be non-zero, this seeks to a
garbage offset and tries to parse arbitrary data as an MCCV chunk, causing
undefined behavior (corrupted read, crash, or silent data corruption on
write-back).

**Fix:** Also check `FLAG_MCCV` (`flags & 0x40`) which is only set when
the chunk genuinely contains MCCV data:
```cpp
if ((entries[i].header.flags & FLAG_MCCV) && entries[i].header.ofsMCCV)
    entries[i].mccv = std::make_unique<MCCV>(...);
```

### 2. Conditional 32KB padding in WriteToDisk (adt.cpp)

The original code unconditionally seeks forward 32,768 bytes between
MFBO and MH2O when writing:
```cpp
adtFile.seekp(32768, std::ios_base::cur);
```

For Vanilla/TBC ADTs that have no MFBO, MH2O, or MTXF chunks, this
creates 32 KB of dead padding in the output file.  The WoW client reads
chunks by MHDR offsets so it still works, but the file is needlessly
bloated.

**Fix:** Only add the padding when post-MCNK chunks (MH2O or MTXF)
will actually be written.

### 3. MVER version logging (adt.cpp)

Logs the ADT version number at parse time and warns if it is not 18
(the expected value for all pre-Cataclysm ADTs).  This is advisory only
and does not reject files.

## What already worked (no changes needed)

The vast majority of AdtTools already handles Vanilla ADTs correctly:

| Mechanism | Why it works |
|-----------|-------------|
| MVER read | Reads version bytes without validating against a constant |
| MHDR offsets | WotLK-specific offset fields (offsMFBO, offsMH2O, offsMTXF) are 0 in Vanilla ADTs; the code checks `if (mhdr->offsXXX)` before reading each |
| MCNK structure | MCNKHeader is 128 bytes in all pre-Cata versions; sub-chunk offsets are 0 when the sub-chunk is absent |
| MCLQ water | Same binary format across Vanilla, TBC, and WotLK (per-MCNK liquid data with 9x9 vertex grid) |
| MDDF / MODF | Same 36-byte / 64-byte placement structures across all versions |
| WriteToDisk | Optional chunks are written only when their unique_ptr is non-null; otherwise the MHDR offset is zeroed |

## Files changed

| File | Change |
|------|--------|
| `README.md` | Version list updated from "2.4.3 and 3.3.5" to "1.12.x, 2.4.3, 3.3.5a" with format explanation |
| `core/adt.cpp` | Added MVER version logging after parse. Made 32KB padding conditional on MH2O/MTXF presence |
| `core/chunks/MCNK.cpp` | Added `FLAG_MCCV` check to MCCV read guard |
| `core/chunks/MCNK.h` | Updated `ofsMCCV` field comment documenting the Vanilla vs WotLK difference |

## Risk assessment

**Low.**
- The MCCV flag check is strictly more conservative than the original --
  it can only prevent reads, never cause new ones.  WotLK ADTs with MCCV
  data always have `FLAG_MCCV` set, so existing behavior is preserved.
- The conditional padding changes write output for files that lack MH2O
  and MTXF (Vanilla/TBC), producing smaller but correctly structured files.
  WotLK ADTs that have these chunks get the same padding as before.
- The MVER log is read-only and advisory.

## How to apply

```bash
cd AdtTools
git apply adttools-vanilla.patch
```

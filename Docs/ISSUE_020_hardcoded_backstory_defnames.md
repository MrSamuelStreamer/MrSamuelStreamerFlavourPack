# ISSUE 020: Hardcoded Backstory DefNames in Mercenary Generation

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | RimWorld Best Practices |
| **File** | `1.6/Source/MSSFP/Letters/ChoiceLetter_HireMercenaries.cs` |
| **Lines** | ~250-275 |

## Description

Mercenary pawn generation uses hardcoded backstory `defName` strings (e.g., specific childhood/adulthood backstory names). These are looked up by exact string match at runtime.

## Impact

- If RimWorld renames, removes, or reorganizes backstory defs in an update, the lookup fails silently (returns null), and the mercenary generation either crashes or produces pawns with no backstory.
- No fallback mechanism if the named backstory doesn't exist.

## Suggested Fix

1. Use `DefDatabase<BackstoryDef>.GetNamedSilentFail()` with a fallback to a random valid backstory.
2. Or define the backstory references in XML (e.g., via a custom def or mod extension) so they can be patched.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`DefDatabase<BackstoryDef>.GetNamed(...)` calls replaced with `GetNamedSilentFail(...)`. When a named backstory is not found, a `Log.Warning` is emitted and a random valid backstory is used as fallback, preventing a null-reference crash.

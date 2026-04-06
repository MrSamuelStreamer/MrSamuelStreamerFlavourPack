# ISSUE 039: HediffCompProperties_Haunt.AlwaysOn Always Returns False (struct compared to null)

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Hediffs/HediffCompProperties_Haunt.cs` |
| **Lines** | 22, 38, 43 |
| **Found during** | In-game testing, 2026-04-06 |

## Description

`IntRange` is a value type (struct). Comparing a struct to `null` in C# always evaluates to `false` — the compiler permits it but it is a logic error.

```csharp
// AlwaysOn — BEFORE (always false, struct can never be null)
public bool AlwaysOn => OnTimeTicksRange == null && OffTimeTicksRange == null;

// ConfigErrors — BEFORE (null checks never fire, validation silently skipped)
if (OnTimeTicksRange != null && OffTimeTicksRange == null) ...
if (OffTimeTicksRange != null && OnTimeTicksRange == null) ...
```

The intent is: if neither range is configured in the Def XML, the ghost should display permanently. Because `null` is unreachable for a struct, `AlwaysOn` is permanently `false` for every haunt def. `ShouldDisplayNow()` then always returns `false`, and no haunt ghost ever renders regardless of pawn selection, drafting, or mod settings.

The `ConfigErrors` checks are also dead code for the same reason — they can never fire.

## Impact

All haunt ghosts (`MSS_FP_PawnDisplayer`, named haunts) are completely invisible. `ShouldDisplayNow()` returns `false` on every call.

## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

All three comparisons changed from `== null` / `!= null` to `== default` / `!= default`, which correctly matches the zero-value `IntRange(0, 0)` that XML-unset fields receive.

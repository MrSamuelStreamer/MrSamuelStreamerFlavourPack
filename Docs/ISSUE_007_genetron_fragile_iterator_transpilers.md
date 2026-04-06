# ISSUE 007: Genetron Patch Uses 9 Fragile Iterator Transpilers

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | Harmony Patches Quality |
| **File** | `1.6/Source/MSSFP-Generator/Building_Genetron_Patch.cs` |
| **Lines** | 1-228 |

## Description

The file contains **9 nested transpiler classes**, all targeting compiler-generated iterator `MoveNext` methods via a `FindIteratorMoveNext` helper. These patch the internal state machine of `IEnumerable`/`IEnumerator` implementations in the Resource Generator mod.

Iterator state machines are **compiler implementation details** -- their structure, field names, and instruction sequences can change with any compiler update, target mod update, or even a recompile with different optimization settings.

## Impact

- **Extreme fragility**: Any update to the Resource Generator mod will likely break all 9 transpilers silently. The patched `MoveNext` methods are anonymous compiler-generated code with no stable API contract.
- **Silent failures**: If the IL pattern changes, the transpiler may fail to find its injection point and either skip the patch (no effect) or corrupt the method's IL (crash).
- **Empty catch block** at line 61 swallows exceptions during `FindIteratorMoveNext`, masking failures.

## Suggested Fix

1. Consider using Prefix/Postfix patches on the public-facing methods instead of transpiling iterator internals.
2. If transpilers are unavoidable, add robust validation that logs warnings when the expected IL pattern is not found (similar to `ResearchProjectDef_UnlockedDefs_NullSafePatch`).
3. Remove the empty catch block; log the exception.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

All 9 iterator transpilers replaced with a single `Postfix` on `Building_Genetron.Tick()`. The Postfix replicates the same running-condition guard (`PowerOn && HasFuel && !BrokenDown`) and adds 3 to `totalRunningTicks` whenever the original `Tick()` would have added 1, achieving 4× progression. This approach:

- Requires no IL inspection or iterator MoveNext discovery
- Survives any VQE update that changes constants, compiler output, or iterator structure
- Covers all 9 variants via inheritance — `Tick()` is defined once on `Building_Genetron` and not overridden in any of the patched subclasses
- `totalRunningTicks` and all three comp fields are `public` on `Building_Genetron`, so no reflection is needed

The `ARCTimersMultiplier` setting applied to thresholds in `GetGizmos` is preserved correctly: 4× faster ticks × any multiplier on the threshold produces the intended ratio regardless of player difficulty setting.

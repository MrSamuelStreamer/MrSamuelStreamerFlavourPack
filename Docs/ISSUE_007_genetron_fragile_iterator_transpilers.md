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

**Status**: ⏸ DEFERRED

A full transpiler rewrite is unsafe without live IL validation against the target mod. The empty catch block in `FindIteratorMoveNext` now logs a `Log.Warning` (see issue 019 fix), so failures will be visible in the log rather than silently swallowed. The fragility of the 9 iterator transpilers themselves remains; a future PR should replace them with Prefix/Postfix patches on the public-facing methods.

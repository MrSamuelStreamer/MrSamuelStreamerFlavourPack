# ISSUE 019: Empty Catch Blocks Swallow Exceptions

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | C# Code Quality |
| **Files** | Multiple |
| **Locations** | `MSSFP/DebugLog.cs:84`, `MSSFP-Generator/Building_Genetron_Patch.cs:61` |

## Description

Multiple files contain empty catch blocks that silently swallow exceptions:

1. **DebugLog.cs line 84**: `catch (Exception) { return string.Empty; }` -- hides any error in the debug logging utility itself, making debugging the debugger impossible.
2. **Building_Genetron_Patch.cs line 61**: Empty catch in `FindIteratorMoveNext` -- silently hides failures to locate the target method for transpiler patches, meaning broken patches produce no diagnostic output.

## Impact

- Errors are hidden, making bugs much harder to diagnose.
- In the Genetron case, a failed method lookup means the transpiler silently does nothing, and the user has no indication that the compatibility patch is inactive.

## Suggested Fix

At minimum, log the exception:

```csharp
catch (Exception ex)
{
    Log.Warning($"[MSSFP] Failed to ...: {ex.Message}");
}
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Both empty catch blocks now emit `Log.Warning`:
- `DebugLog.cs`: logs `[MSSFP] DebugLog error: {ex.Message}` before returning `string.Empty`.
- `Building_Genetron_Patch.cs` (`FindIteratorMoveNext`): logs `[MSSFP] Genetron: Failed to find iterator MoveNext for {typeName}: {ex.Message}`.

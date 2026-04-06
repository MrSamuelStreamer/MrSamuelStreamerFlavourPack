# ISSUE 014: MouseoverReadout_Patch Has [HarmonyDebug] Left On

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Harmony Patches Quality |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/MouseoverReadout_Patch.cs` |
| **Lines** | ~267 |

## Description

The `[HarmonyDebug]` attribute is present on a patch class in the release code. This attribute causes Harmony to dump detailed IL code to the debug log every time the patch is applied at startup.

## Impact

- Spams the RimWorld debug log with large IL dumps on every game start.
- Makes it harder for users and other mod developers to read the log for actual errors.
- Minor performance impact during startup.

## Suggested Fix

Remove the `[HarmonyDebug]` attribute, or wrap it in `#if DEBUG`:

```csharp
#if DEBUG
[HarmonyDebug]
#endif
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`[HarmonyDebug]` wrapped in `#if DEBUG`. Release builds no longer emit IL dumps to the log on startup.

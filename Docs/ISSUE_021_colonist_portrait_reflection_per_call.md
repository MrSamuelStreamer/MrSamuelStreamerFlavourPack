# ISSUE 021: ColonistPortraitHiding Uses Reflection on Every CheckRecacheEntries Call

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Performance |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/ColonistPortraitHiding_Patches.cs` |
| **Lines** | ~389-396 |

## Description

The patch accesses `cachedEntries` and `cachedDrawLocs` fields via reflection on every `CheckRecacheEntries` call. While the `FieldInfo` objects themselves are likely cached (via static fields), the `GetValue()` calls still involve reflection overhead on a method that can be called frequently during UI updates.

## Impact

Reflection `GetValue()` is ~10-100x slower than direct field access. On the colonist bar recache path (triggered by colonist count changes, UI resizes, etc.), this adds measurable overhead.

## Suggested Fix

Use Harmony's `AccessTools` with compiled delegates or `Traverse` with caching to eliminate per-call reflection overhead:

```csharp
private static readonly FieldRef<ColonistBar, List<...>> cachedEntriesRef = 
    AccessTools.FieldRefAccess<ColonistBar, List<...>>("cachedEntries");
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Per-call `FieldInfo.GetValue()` replaced with cached `AccessTools.FieldRefAccess<ColonistBar, T>` delegates for `cachedEntries` and `cachedDrawLocs`. Field access is now direct (delegate invocation) rather than reflective on each `CheckRecacheEntries` call.

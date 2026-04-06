# ISSUE 027: Dual Harmony Instance Creation

| Field | Value |
|---|---|
| **Severity** | LOW |
| **Category** | Mod Architecture |
| **File** | `1.6/Source/MSSFP/MSSFPMod.cs` |
| **Lines** | 32, 59 |

## Description

The Harmony instance `"MrSamuelStreamer.rimworld.MSSFP.main"` is created twice:
1. Line 32: In the constructor for `PatchAll()`.
2. Line 59: In `ToggleSettlementDefeatPatch()` for dynamic patching.

While Harmony handles multiple instances with the same ID gracefully (they share patch state), creating new instances is unnecessary and slightly confusing.

## Impact

Minimal functional impact. Minor code clarity issue -- it suggests the author may not realize Harmony instances with the same ID are interchangeable.

## Suggested Fix

Store the Harmony instance as a static field and reuse it:

```csharp
private static Harmony _harmony;

public MSSFPMod(ModContentPack content) : base(content)
{
    _harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.main");
    _harmony.PatchAll();
}
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Harmony instance extracted to `private static Harmony _harmony`. Both the constructor (`PatchAll`) and `ToggleSettlementDefeatPatch` now share the same instance.

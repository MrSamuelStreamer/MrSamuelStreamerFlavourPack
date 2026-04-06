# ISSUE 003: LoversRetreatMapComponent Inverted Tick Check

| Field | Value |
|---|---|
| **Severity** | CRITICAL |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Comps/Map/LoversRetreatMapComponent.cs` |
| **Lines** | ~896 |

## Description

The tick-gating logic is inverted:

```csharp
if (Find.TickManager.TicksGame % 600 == 0) return;
```

This **skips** processing on every 600th tick and **runs** on every other tick. The intended behavior is almost certainly the opposite: run only every 600th tick for performance, skip the rest.

## Impact

The component's main logic runs on 599 out of every 600 ticks instead of 1 out of 600. This is a ~600x performance penalty on whatever processing follows this check. On the single tick where it should run, it returns early instead.

## Suggested Fix

```csharp
if (Find.TickManager.TicksGame % 600 != 0) return;
```

Also note: the file contains a typo `ExcpectedBackTick` (should be `ExpectedBackTick`).


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Tick gate corrected from `== 0` to `!= 0`. The `ExcpectedBackTick` C# field and property name was also renamed to `ExpectedBackTick` for correctness; the Scribe XML key string `"ExcpectedBackTick"` was preserved unchanged to maintain save compatibility.

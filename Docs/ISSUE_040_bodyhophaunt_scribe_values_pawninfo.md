# ISSUE 040: HediffComp_BodyHopHaunt Uses Scribe_Values for IExposable PawnInfo

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Hediffs/HediffComp_BodyHopHaunt.cs` |
| **Line** | 100 |
| **Found during** | Code review, 2026-04-06 |

## Description

`CompExposeData` uses `Scribe_Values.Look` to serialise `pawnToShow`, which is of type `PawnInfo` — a custom class implementing `IExposable`:

```csharp
Scribe_Values.Look(ref pawnToShow, "pawnToShow");  // WRONG: PawnInfo is IExposable, not a primitive
```

`Scribe_Values` only handles primitives and enums. For a reference type implementing `IExposable`, `Scribe_Deep.Look` must be used. Using `Scribe_Values` on a complex object means `pawnToShow` is silently lost on save/load — it will be `null` after any game reload.

## Impact

After saving and reloading a game, the `pawnToShow` field on `HediffComp_BodyHopHaunt` is always `null`. The ghost for `MSS_FP_PawnDisplayerPossession` will not render after a load until the next daily tick randomises `pawnToShow` from the `pawns` list (and even then, only if `pawns` itself loads correctly).

## Suggested Fix

```csharp
Scribe_Deep.Look(ref pawnToShow, "pawnToShow");
```

## Resolution

**Status**: ⏸ DEFERRED — 2026-04-06 (`misc_fixes`)

The fix is one line but must be verified: `Scribe_Deep.Look` with a null ref will write a null entry on first save (before any pawn is selected). Confirm this round-trips cleanly before applying.

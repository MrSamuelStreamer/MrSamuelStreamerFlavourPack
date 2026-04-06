# ISSUE 038: HediffComp_Haunt Ghost Never Renders (field vs property)

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Hediffs/HediffComp_Haunt.cs` |
| **Lines** | 186, 239 |
| **Found during** | In-game testing, 2026-04-06 |

## Description

`DrawAt` passes the raw `pawnTexture` **field** (lowercase) to `gfx.SetOverrideMaterial()` instead of the `PawnTexture` **property** (uppercase). The property contains the lazy-load logic that reads the texture from disk; the field is `null` until that property is accessed.

`SetPawnToDraw` sets `texPath` (the path on disk) but never clears the `pawnTexture` field, so the lazy-load in `PawnTexture` never fires on subsequent renders either — the field retains a stale value (or stays `null` for a freshly-added hediff).

```csharp
// DrawAt — BEFORE (always passes null or stale texture)
gfx.SetOverrideMaterial(pawnTexture);

// SetPawnToDraw — BEFORE (texPath updated, pawnTexture field not cleared)
texPath = PawnGraphicUtils.SavePawnTexture(pawn);
```

Because `SetOverrideMaterial(null)` is called, `PawnHauntGraphic` has no material to render, and the ghost is never visible.

## Impact

The ghost visual for `MSS_FP_PawnDisplayer` (and `MSS_FP_PawnDisplayerPossession`) never appears even after correctly selecting a pawn via the "Select Pawn To Draw" dev gizmo.

## Note: MSS_FP_PawnDisplayer Severity Decay

`MSS_FP_PawnDisplayer` has `<severityPerDay>-2</severityPerDay>`, meaning it decays and is removed in under half a day. This is intentional — it is the temporary haunting variant. `MSS_FP_PawnDisplayerPossession` is the permanent version (`<chronic>true</chronic>`). Use `MSS_FP_PawnDisplayerPossession` to test the persistent ghost.

## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

1. `DrawAt`: changed `gfx.SetOverrideMaterial(pawnTexture)` → `gfx.SetOverrideMaterial(PawnTexture)` (use property, which lazy-loads from disk).
2. `SetPawnToDraw`: added `pawnTexture = null;` after `texPath` is set, forcing the property to reload the texture on the next render.

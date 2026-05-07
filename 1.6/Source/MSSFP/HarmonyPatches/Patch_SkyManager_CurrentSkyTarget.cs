using System;
using HarmonyLib;
using MSSFP.MapComponents;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on <see cref="Verse.SkyManager.CurrentSkyTarget"/> that clamps the map's
/// sky glow to a 0.85 minimum while at least one MSSFP_SunTower on that map is
/// active. Skipped when an Anomaly UnnaturalDarkness / DarkenedSkies condition is
/// in force (see <see cref="MSSFPSunTowerMapComponent.IsForcedDark"/>).
///
/// Runs after the vanilla aggregation (game conditions via LerpDarken, then any
/// CompAffectsSky comps), so the clamp wins last. Postfix never mutates SkyManager
/// state — it only rewrites the returned struct's glow field — making it MP-safe.
///
/// Performance: <c>CurrentSkyTarget</c> is on the per-frame Map.MapUpdate path, so
/// the field-ref delegate is cached at type-init and we do an O(1) MapComponent
/// lookup + two bool reads in the common case. NRE-safe via null guards on every
/// lookup (postfix may be invoked with a SkyManager that has no Map during world view
/// or tests).
/// </summary>
[HarmonyPatch(typeof(SkyManager), "CurrentSkyTarget")]
public static class Patch_SkyManager_CurrentSkyTarget
{
    /// <summary>Cached delegate to read SkyManager's private <c>map</c> field.
    /// Resolved once at type-init; null if vanilla renames the field across versions.
    /// We log an error in that case to surface the silent-failure mode.</summary>
    private static readonly AccessTools.FieldRef<SkyManager, Map> MapField = ResolveMapField();

    /// <summary>Minimum glow value enforced while a tower is active. Tied to the
    /// design spec — 0.85 = sun-lamp-equivalent ground glow, lifts plant growth
    /// over the 0.51 vanilla growth threshold even in winter / on dark-map mods.</summary>
    private const float MinGlow = 0.85f;

    private static AccessTools.FieldRef<SkyManager, Map> ResolveMapField()
    {
        try
        {
            return AccessTools.FieldRefAccess<SkyManager, Map>("map");
        }
        catch (Exception e)
        {
            Log.Error($"[MSSFP] Failed to resolve SkyManager.map field — sun tower glow clamp will not work. {e}");
            return null;
        }
    }

    public static void Postfix(SkyManager __instance, ref SkyTarget __result)
    {
        if (MapField == null)
        {
            return;
        }
        try
        {
            Map m = MapField(__instance);
            if (m == null)
            {
                return;
            }
            MSSFPSunTowerMapComponent c = m.GetComponent<MSSFPSunTowerMapComponent>();
            if (c == null || !c.AnyActive || c.IsForcedDark)
            {
                return;
            }
            if (__result.glow < MinGlow)
            {
                __result.glow = MinGlow;
            }
        }
        catch (Exception e)
        {
            // ErrorOnce avoids per-frame log spam while still surfacing the bug.
            Log.ErrorOnce($"[MSSFP] SkyManager postfix threw: {e}", 0x55_4E_54_57);
        }
    }
}

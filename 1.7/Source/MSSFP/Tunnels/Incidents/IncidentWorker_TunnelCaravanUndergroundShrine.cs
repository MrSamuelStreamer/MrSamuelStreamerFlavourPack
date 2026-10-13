using MSSFP.Tunnels.MapComponents;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: the caravan stops in a domed chamber holding an ancient shrine.
/// A non-combat atmospheric event — the player may approach and pray (random
/// outcome) or leave undisturbed.
///
/// Fires at most once per save game — tracked on
/// <see cref="TunnelGenData.shrineFired"/>, restoring DFP's fire-once semantic
/// that was dropped in commit 3 (parity restored in commit 4).
///
/// Map setup and letter sending are handled by IncidentWorker_TunnelCaravanNonCombat.
/// No ghost pawn required; the shrine prop and offering items are placed via
/// PostSetupEncounterMap. MapComponent_ShrineDialog fires the prayer dialog when
/// the player first views the map.
/// </summary>
public class IncidentWorker_TunnelCaravanUndergroundShrine : IncidentWorker_TunnelCaravanNonCombat
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;

        TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
        if (comp != null && comp.shrineFired) return false;

        return true;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        bool result = base.TryExecuteWorker(parms);

        if (result)
        {
            TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
            if (comp != null)
                comp.shrineFired = true;
        }

        return result;
    }

    protected override void PostSetupEncounterMap(Map map)
    {
        IntVec3 shrineCell = FindCellNearCenter(map);

        // Reliquary (Ideology DLC) → NatureShrine_Large → NatureShrine_Small.
        // GenStuff.DefaultStuffFor silences the madeFromStuff-without-stuff warning.
        ThingDef shrineDef = null;
        if (ModsConfig.IdeologyActive)
            shrineDef = ThingDef.Named("Reliquary");
        if (shrineDef == null)
            shrineDef = ThingDef.Named("NatureShrine_Large");
        if (shrineDef == null)
            shrineDef = ThingDef.Named("NatureShrine_Small");

        if (shrineDef != null)
        {
            Thing shrine = GenSpawn.Spawn(
                ThingMaker.MakeThing(shrineDef, GenStuff.DefaultStuffFor(shrineDef)),
                shrineCell, map, Rot4.South);

            if (shrine == null)
                Log.Warning("[MSSFP] UndergroundShrine: shrine prop failed to spawn at " + shrineCell + "; scene continues without it.");
        }

        // 3–5 offering items placed near the shrine.
        int offeringCount = Rand.RangeInclusive(3, 5);
        for (int i = 0; i < offeringCount; i++)
            TrySpawnOffering(shrineCell, map);

        // Dialog component fires the prayer choice when the map is viewed.
        map.GetComponent<MapComponent_ShrineDialog>().Activate();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void TrySpawnOffering(IntVec3 origin, Map map)
    {
        if (!CellFinder.TryFindRandomCellNear(origin, map, 6,
                c => c.Standable(map) && !c.Fogged(map), out IntVec3 cell))
            return;

        Thing item = MakeOffering();
        if (item != null)
            GenSpawn.Spawn(item, cell, map);
    }

    private static Thing MakeOffering()
    {
        float roll = Rand.Value;
        ThingDef def;
        int count;

        if (roll < 0.45f)
        {
            def = ThingDefOf.Silver;
            count = Rand.RangeInclusive(10, 30);
        }
        else if (roll < 0.75f)
        {
            def = ThingDefOf.Gold;
            count = Rand.RangeInclusive(3, 10);
        }
        else if (roll < 0.90f)
        {
            def = ThingDefOf.Jade;
            count = Rand.RangeInclusive(5, 15);
        }
        else
        {
            // Nutrient paste — flavour item; null-guarded.
            def = ThingDef.Named("MealNutrientPaste");
            count = Rand.RangeInclusive(1, 3);
        }

        if (def == null) return null;
        Thing item = ThingMaker.MakeThing(def);
        item.stackCount = Mathf.Min(count, def.stackLimit);
        return item;
    }
}

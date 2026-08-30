using System.Collections.Generic;
using HarmonyLib;
using ImplantSalvage;
using RimWorld;
using Verse;

namespace MSSFP.IS.HarmonyPatches;

/// <summary>
/// Turns a share of successful extractions into non-installable biocoded salvage.
///
/// DELIBERATELY NOT a patch on <c>ThingMaker.MakeThing</c>. Two reasons. Correctness:
/// MakeThing constructs via <c>Activator.CreateInstance(def.thingClass)</c>, assigns
/// <c>thing.def</c>, then <c>PostMake()</c> runs <c>InitializeComps()</c> off
/// <c>def.comps</c> — so reassigning <c>.def</c> afterwards leaves comps and HitPoints
/// built from the wrong def. Cost: MakeThing is called 50k-200k times per map generation,
/// and a Harmony stub there taxes every item spawn in the game, permanently, for a
/// per-corpse feature.
///
/// Instead we bracket Luke's own method. The prefix snapshots which matching implant items
/// already lie near the surgeon; the postfix diffs to find the one Luke just placed
/// (he uses <c>ThingPlaceMode.Near</c> at <c>surgeon.Position</c>), then destroys it and
/// places a freshly-made variant. Fresh construction means comps are correct by definition.
/// Because there is no open flag window, a foreign mod spawning the same def mid-call
/// cannot be caught by mistake.
/// </summary>
[HarmonyPatch(typeof(ImplantSalvageUtility), nameof(ImplantSalvageUtility.Extract))]
public static class Extract_Patch
{
    /// <summary>
    /// Distinct from Luke's own seed salt so our roll draws from an independent stream
    /// while staying deterministic per (corpse, implant) — save-scum-proof and MP-safe,
    /// matching the contract his destroy roll already keeps.
    /// </summary>
    private const int SeedSalt = 0x4D535346;

    private const float SearchRadius = 3f;

    public class ExtractContext
    {
        public ThingDef Product;
        public ThingDef Biocoded;
        public HashSet<int> PreExisting;
        public int CorpseId;
        public int ImplantLoadId;
    }

    [HarmonyPrefix]
    public static void Prefix(Pawn surgeon, Corpse corpse, Hediff implant, out ExtractContext __state)
    {
        __state = null;

        ThingDef product = implant?.def?.spawnThingOnRemoved;
        if (product == null || corpse == null || surgeon?.Map == null)
            return;

        if (!BiocodedImplantDefs.TryGetBiocoded(product, out ThingDef biocoded))
            return;

        HashSet<int> preExisting = new();
        Map map = surgeon.Map;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(surgeon.Position, SearchRadius, true))
        {
            if (!cell.InBounds(map))
                continue;

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == product)
                    preExisting.Add(things[i].thingIDNumber);
            }
        }

        __state = new ExtractContext
        {
            Product = product,
            Biocoded = biocoded,
            PreExisting = preExisting,
            CorpseId = corpse.thingIDNumber,
            ImplantLoadId = implant.loadID,
        };
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn surgeon, ExtractContext __state)
    {
        if (__state == null || surgeon?.Map == null)
            return;

        Thing spawned = FindNewlyPlaced(surgeon, __state);
        // Null when Luke's destroy roll wrecked the implant — nothing was placed.
        if (spawned == null)
            return;

        if (!RollBiocode(__state))
            return;

        IntVec3 pos = spawned.Position;
        Map map = spawned.Map;

        // Place the replacement BEFORE destroying the original. The reverse order opens a
        // window where a failed placement leaves the player with nothing at all — the
        // extraction succeeded, but the item silently evaporated.
        Thing replacement = ThingMaker.MakeThing(__state.Biocoded);
        if (!GenPlace.TryPlaceThing(replacement, pos, map, ThingPlaceMode.Near))
        {
            ModLog.Error(
                $"[MSSFP.IS] Could not place {__state.Biocoded.defName} at {pos}; "
                    + "leaving the un-biocoded implant in place rather than destroying it."
            );
            return;
        }

        spawned.Destroy(DestroyMode.Vanish);

        Messages.Message(
            "MSSFP_IS_BiocodedMessage".Translate(replacement.LabelCap),
            replacement,
            MessageTypeDefOf.NeutralEvent,
            false
        );
    }

    /// <summary>
    /// The thing Luke just placed, identified as the one matching implant product near the
    /// surgeon that was not there before the call.
    ///
    /// KNOWN LIMITATION: if the product def has a stack limit above 1 and a stack of the
    /// same def already sits in range, <c>TryPlaceThing</c> merges into that stack instead
    /// of creating a new Thing, and this returns null — the extraction is simply not
    /// biocoded. Vanilla body-part items are stackLimit 1 so this cannot happen for them;
    /// a modded stackable implant would under-apply the nerf, never misfire it.
    /// </summary>
    private static Thing FindNewlyPlaced(Pawn surgeon, ExtractContext state)
    {
        Map map = surgeon.Map;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(surgeon.Position, SearchRadius, true))
        {
            if (!cell.InBounds(map))
                continue;

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t.def == state.Product && !state.PreExisting.Contains(t.thingIDNumber))
                    return t;
            }
        }

        return null;
    }

    private static bool RollBiocode(ExtractContext state)
    {
        int seed = Gen.HashCombineInt(
            Gen.HashCombineInt(state.CorpseId, state.ImplantLoadId),
            SeedSalt
        );

        Rand.PushState(seed);
        try
        {
            return Rand.Chance(ImplantSalvageSettingsTab.Chance);
        }
        finally
        {
            Rand.PopState();
        }
    }
}

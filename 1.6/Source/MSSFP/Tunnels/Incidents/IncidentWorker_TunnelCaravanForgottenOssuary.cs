using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: the caravan stops in a natural chamber where ancient worshippers died
/// before whatever shrine they came to serve. A non-combat discovery event —
/// no enemies, only the scene and whatever loot the dead carried.
///
/// Map setup and letter sending are handled by IncidentWorker_TunnelCaravanNonCombat.
/// No ghost pawn required.
///
/// Fires at most once per save game — tracked on
/// <see cref="TunnelGenData.ossuaryFired"/>.
/// </summary>
public class IncidentWorker_TunnelCaravanForgottenOssuary : IncidentWorker_TunnelCaravanNonCombat
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;

        TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
        if (comp != null && comp.ossuaryFired) return false;

        return true;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        bool result = base.TryExecuteWorker(parms);

        if (result)
        {
            TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
            if (comp != null)
                comp.ossuaryFired = true;
        }

        return result;
    }

    protected override void PostSetupEncounterMap(Map map)
    {
        IntVec3 shrineCell = FindCellNearCenter(map);

        // Spawn shrine — Reliquary if Ideology is active, otherwise NatureShrine_Large.
        // NatureShrine_Large is [MayRequireRoyalty], so without either DLC there's no
        // shrine def to spawn at all; skip the shrine and just leave the corpses/loot
        // arranged around shrineCell as the anchor point.
        // Pass GenStuff.DefaultStuffFor to silence the madeFromStuff warning on Reliquary.
        ThingDef shrineDef = ModsConfig.IdeologyActive
            ? ThingDefOf.Reliquary
            : ModsConfig.RoyaltyActive
                ? ThingDefOf.NatureShrine_Large
                : null;
        if (shrineDef != null)
            GenSpawn.Spawn(ThingMaker.MakeThing(shrineDef, GenStuff.DefaultStuffFor(shrineDef)), shrineCell, map, Rot4.South);

        // Spawn 5–8 skeleton corpses in a radial arc in front of the shrine.
        int corpseCount = Rand.RangeInclusive(5, 8);
        for (int i = 0; i < corpseCount; i++)
        {
            // Spread cells in a semicircle south of the shrine (worshippers face it).
            float angle = Mathf.Lerp(-70f, 70f, corpseCount > 1 ? (float)i / (corpseCount - 1) : 0.5f);
            float dist = Rand.Range(3f, 8f);
            IntVec3 cell = shrineCell + IntVec3.FromVector3(
                new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist,
                    0f,
                    -dist)); // negative Z = south / in front

            if (!cell.InBounds(map) || !cell.Standable(map))
                CellFinder.TryFindRandomCellNear(shrineCell, map, 10,
                    c => c.Standable(map) && c != shrineCell, out cell);

            Pawn ancient = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.SpaceRefugee,
                faction: null,
                context: PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false
            ));
            GenSpawn.Spawn(ancient, cell, map, Rot4.Random);
            KillAndDecay(ancient);

            // Give loot to roughly half the corpses.
            if (ancient.Corpse != null && Rand.Bool)
                AddCorpseLoot(ancient.Corpse);
        }

        // Place 1–2 loose shrine offerings on the ground near the centre.
        int offeringCount = Rand.RangeInclusive(1, 2);
        for (int i = 0; i < offeringCount; i++)
        {
            CellFinder.TryFindRandomCellNear(shrineCell, map, 4,
                c => c.Standable(map) && !c.Fogged(map), out IntVec3 lootCell);
            GenSpawn.Spawn(RandomShrineOffering(), lootCell, map, Rot4.Random);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Kills a spawned pawn and advances its corpse to skeleton stage.</summary>
    private static void KillAndDecay(Pawn pawn)
    {
        if (!pawn.Dead)
            pawn.Kill(null);

        Corpse corpse = pawn.Corpse;
        CompRottable rot = corpse?.TryGetComp<CompRottable>();
        if (rot != null)
            rot.RotProgress = rot.PropsRot.TicksToDessicated + 1f;
    }

    private static void AddCorpseLoot(Corpse corpse)
    {
        ThingDef[] options =
        {
            ThingDefOf.Silver,
            ThingDefOf.Gold,
            ThingDefOf.ComponentIndustrial,
            ThingDefOf.Jade,
        };
        ThingDef chosen = options.RandomElement();
        Thing item = ThingMaker.MakeThing(chosen);
        item.stackCount = chosen == ThingDefOf.Silver ? Rand.RangeInclusive(15, 40)
            : chosen == ThingDefOf.Gold ? Rand.RangeInclusive(5, 15)
            : chosen == ThingDefOf.ComponentIndustrial ? Rand.RangeInclusive(1, 3)
            : Rand.RangeInclusive(10, 30); // Jade
        corpse.InnerPawn.inventory.innerContainer.TryAdd(item, canMergeWithExistingStacks: true);
    }

    private static Thing RandomShrineOffering()
    {
        float roll = Rand.Value;
        ThingDef def;
        int count;
        if (roll < 0.5f)
        {
            def = ThingDefOf.Silver;
            count = Rand.RangeInclusive(20, 60);
        }
        else if (roll < 0.85f)
        {
            def = ThingDefOf.Gold;
            count = Rand.RangeInclusive(8, 20);
        }
        else
        {
            def = ThingDefOf.ComponentSpacer;
            count = 1;
        }
        Thing item = ThingMaker.MakeThing(def);
        item.stackCount = count;
        return item;
    }
}

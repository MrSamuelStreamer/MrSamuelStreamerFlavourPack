using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MSSFP.ModExtensions;

/// <summary>
/// A pawn described declaratively in XML, so a viewer-submitted structure can ship with its
/// author's own colonists. Ported from GoldenDuckFlavourPack (GDFP.PawnRepr).
///
/// This is load-from-XML only. GDFP's capture path (FromPawn) and savegame scribing belonged to
/// its gate system, which MSSFP does not reproduce, so they are deliberately not ported.
///
/// Def references are stored as strings and resolved with GetNamedSilentFail at spawn time
/// rather than as typed Def fields. A structure's author may reference defs from mods the
/// player doesn't have installed; typed fields would turn those into load-time cross-reference
/// errors regardless of the layout's own modRequirements gate. Resolution is deferred instead,
/// and anything that fails to resolve is dropped, leaving the pawn a plain baseliner colonist.
/// </summary>
public class PawnRepr
{
    /// <summary>A single item of gear or inventory, described in XML.</summary>
    public class ThingRepr
    {
        public string def;
        public int count = 1;
        public string stuff;
        public string color;
        public QualityCategory quality;

        /// <summary>Builds the thing, or null if <see cref="def"/> doesn't resolve.</summary>
        public Thing ToThing()
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(def);
            if (thingDef == null)
                return null;

            ThingDef stuffDef = stuff.NullOrEmpty() ? null : DefDatabase<ThingDef>.GetNamedSilentFail(stuff);
            if (stuffDef == null && thingDef.MadeFromStuff)
                stuffDef = GenStuff.DefaultStuffFor(thingDef);

            Thing thing = ThingMaker.MakeThing(thingDef, stuffDef);
            thing.stackCount = count;

            if (thing.TryGetComp(out CompQuality compQuality))
            {
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            if (!color.NullOrEmpty() && thing.TryGetComp(out CompColorable compColorable))
            {
                compColorable.SetColor(ParseHelper.FromString<Color>(color));
            }

            return thing;
        }
    }

    public string kindDef;
    public Name nameInt;
    public Gender gender;
    public bool FactionLeader;
    public int age;
    public List<ThingRepr> inventory;
    public ThingRepr equipment;
    public string beardDef;
    public string faceTattoo;
    public string bodyTattoo;
    public List<string> genes;
    public string xenotype;
    public IntVec3 spawnCell;

    /// <summary>
    /// Generates and spawns this pawn. Returns false and logs rather than throwing, so one bad
    /// pawn definition cannot abort map generation.
    /// </summary>
    public bool SpawnPawn(Map map, Faction faction, Lord lord = null)
    {
        try
        {
            Pawn pawn = FactionLeader ? faction?.leader : null;

            if (pawn == null)
            {
                PawnKindDef resolvedKind = kindDef.NullOrEmpty()
                    ? null
                    : DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDef);
                resolvedKind ??= PawnKindDefOf.Colonist;

                // Gene and xenotype defs only exist when Biotech is active, so resolving them
                // otherwise would just return null anyway; the guard makes that explicit.
                List<GeneDef> resolvedGenes = null;
                XenotypeDef resolvedXenotype = null;
                if (ModsConfig.BiotechActive)
                {
                    if (!genes.NullOrEmpty())
                    {
                        resolvedGenes = genes
                            .Select(g => DefDatabase<GeneDef>.GetNamedSilentFail(g))
                            .Where(g => g != null)
                            .ToList();
                        if (resolvedGenes.Count == 0)
                            resolvedGenes = null;
                    }

                    if (!xenotype.NullOrEmpty())
                        resolvedXenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotype);
                }

                PawnGenerationRequest request = new(
                    resolvedKind,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    inhabitant: false,
                    fixedBiologicalAge: age > 0 ? age : null,
                    fixedChronologicalAge: age > 0 ? age : null,
                    fixedGender: gender,
                    forcedEndogenes: resolvedGenes,
                    forcedXenotype: resolvedXenotype,
                    dontGiveWeapon: true
                );

                pawn = PawnGenerator.GeneratePawn(request);
            }

            if (nameInt != null)
                pawn.Name = nameInt;

            if (pawn.style != null)
            {
                // Only assign when resolved, so a missing style def leaves the generated
                // default rather than blanking it out.
                BeardDef resolvedBeard = beardDef.NullOrEmpty() ? null : DefDatabase<BeardDef>.GetNamedSilentFail(beardDef);
                if (resolvedBeard != null)
                    pawn.style.beardDef = resolvedBeard;

                TattooDef resolvedFaceTattoo = faceTattoo.NullOrEmpty() ? null : DefDatabase<TattooDef>.GetNamedSilentFail(faceTattoo);
                if (resolvedFaceTattoo != null)
                    pawn.style.FaceTattoo = resolvedFaceTattoo;

                TattooDef resolvedBodyTattoo = bodyTattoo.NullOrEmpty() ? null : DefDatabase<TattooDef>.GetNamedSilentFail(bodyTattoo);
                if (resolvedBodyTattoo != null)
                    pawn.style.BodyTattoo = resolvedBodyTattoo;
            }

            lord?.AddPawn(pawn);

            if (!inventory.NullOrEmpty())
            {
                foreach (ThingRepr thingRepr in inventory)
                {
                    Thing thing = thingRepr.ToThing();
                    if (thing != null)
                        pawn.inventory?.innerContainer.TryAdd(thing);
                }
            }

            if (equipment != null && equipment.ToThing() is ThingWithComps weapon)
            {
                pawn.equipment?.AddEquipment(weapon);
            }

            IntVec3 cell = spawnCell;
            if (!cell.InBounds(map) && !RCellFinder.TryFindRandomCellNearWith(map.Center, _ => true, map, out cell))
            {
                ModLog.Warn($"PawnRepr: no valid spawn cell for {nameInt}, skipping");
                return false;
            }

            GenSpawn.Spawn(pawn, cell, map);
            return true;
        }
        catch (Exception e)
        {
            ModLog.Error($"PawnRepr: failed to spawn {nameInt}", e);
            return false;
        }
    }
}

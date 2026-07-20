using System;
using System.Collections.Generic;
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
/// </summary>
public class PawnRepr
{
    /// <summary>A single item of gear or inventory, described in XML.</summary>
    public class ThingRepr
    {
        public ThingDef def;
        public int count = 1;
        public ThingDef stuff;
        public string color;
        public QualityCategory quality;

        public Thing ToThing()
        {
            Thing thing = ThingMaker.MakeThing(def, stuff);
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

    public PawnKindDef kindDef;
    public Name nameInt;
    public Gender gender;
    public bool FactionLeader;
    public int age;
    public List<ThingRepr> inventory;
    public ThingRepr equipment;
    public BeardDef beardDef;
    public TattooDef faceTattoo;
    public TattooDef bodyTattoo;
    public List<GeneDef> genes;
    public XenotypeDef xenotype;
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
                PawnGenerationRequest request = new(
                    kindDef,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    inhabitant: false,
                    fixedBiologicalAge: age > 0 ? age : null,
                    fixedChronologicalAge: age > 0 ? age : null,
                    fixedGender: gender,
                    forcedEndogenes: genes,
                    forcedXenotype: xenotype,
                    dontGiveWeapon: true
                );

                pawn = PawnGenerator.GeneratePawn(request);
            }

            if (nameInt != null)
                pawn.Name = nameInt;

            if (pawn.style != null)
            {
                pawn.style.beardDef = beardDef;
                pawn.style.FaceTattoo = faceTattoo;
                pawn.style.BodyTattoo = bodyTattoo;
            }

            lord?.AddPawn(pawn);

            if (!inventory.NullOrEmpty())
            {
                foreach (ThingRepr thingRepr in inventory)
                {
                    pawn.inventory?.innerContainer.TryAdd(thingRepr.ToThing());
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

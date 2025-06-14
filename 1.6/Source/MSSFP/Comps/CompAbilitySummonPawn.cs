using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompAbilitySummonPawn : CompAbilityEffect_WithDest
{
    public CompAbilitySummonPawn() { }

    public CompProperties_AbilitySummonPawn AbilityProps => (CompProperties_AbilitySummonPawn)props;

    public List<IntVec3> spawnPositions = new List<IntVec3>();

    public override IEnumerable<PreCastAction> GetPreCastActions()
    {
        yield return new PreCastAction
        {
            action = delegate(LocalTargetInfo t, LocalTargetInfo d)
            {
                if (IdentifyValidSpawnPositions(d, out spawnPositions))
                {
                    foreach (IntVec3 spawnPosition in spawnPositions)
                    {
                        FleckMaker.Static(spawnPosition, parent.pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    }
                }
            },
            ticksAwayFromCast = 5,
        };
        yield break;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (spawnPositions.NullOrEmpty())
            return;
        base.Apply(target, dest);

        foreach (IntVec3 spawnPosition in spawnPositions)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(AbilityProps.pawnDef, Faction.OfPlayer);
            GenSpawn.Spawn(pawn, spawnPosition, parent.pawn.Map);
            pawn.Notify_Teleported();
            parent.AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(spawnPosition, pawn.Map, 1f), spawnPosition, 60, null);
            FloodFillerFog.FloodUnfog(pawn.Position, pawn.Map);

            if (Props.destClamorType != null)
            {
                GenClamor.DoClamor(pawn, spawnPosition, (float)Props.destClamorRadius, Props.destClamorType);
            }
        }
    }

    public virtual bool IdentifyValidSpawnPositions(LocalTargetInfo target, out List<IntVec3> positions)
    {
        positions = null;

        IEnumerable<IntVec3> validCells = GenRadial
            .RadialCellsAround(target.Cell, 5, true)
            .Where(cell => parent.pawn.Map.thingGrid.ThingsAt(cell).All(thing => thing.def.passability != Traversability.Impassable))
            .InRandomOrder()
            .ToList();

        if (validCells.Count() < AbilityProps.count)
            return false;

        positions = validCells.Take(AbilityProps.count).ToList();

        return true;
    }

    public override bool CanHitTarget(LocalTargetInfo target)
    {
        return CanPlaceSelectedTargetAt(target) && base.CanHitTarget(target) && IdentifyValidSpawnPositions(target, out List<IntVec3> _);
    }

    public override bool Valid(LocalTargetInfo target, bool showMessages = false)
    {
        return IdentifyValidSpawnPositions(target, out List<IntVec3> _) && base.Valid(target, showMessages);
    }
}

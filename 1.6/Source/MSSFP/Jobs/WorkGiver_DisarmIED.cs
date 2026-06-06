using System.Collections.Generic;
using MSSFP.Things;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

/// <summary>
/// Scans MSSFP_DisarmIED designations on the pawn's map and assigns
/// <see cref="JobDriver_DisarmIED"/> to humanlike colonists with Intellectual ≥ 4.
/// Piggybacks the Research work type — Intellectual is its skill but min skill is
/// enforced here, not by the work type itself (Research has no min-skill gate).
/// </summary>
public class WorkGiver_DisarmIED : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Designation des in pawn.Map.designationManager.SpawnedDesignationsOfDef(MSSFPDefOf.MSSFP_DisarmIEDDesignation))
        {
            if (des.target.HasThing) yield return des.target.Thing;
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!Building_IEDTrap.CanDisarm(pawn)) return true;
        return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(MSSFPDefOf.MSSFP_DisarmIEDDesignation);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!(t is Building_IEDTrap trap)) return false;
        if (trap.Faction == Faction.OfPlayer) return false;
        if (!Building_IEDTrap.CanDisarm(pawn)) return false;
        if (pawn.Map.designationManager.DesignationOn(trap, MSSFPDefOf.MSSFP_DisarmIEDDesignation) == null) return false;
        if (trap.IsForbidden(pawn)) return false;
        if (!pawn.CanReserve(trap, 1, -1, null, forced)) return false;
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!HasJobOnThing(pawn, t, forced)) return null;
        return JobMaker.MakeJob(MSSFPDefOf.MSSFP_DisarmIED, t);
    }
}

using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.AICore;

/// <summary>
/// Hauling WorkGiver: feeds buildings with a <see cref="CompTrueAICore"/> whose
/// <see cref="CompTrueAICore.WantsHaul"/> flag is set.
///
/// CONTRACT:
/// - Scans only <see cref="ThingRequestGroup.BuildingArtificial"/>; filters per-building by comp presence.
/// - Picks the closest haulable on the comp's map whose <see cref="ThingDef"/> is in
///   <see cref="CompProperties_TrueAICore.artInputs"/>.
/// - Queues vanilla <see cref="JobDriver_HaulToContainer"/> via JobDef <c>MSSFP_HaulToAICore</c>.
///   Driver finds the comp's <see cref="System.Collections.Generic.List{ThingComp}"/> via
///   <see cref="ThingOwnerUtility.TryGetInnerInteractableThingOwner(Thing)"/> — recon-confirmed
///   walks <c>ThingWithComps.AllComps</c> for <see cref="IThingHolder"/>.
///
/// V1 SIMPLIFICATIONS:
/// - No enroute tracker (we don't implement <see cref="IHaulEnroute"/>) — multi-hauler over-delivery
///   is acceptable; comp self-trims on art completion.
/// - No quality / forbidden-stuff filtering beyond <see cref="CompProperties_TrueAICore.artInputs"/>.
/// </summary>
public class WorkGiver_HaulToAICore : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        // Fast pass — bail if no AI core on the map wants hauling.
        List<Building> buildings = pawn.Map.listerBuildings.allBuildingsColonist;
        for (int i = 0; i < buildings.Count; i++)
        {
            CompTrueAICore comp = buildings[i].GetComp<CompTrueAICore>();
            if (comp != null && comp.WantsHaul) return false;
        }
        return true;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!(t is Building building)) return false;
        CompTrueAICore comp = building.GetComp<CompTrueAICore>();
        if (comp == null || !comp.WantsHaul) return false;
        if (building.IsForbidden(pawn)) return false;
        if (building.IsBurning()) return false;
        if (!pawn.CanReserve(building, 1, 1, null, forced)) return false;

        Thing input = FindInput(pawn, comp, forced);
        return input != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Building building = (Building)t;
        CompTrueAICore comp = building.GetComp<CompTrueAICore>();
        if (comp == null) return null;

        Thing input = FindInput(pawn, comp, forced);
        if (input == null) return null;

        Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_HaulToAICore, input, building);
        job.count = Mathf.Clamp(comp.RemainingNeed, 1, input.stackCount);
        return job;
    }

    private static Thing FindInput(Pawn pawn, CompTrueAICore comp, bool forced)
    {
        List<ThingDef> defs = comp.Props.artInputs;
        if (defs == null || defs.Count == 0) return null;

        bool Validator(Thing th)
        {
            if (th == null || th.Destroyed) return false;
            if (!defs.Contains(th.def)) return false;
            if (th.IsForbidden(pawn)) return false;
            if (!pawn.CanReserve(th, 1, -1, null, forced)) return false;
            return true;
        }

        return GenClosest.ClosestThingReachable(
            comp.parent.Position,
            comp.parent.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            9999f,
            Validator
        );
    }
}

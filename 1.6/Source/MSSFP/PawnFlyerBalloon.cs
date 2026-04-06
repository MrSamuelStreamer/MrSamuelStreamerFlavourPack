using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP;

public class PawnFlyerBalloon : PawnFlyer
{
    private static readonly AccessTools.FieldRef<PawnFlyer, Vector3> _effectivePosRef =
        AccessTools.FieldRefAccess<PawnFlyer, Vector3>("effectivePos");
    private Vector3 effectivePos
    {
        get => _effectivePosRef(this);
        set => _effectivePosRef(this) = value;
    }

    private static readonly AccessTools.FieldRef<PawnFlyer, Vector3> _groundPosRef =
        AccessTools.FieldRefAccess<PawnFlyer, Vector3>("groundPos");
    private Vector3 groundPos
    {
        get => _groundPosRef(this);
        set => _groundPosRef(this) = value;
    }

    private static readonly AccessTools.FieldRef<PawnFlyer, int> _positionLastComputedTickRef =
        AccessTools.FieldRefAccess<PawnFlyer, int>("positionLastComputedTick");
    private int positionLastComputedTick
    {
        get => _positionLastComputedTickRef(this);
        set => _positionLastComputedTickRef(this) = value;
    }

    private static readonly AccessTools.FieldRef<PawnFlyer, float> _effectiveHeightRef =
        AccessTools.FieldRefAccess<PawnFlyer, float>("effectiveHeight");
    private float effectiveHeight
    {
        get => _effectiveHeightRef(this);
        set => _effectiveHeightRef(this) = value;
    }

    private static readonly AccessTools.FieldRef<PawnFlyer, ThingOwner<Thing>> _innerContainerRef =
        AccessTools.FieldRefAccess<PawnFlyer, ThingOwner<Thing>>("innerContainer");
    private ThingOwner<Thing> innerContainer
    {
        get => _innerContainerRef(this);
        set => _innerContainerRef(this) = value;
    }

    private static readonly AccessTools.FieldRef<PawnFlyer, Thing> _carriedThingRef =
        AccessTools.FieldRefAccess<PawnFlyer, Thing>("carriedThing");
    private Thing carriedThing
    {
        get => _carriedThingRef(this);
        set => _carriedThingRef(this) = value;
    }

    private static readonly AccessTools.FieldRef<PawnFlyer, IntVec3> _destCellRef =
        AccessTools.FieldRefAccess<PawnFlyer, IntVec3>("destCell");
    private IntVec3 destCell
    {
        get => _destCellRef(this);
        set => _destCellRef(this) = value;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad)
            return;
        float dist = DestinationPos.z - startVec.z;
        float time = dist / 20;
        ticksFlightTime = Math.Max(10, time.SecondsToTicks());
        ticksFlying = 0;
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        drawLoc.y = AltitudeLayer.Skyfaller.AltitudeFor();
        Graphic.Draw(drawLoc, Rotation, this);
    }

    public Building_Bed bed;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref bed, "bed");
    }

    public static bool BedAvailableFor(Pawn pawn, out Building_Bed bed)
    {
        bed = null;

        if (pawn.IsColonist || pawn.IsColonySubhuman)
        {
            if (pawn.Downed)
            {
                bed = RestUtility.FindPatientBedFor(pawn);
                if (bed != null)
                {
                    return true;
                }
            }
            else
            {
                bed = pawn.CurrentBed();
                if (bed != null)
                {
                    return true;
                }
                else
                {
                    bed = RestUtility.FindBedFor(pawn);
                    if (bed != null)
                    {
                        return true;
                    }
                    else
                    {
                        Map homeMap = Find.AnyPlayerHomeMap;
                        if (homeMap != null)
                        {
                            bed = homeMap
                                .listerBuildings.allBuildingsColonist.OfType<Building_Bed>()
                                .Where(b =>
                                    b.CompAssignableToPawn.HasFreeSlot
                                    && b.CompAssignableToPawn.CanAssignTo(pawn)
                                )
                                .RandomElementWithFallback();
                        }
                        if (bed != null)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        if (!pawn.IsColonist && !pawn.IsColonySubhuman)
        {
            // find prisoner bed
            CaptureUtility.TryGetBed(null, pawn, out Thing bedThing);
            if (bedThing != null)
            {
                bed = (Building_Bed)bedThing;
                return true;
            }
            else
            {
                Map prisonerHomeMap = Find.AnyPlayerHomeMap;
                if (prisonerHomeMap != null)
                {
                    bed = prisonerHomeMap
                        .listerBuildings.allBuildingsColonist.OfType<Building_Bed>()
                        .Where(b => b.CompAssignableToPawn.HasFreeSlot && b.ForPrisoners)
                        .RandomElementWithFallback();
                }
                if (bed != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void SetBed(Building_Bed bedToSet)
    {
        bed = bedToSet;
        bed.CompAssignableToPawn.TryAssignPawn(FlyingPawn);
    }

    protected override void RespawnPawn()
    {
        Pawn flyingPawn = FlyingPawn;
        if (flyingPawn == null)
            return;

        Map respawnMap = Find.AnyPlayerHomeMap;
        if (respawnMap == null)
            return;

        CellFinder.TryFindRandomSpawnCellForPawnNear(
            respawnMap.Center,
            respawnMap,
            out IntVec3 pos
        );

        if (bed != null)
        {
            SpawnInBed(flyingPawn, out pos);
        }
        else
        {
            SpawnInPlace(pos, flyingPawn);
        }

        flyingPawn.Rotation = Rotation;

        if (
            carriedThing != null
            && innerContainer.TryDrop(
                carriedThing,
                pos,
                flyingPawn.MapHeld,
                ThingPlaceMode.Direct,
                out Thing _,
                playDropSound: false
            )
        )
        {
            carriedThing.DeSpawn(DestroyMode.Vanish);
            if (!flyingPawn.carryTracker.TryStartCarry(carriedThing))
                Log.Error(
                    $"Could not carry {carriedThing.ToStringSafe()} after respawning flyer pawn."
                );
        }
    }

    public void SpawnInBed(Pawn pawn, out IntVec3 pos)
    {
        innerContainer.TryDrop(
            pawn,
            bed.Position,
            bed.Map,
            ThingPlaceMode.Direct,
            out Thing _,
            playDropSound: false
        );

        if (pawn.Map == bed.Map && bed.Spawned && !bed.Destroyed)
        {
            try
            {
                RestUtility.TuckIntoBed(bed, pawn, pawn, false);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"Failed to tuck pawn {pawn} into bed {bed}: {ex.Message}");

                if (pawn.CanReach(bed, PathEndMode.OnCell, Danger.None))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.LayDown, bed);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }
        }

        pos = bed.Position;
    }

    public void SpawnInPlace(IntVec3 pos, Pawn p)
    {
        Map targetMap = Find.AnyPlayerHomeMap;
        if (targetMap == null)
            return;

        innerContainer.TryDrop(
            p,
            pos,
            targetMap,
            ThingPlaceMode.Direct,
            out Thing _,
            playDropSound: false
        );
    }
}

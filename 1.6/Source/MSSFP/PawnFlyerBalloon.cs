using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP;

public class PawnFlyerBalloon : PawnFlyer
{
    private static Lazy<FieldInfo> _effectivePos = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "effectivePos")
    );
    private Vector3 effectivePos
    {
        get => (Vector3)_effectivePos.Value.GetValue(this);
        set => _effectivePos.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _groundPos = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "groundPos")
    );
    private Vector3 groundPos
    {
        get => (Vector3)_groundPos.Value.GetValue(this);
        set => _groundPos.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _positionLastComputedTick = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "positionLastComputedTick")
    );
    private int positionLastComputedTick
    {
        get => (int)_positionLastComputedTick.Value.GetValue(this);
        set => _positionLastComputedTick.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _effectiveHeight = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "effectiveHeight")
    );
    private float effectiveHeight
    {
        get => (float)_effectiveHeight.Value.GetValue(this);
        set => _effectiveHeight.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _innerContainer = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "innerContainer")
    );
    private ThingOwner<Thing> innerContainer
    {
        get => (ThingOwner<Thing>)_innerContainer.Value.GetValue(this);
        set => _innerContainer.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _carriedThing = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "carriedThing")
    );
    private Thing carriedThing
    {
        get => (Thing)_carriedThing.Value.GetValue(this);
        set => _carriedThing.Value.SetValue(this, value);
    }

    private static Lazy<FieldInfo> _destCell = new(() =>
        AccessTools.Field(typeof(PawnFlyer), "destCell")
    );
    private IntVec3 destCell
    {
        get => (IntVec3)_destCell.Value.GetValue(this);
        set => _destCell.Value.SetValue(this, value);
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
                        bed = Find
                            .AnyPlayerHomeMap.listerBuildings.allBuildingsColonist.OfType<Building_Bed>()
                            .Where(b =>
                                b.CompAssignableToPawn.HasFreeSlot
                                && b.CompAssignableToPawn.CanAssignTo(pawn)
                            )
                            .RandomElementWithFallback();
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
                bed = Find
                    .AnyPlayerHomeMap.listerBuildings.allBuildingsColonist.OfType<Building_Bed>()
                    .Where(b => b.CompAssignableToPawn.HasFreeSlot && b.ForPrisoners)
                    .RandomElementWithFallback();
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

        CellFinder.TryFindRandomSpawnCellForPawnNear(
            Find.AnyPlayerHomeMap.Center,
            Find.AnyPlayerHomeMap,
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
        innerContainer.TryDrop(
            p,
            pos,
            Find.AnyPlayerHomeMap,
            ThingPlaceMode.Direct,
            out Thing _,
            playDropSound: false
        );
    }
}

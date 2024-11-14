using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Mabel;

public class CompProperties_FloorKiller : CompProperties
{
    public IntRange ticksBetweenFloorDestruction = new(30000, 300000);
    public int digFloorCommandRadius = 4;

    public CompProperties_FloorKiller() => compClass = typeof(CompFloorKiller);
}

public class CompFloorKiller : ThingComp
{
    private static int friendshipDate = 1507;
    private int _nextDestruction;

    private CompProperties_FloorKiller Props => (CompProperties_FloorKiller) props;

    private bool CanDestroy => parent is Pawn pawn && pawn.Awake() &&
                               !pawn.health.Downed &&
                               (pawn.mindState.exitMapAfterTick <= 0 ||
                                GenTicks.TicksGame < pawn.mindState.exitMapAfterTick);

    public override void Initialize(CompProperties initProps)
    {
        base.Initialize(initProps);
        _nextDestruction = GenTicks.TicksGame + Props.ticksBetweenFloorDestruction.RandomInRange;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (GenTicks.TicksGame < _nextDestruction) return;
        _nextDestruction += TryDestroyFloor() ? Props.ticksBetweenFloorDestruction.RandomInRange : friendshipDate;
    }

    public bool TryDestroyFloor()
    {
        if (!MabelMod.settings.destroyFloors) return true; // If the setting is off, skip the destruction
        if (!CanDestroy) return false;
        IntVec3 cell = parent.Position;
        Map parentMap = parent.Map;
        if (parentMap == null || !parentMap.terrainGrid.CanRemoveTopLayerAt(cell)) return false;
        parentMap.terrainGrid.RemoveTopLayer(cell);
        FilthMaker.RemoveAllFilth(cell, parentMap);
        FilthMaker.TryMakeFilth(cell.RandomAdjacentCell8Way(), parentMap, ThingDefOf.Filth_Dirt, 2, FilthSourceFlags.Terrain);
        FilthMaker.TryMakeFilth(cell.RandomAdjacentCell8Way(), parentMap, ThingDefOf.Filth_Vomit, 1, FilthSourceFlags.Pawn);
        Messages.Message("Mabel_FloorDestructionMessage".Translate(parent.LabelShort), parent, MessageTypeDefOf.NegativeEvent, historical: false);
        return true;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _nextDestruction, "nextDestruction", 60000);
        base.PostExposeData();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (DebugSettings.ShowDevGizmos)
            yield return new Command_Action
            {
                defaultLabel = "DEV: Destroy Floor Now", defaultDesc = "DEV: Force the next floor destruction tick to be now", action = () => _nextDestruction = 0
            };

        // Gizmo to give the parent a Job to dig the floor in a radius
        if (parent as Pawn is not { } pawn || pawn.Faction != Faction.OfPlayer) yield break;
        bool enabled = pawn.IsPlayerControlled || (pawn.training?.HasLearned(TrainableDefOf.Obedience) ?? false);

        yield return new Command_TargetRadius
        {
            Disabled = !enabled,
            disabledReason = enabled ? null : "Mabel_FloorDestructionDisabled".Translate(),
            defaultLabel = "Mabel_FloorDestructionCommand".Translate(),
            defaultDesc = "Mabel_FloorDestructionCommandDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Designators/RemoveFloor"),
            radius = Props.digFloorCommandRadius,
            targetingParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetBuildings = false,
                canTargetHumans = false,
                canTargetMechs = false,
                canTargetAnimals = false,
                onlyTargetColonists = false,
                validator = Validator
            },
            action = target =>
            {
                GenRadial.RadialCellsAround(target.Cell, 4, true)
                    .Where(c => Validator(new TargetInfo(c, pawn.Map)))
                    .InRandomOrder()
                    .Select(v => JobMaker.MakeJob(JobDefOf.RemoveFloor, v))
                    .Do(j => pawn.jobs.TryTakeOrderedJob(j, JobTag.TrainedAnimalBehavior, requestQueueing: true));
            }
        };
        yield break;

        bool Validator(TargetInfo info) => info.IsValid && info.Map.terrainGrid.CanRemoveTopLayerAt(info.Cell) &&
                                           !WorkGiver_ConstructRemoveFloor.AnyBuildingBlockingFloorRemoval(info.Cell, info.Map);
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompSpawnerThings : ThingComp
{
    private int ticksUntilSpawn;

    public CompProperties_SpawnerThings PropsSpawner => (CompProperties_SpawnerThings)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (respawningAfterLoad)
            return;
        ResetCountdown();
    }

    public override void CompTick() => TickInterval(1);

    public override void CompTickRare() => TickInterval(250);

    private void TickInterval(int interval)
    {
        if (!parent.Spawned)
            return;
        CompCanBeDormant comp = parent.GetComp<CompCanBeDormant>();
        if (comp != null)
        {
            if (!comp.Awake)
                return;
        }
        else if (parent.Position.Fogged(parent.Map))
            return;
        ticksUntilSpawn -= interval;
        CheckShouldSpawn();
    }

    private void CheckShouldSpawn()
    {
        if (ticksUntilSpawn > 0)
            return;
        ResetCountdown();
        TryDoSpawn();
    }

    public bool TryDoSpawn()
    {
        if (!parent.Spawned)
            return false;
        if (PropsSpawner.thingsPool.NullOrEmpty())
            return false;

        ThingDef thingToSpawn = PropsSpawner.thingsPool.RandomElement();
        int countToSpawn = PropsSpawner.spawnCountRange.RandomInRange;

        if (PropsSpawner.spawnMaxAdjacent >= 0)
        {
            int num = 0;
            for (int index1 = 0; index1 < 9; ++index1)
            {
                IntVec3 c = parent.Position + GenAdj.AdjacentCellsAndInside[index1];
                if (c.InBounds(parent.Map))
                {
                    List<Thing> thingList = c.GetThingList(parent.Map);
                    for (int index2 = 0; index2 < thingList.Count; ++index2)
                    {
                        if (thingList[index2].def == thingToSpawn)
                        {
                            num += thingList[index2].stackCount;
                            if (num >= PropsSpawner.spawnMaxAdjacent)
                                return false;
                        }
                    }
                }
            }
        }
        IntVec3 result;
        if (!CompSpawner.TryFindSpawnCell(parent, thingToSpawn, countToSpawn, out result))
            return false;
        Thing thing = ThingMaker.MakeThing(thingToSpawn);
        thing.stackCount = countToSpawn;
        if (thing == null)
            Log.Error("Could not spawn anything for " + parent);
        if (PropsSpawner.inheritFaction && thing.Faction != parent.Faction)
            thing.SetFaction(parent.Faction);
        Thing lastResultingThing;
        GenPlace.TryPlaceThing(
            thing,
            result,
            parent.Map,
            ThingPlaceMode.Direct,
            out lastResultingThing
        );
        if (PropsSpawner.spawnForbidden)
            lastResultingThing.SetForbidden(true);
        if (PropsSpawner.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
            Messages.Message(
                "MessageCompSpawnerSpawnedItem".Translate(thingToSpawn.LabelCap),
                thing,
                MessageTypeDefOf.PositiveEvent
            );
        return true;
    }

    public static bool TryFindSpawnCell(
        Thing parent,
        ThingDef thingToSpawn,
        int spawnCount,
        out IntVec3 result
    )
    {
        foreach (IntVec3 intVec3 in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
        {
            if (intVec3.Walkable(parent.Map))
            {
                Building edifice = intVec3.GetEdifice(parent.Map);
                if (
                    (edifice == null || !thingToSpawn.IsEdifice())
                    && (!(edifice is Building_Door buildingDoor) || buildingDoor.FreePassage)
                    && (
                        parent.def.passability == Traversability.Impassable
                        || GenSight.LineOfSight(parent.Position, intVec3, parent.Map)
                    )
                )
                {
                    bool flag = false;
                    List<Thing> thingList = intVec3.GetThingList(parent.Map);
                    for (int index = 0; index < thingList.Count; ++index)
                    {
                        Thing thing = thingList[index];
                        if (
                            thing.def.category == ThingCategory.Item
                            && (
                                thing.def != thingToSpawn
                                || thing.stackCount > thingToSpawn.stackLimit - spawnCount
                            )
                        )
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        result = intVec3;
                        return true;
                    }
                }
            }
        }
        result = IntVec3.Invalid;
        return false;
    }

    private void ResetCountdown()
    {
        ticksUntilSpawn = PropsSpawner.spawnIntervalRange.RandomInRange;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(
            ref ticksUntilSpawn,
            (PropsSpawner.saveKeysPrefix.NullOrEmpty() ? null : PropsSpawner.saveKeysPrefix + "_")
                + "ticksUntilSpawn"
        );
    }

    public override string CompInspectStringExtra()
    {
        return PropsSpawner.writeTimeLeftToSpawn
            ? "MSS_FP_CompSpawnThings".Translate()
                + ": "
                + ticksUntilSpawn.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor)
            : null;
    }
}

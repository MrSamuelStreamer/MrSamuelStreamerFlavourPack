using System.Collections.Generic;
using System.Linq;
using MSSFP.ModExtensions;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

public class ThingSpawnerWorldComp(RimWorld.Planet.World world) : WorldComponent(world)
{
    public List<ThingDef> SpawnedThings;

    public Dictionary<ThingDef, int> SpawnThingsAtTick = new();

    public override void FinalizeInit(bool fromLoad)
    {
        if(fromLoad) return;
        ModLog.Debug("Initializing ThingSpawnerWorldComp");
        SpawnedThings ??= [];
        foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(t=>t.HasModExtension<AutoSpawningModExtension>()))
        {
            if (!SpawnedThings.Contains(thingDef))
            {
                AutoSpawningModExtension ext = thingDef.GetModExtension<AutoSpawningModExtension>();
                if(ext == null) continue;

                int tickToSpawn = ext.notBeforeTick + ext.randomFuzzRange.RandomInRange;

                SpawnThingsAtTick.Add(thingDef, tickToSpawn);
                ModLog.Debug($"Added {thingDef.defName} to spawn list at tick {tickToSpawn}");
            }
        }
    }

    public override void WorldComponentTick()
    {
        int tick = Find.TickManager.TicksAbs;

        foreach (ThingDef thing in SpawnThingsAtTick.Keys.ToList())
        {
            if (tick > SpawnThingsAtTick[thing])
            {
                Verse.Map playerMap = Find.RandomPlayerHomeMap;
                AutoSpawningModExtension ext = thing.GetModExtension<AutoSpawningModExtension>();

                ModLog.Debug($"Attempting to spawn {thing.defName} on map {playerMap}");
                Thing spawnedThing = ThingMaker.MakeThing(thing);
                if (spawnedThing == null)
                {
                    ModLog.Error($"Could not spawn {thing.defName} on map {playerMap}, removing from spawn list");
                    SpawnThingsAtTick.Remove(thing);
                    continue;
                }
                GenPlace.TryPlaceThing(spawnedThing, playerMap.Center, playerMap, ThingPlaceMode.Near);
                SpawnedThings.Add(thing);
                SpawnThingsAtTick.Remove(thing);

                if (ext is { spawnedLetterType: not null })
                {
                    Find.LetterStack.ReceiveLetter(
                        ext.spawnedLetterLabel.Formatted(),
                        ext.spawnedLetterText.Formatted(),
                        ext.spawnedLetterType,
                        new LookTargets([spawnedThing])
                    );
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref SpawnedThings, "SpawnedThings", LookMode.Def);

        SpawnedThings ??= [];
    }


}

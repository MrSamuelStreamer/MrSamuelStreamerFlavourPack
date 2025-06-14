using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_SpawnerThings : CompProperties
{
    public List<ThingDef> thingsPool;
    public IntRange spawnCountRange = new IntRange(1, 1);
    public IntRange spawnIntervalRange = new IntRange(100, 100);
    public int spawnMaxAdjacent = -1;
    public bool spawnForbidden;
    public bool requiresPower;
    public bool writeTimeLeftToSpawn;
    public bool showMessageIfOwned;
    public string saveKeysPrefix;
    public bool inheritFaction;

    public CompProperties_SpawnerThings()
    {
        compClass = typeof(CompSpawnerThings);
    }
}

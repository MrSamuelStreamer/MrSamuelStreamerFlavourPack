using System;
using System.Collections.Generic;
using System.Xml;
using RimWorld;
using Verse;

namespace MSSFP.ModExtensions;

public class OnUseDefModExtension : DefModExtension
{
    public class ThingDefChance
    {
        public ThingDef thingDef;
        public float chance;

        public ThingDefChance() { }
        public ThingDefChance(ThingDef thingDef, float chance)
        {
            this.thingDef = thingDef;
            this.chance = chance;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
            chance = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }

    public List<ThingDefChance> spawnThingOnUseWithChance;

    public float? chanceToSpawnOnUse;

    public bool TrySpawnThing(Pawn pawn, out Thing thing)
    {
        if (!Rand.Chance(chanceToSpawnOnUse ?? 1f))
        {
            thing = null;
            return false;
        }

        try
        {
            ThingDef thingToSpawn = spawnThingOnUseWithChance.RandomElementByWeight(e => e.chance).thingDef;
            if (!thingToSpawn.MadeFromStuff)
            {
                return GenSpawn.TrySpawn(thingToSpawn, pawn.Position, pawn.Map, out thing);
            }

            ThingDef stuffDef = GenStuff.RandomStuffByCommonalityFor(thingToSpawn);
            thing = ThingMaker.MakeThing(thingToSpawn, stuffDef);
            GenSpawn.Spawn(thing, pawn.Position, pawn.Map);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to spawn thing on use: {e}");
            thing = null;
            return false;
        }
    }
}

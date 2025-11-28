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
    public class HediffDefChance
    {
        public HediffDef hediffDef;
        public float chance;

        public HediffDefChance() { }
        public HediffDefChance(HediffDef hediffDef, float chance)
        {
            this.hediffDef = hediffDef;
            this.chance = chance;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "hediffDef", xmlRoot.Name);
            chance = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }

    public List<ThingDefChance> spawnThingOnUseWithChance;
    public float? chanceToSpawnOnUse;

    public List<HediffDefChance> spawnHediffOnUseWithChance;
    public float? chanceToSpawnHediffOnUse;
    public float? hediffSeverityOnUse;

    public bool TrySpawnThing(Pawn pawn, out Thing thing)
    {
        if (!Rand.Chance(chanceToSpawnOnUse ?? 0f))
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


    public bool TryGiveHediff(Pawn pawn, out Hediff hediff)
    {
        if (!Rand.Chance(chanceToSpawnHediffOnUse ?? 0f))
        {
            hediff = null;
            return false;
        }

        try
        {
            HediffDef hediffToGive = spawnHediffOnUseWithChance.RandomElementByWeight(e => e.chance).hediffDef;

            if (pawn.health.hediffSet.TryGetHediff(hediffToGive, out hediff))
            {
                return true;
            }

            hediff = pawn.health.AddHediff(hediffToGive);
            hediff.Severity = hediffSeverityOnUse ?? 1f;

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to give hediff on use: {e}");
            hediff = null;
            return false;
        }
    }
}

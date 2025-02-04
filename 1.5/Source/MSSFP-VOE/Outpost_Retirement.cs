using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Outposts;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.VOE;

public class Outpost_Retirement : Outpost
{
    public Lazy<FieldInfo> ticksTillProductionInfo = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(Outpost), "ticksTillProduction"));
    public OutpostDefModExtension modExt => def.GetModExtension<OutpostDefModExtension>();

    public override int TicksPerProduction => Mathf.Max(base.TicksPerProduction - PawnCount * 600000, 600000);

    public int CombinedAge => CapablePawns.Sum(p => p.ageTracker.AgeBiologicalYears);

    protected override bool IsCapable(Pawn pawn)
    {
        return pawn.RaceProps.Humanlike && pawn.ageTracker.AgeBiologicalYears > 60;
    }

    public override string RelevantSkillDisplay()
    {
        return "MSS_FP_AgeInspector".Translate(CombinedAge);
    }

    public override void Tick()
    {
        int ticksTillProd = (int) ticksTillProductionInfo.Value.GetValue(this);

        if (!CapablePawns.Any() && ticksTillProd >= 0)
        {
            ticksTillProductionInfo.Value.SetValue(this, -1);
            Find.LetterStack.ReceiveLetter("MSS_FP_RetirementNoOldPeople".Translate(Name), "MSS_FP_RetirementNoOldPeopleDesc".Translate(Name), LetterDefOf.NegativeEvent);
        }else if (CapablePawns.Any() && ticksTillProd < 0)
        {
            ticksTillProductionInfo.Value.SetValue(this, Mathf.RoundToInt(TicksPerProduction * OutpostsMod.Settings.TimeMultiplier));
        }
        base.Tick();
    }

    public int CombinedAgeMultiplier => Mathf.Max(1, CombinedAge / 250);

    public static QualityCategory GetRandomQualityCategory()
    {
        return Enum.GetValues(typeof(QualityCategory)) // get values from Type provided
            .OfType<QualityCategory>() // casts to Enum
            .RandomElement();
    }

    public override IEnumerable<Thing> ProducedThings()
    {
        List<Thing> things = [];

        int stacksToMake = modExt.numberOfStacks.RandomInRange;
        int totalToMake = modExt.numberOfItems.RandomInRange * CombinedAgeMultiplier;

        List<int> stackSizes = Utils.GenerateRandomParts(totalToMake, stacksToMake);

        for (int i = 0; i < stacksToMake; i++)
        {
            OutpostDefModExtension.ThingDefWithWeight td = modExt.thingDefs.RandomElementByWeight(td => td.weight);
            ThingDef stuff = td.stuff;
            if (stuff == null && !td.stuffCategories.NullOrEmpty())
            {
                ThingCategoryDef tcd = td.stuffCategories.RandomElement();
                stuff = tcd.childThingDefs.RandomElement();
            }

            Thing t = ThingMaker.MakeThing(td.thingDef, stuff);

            t.stackCount = Mathf.Max(1, (td.stackLimit > 0 ? td.stackLimit : stackSizes[i]));
            CompArt compArt = t.TryGetComp<CompArt>();
            CompQuality compQuality = t.TryGetComp<CompQuality>();


            compArt?.JustCreatedBy(CapablePawns.RandomElement());
            compQuality?.SetQuality(GetRandomQualityCategory(), ArtGenerationContext.Colony);

            things.Add(td.minified ? t.MakeMinified() : t);
        }

        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
        silver.stackCount = PawnCount;

        things.Add(silver);

        return things;
    }

    public override string GetInspectString()
    {

        string[] strArray = [base.GetInspectString(), null];

        if (CapablePawns.Any())
        {
            strArray[1] = "\n" + "MSS_FP_RetirementWillProduce".Translate(TimeTillProduction).RawText;
        }
        else
        {
            strArray[1] = "\n" + "MSS_FP_RetirementNoOldPeople".Translate(Name).RawText;
        }

        return string.Concat(strArray);
    }
}

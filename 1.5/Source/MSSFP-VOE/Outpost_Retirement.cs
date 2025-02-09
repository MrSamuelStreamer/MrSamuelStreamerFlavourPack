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

    public bool NoOldPeopleLetterSent = false;
    public override int TicksPerProduction => Mathf.Max(base.TicksPerProduction - PawnCount * 600000, 600000);

    public int CombinedAge => CapablePawns.Sum(p => p.ageTracker.AgeBiologicalYears);

    protected override bool IsCapable(Pawn pawn)
    {
        return pawn.RaceProps.Humanlike && pawn.ageTracker.AgeBiologicalYears >= 60;
    }

    public virtual bool IsCaretaker(Pawn pawn)
    {
        return pawn.RaceProps.Humanlike && pawn.ageTracker.AgeBiologicalYears < 60;
    }

    public virtual IEnumerable<Pawn> Caretakers()
    {
        return AllPawns.Where(IsCaretaker);
    }

    public int TickBeforeNextBodyReturned = 0;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref NoOldPeopleLetterSent, "NoOldPeopleLetterSent", false);
        Scribe_Values.Look(ref TickBeforeNextBodyReturned, "TickBeforeNextBodyReturned", 0);
    }

    public override string RelevantSkillDisplay()
    {
        return "MSS_FP_AgeInspector".Translate(CombinedAge);
    }

    public override void Tick()
    {
        if (Find.TickManager.TicksGame % 60 != 0)
        {
            base.Tick();
        }
        else
        {
            int ticksTillProd = (int) ticksTillProductionInfo.Value.GetValue(this);

            bool AnyCapablePawns = CapablePawns.Any();

            if (AnyCapablePawns) NoOldPeopleLetterSent = false;

            if (!AnyCapablePawns && ticksTillProd >= 0)
            {
                // offset base tick increase, instead of patching
                if (TicksPerProduction > 0)
                {
                    ticksTillProductionInfo.Value.SetValue(this, ticksTillProd + 60);
                }

                if (!NoOldPeopleLetterSent)
                {
                    NoOldPeopleLetterSent = true;
                    Find.LetterStack.ReceiveLetter("MSS_FP_RetirementNoOldPeople".Translate(Name), "MSS_FP_RetirementNoOldPeopleDesc".Translate(Name), LetterDefOf.NegativeEvent);
                }
            }
            else if (AnyCapablePawns && ticksTillProd < 0)
            {
                ticksTillProductionInfo.Value.SetValue(this, Mathf.RoundToInt(TicksPerProduction * OutpostsMod.Settings.TimeMultiplier * 60));
            }
        }

        if (Find.TickManager.TicksGame % GenDate.TicksPerHour == 0 && Things.Any(p=>p is Corpse) && Find.TickManager.TicksGame > TickBeforeNextBodyReturned)
        {
            Corpse deadPawn = Things.OfType<Corpse>().RandomElement();
            if (deadPawn != null)
            {
                TakeItem(deadPawn);
                if (!DeliverDeadPawn(deadPawn))
                {
                    AddItem(deadPawn);
                }
                else
                {
                    TickBeforeNextBodyReturned = Find.TickManager.TicksGame + GenDate.TicksPerHour * 12;
                }
            }
        }

        base.Tick();
    }

    public static Lazy<MethodInfo> Deliver_PackAnimalInfo = new Lazy<MethodInfo>(()=>AccessTools.Method(typeof(Outpost_Retirement), "Deliver_PackAnimal"));

    public bool DeliverDeadPawn(Corpse pawn)
    {
      Map map = deliveryMap ?? Find.Maps.Where(m => m.IsPlayerHome).OrderBy(m => Find.WorldGrid.ApproxDistanceInTiles(m.Parent.Tile, Tile)).FirstOrDefault();
      if (map == null)
      {
        Log.Warning("Vanilla Outpost Expanded Tried to deliver to a null map, storing instead");
        return false;
      }
      else
      {
        TaggedString text = "MSSFP_DeadPawnFromOutpost".Translate(pawn.InnerPawn.NameFullColored,  Name) + "\n";
        List<Thing> lookAt = new();
        Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(map.Parent.Tile, Tile);
        List<Thing> list = [pawn];

        Deliver_PackAnimalInfo.Value.Invoke(this, [list, map, rotFromTo, lookAt]);

        Find.LetterStack.ReceiveLetter("MSSFP_DeadPawnFromOutpostLabel".Translate(pawn.InnerPawn.NameShortColored), text, LetterDefOf.NeutralEvent, new LookTargets(lookAt));
        return true;
      }
    }

    public static QualityCategory GetRandomQualityCategory()
    {
        return Enum.GetValues(typeof(QualityCategory)) // get values from Type provided
            .OfType<QualityCategory>() // casts to Enum
            .RandomElement();
    }

    public override void Produce()
    {
        if(!CapablePawns.Any()) return;
        base.Produce();
    }

    public virtual float ValueToProduce => modExt.valuePerYearOld.RandomInRange * CombinedAge;

    public override IEnumerable<Thing> ProducedThings()
    {
        int careTakerCount = Caretakers().Count();
        int oldPeopleCount = CapablePawns.Count();

        List<Thing> things = [];

        float valueToProduce = ValueToProduce;

        float chanceToBumpQuality = Mathf.Max(0.05f, careTakerCount / (oldPeopleCount / 2f));

        while (valueToProduce > 0)
        {
            OutpostDefModExtension.ThingDefWithWeight td = modExt.thingDefs.RandomElementByWeight(td => td.weight);
            ThingDef stuff = td.stuff;
            if (stuff == null && !td.stuffCategories.NullOrEmpty())
            {
                ThingCategoryDef tcd = td.stuffCategories.RandomElement();
                stuff = tcd.childThingDefs.RandomElement();
            }

            Thing t = ThingMaker.MakeThing(td.thingDef, stuff);
            CompArt compArt = t.TryGetComp<CompArt>();
            CompQuality compQuality = t.TryGetComp<CompQuality>();

            compArt?.JustCreatedBy(CapablePawns.RandomElement());
            compQuality?.SetQuality(GetRandomQualityCategory(), ArtGenerationContext.Colony);

            if (compQuality != null && Rand.Chance(chanceToBumpQuality))
            {
                if (compQuality.Quality < QualityCategory.Legendary)
                {
                    compQuality.SetQuality(compQuality.Quality + 1, ArtGenerationContext.Colony);
                }
            }

            things.Add(td.minified ? t.MakeMinified() : t);

            valueToProduce -= t.MarketValue;
        }

        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
        silver.stackCount = oldPeopleCount;

        things.Add(silver);

        return things;
    }

    public override string GetInspectString()
    {

        string[] strArray = [base.GetInspectString(), null];

        if (CapablePawns.Any())
        {
            strArray[1] = "\n" + "MSS_FP_RetirementWillProduce".Translate(modExt.valuePerYearOld.min * CombinedAge, modExt.valuePerYearOld.max * CombinedAge, TimeTillProduction).RawText;
        }
        else
        {
            strArray[1] = "\n" + "MSS_FP_RetirementNoOldPeople".Translate(Name).RawText;
        }

        return string.Concat(strArray);
    }
}

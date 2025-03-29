using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompUpgradableBed : ThingComp
{
    public static HashSet<StatDef> StatDefs = new();
    public Building_Bed Bed => parent as Building_Bed;
    public float Experience = 0;
    public int Levels = 0;

    public float HediffMultiplier = 1;

    public List<Pawn> PawnsLovedInThisBed = new();
    public List<Pawn> PawnsConcievedInThisBed = new();
    public List<Pawn> PawnsSleptInThisBed = new();
    public Dictionary<Pawn, List<Pawn>> RegisteredPregnancies = new();

    public static HashSet<CompUpgradableBed> AllBeds = new();

    public static int PointsPerLevel = 25;

    public Dictionary<StatDef, float> StatMultipliers = new();
    public Dictionary<StatDef, float> StatOffsets = new();

    public virtual void AddExperience(float val = 1, string reasonString = null)
    {
        Experience += val;
        while (Experience >= PointsPerLevel)
        {
            Experience -= PointsPerLevel;
            Levels++;
            Messages.Message(
                reasonString != null ? "MSSFP_BedLevelUpWithReason".Translate(parent.LabelCap, reasonString) : "MSSFP_BedLevelUp".Translate(parent.LabelCap),
                new LookTargets([Bed]),
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        AllBeds.Add(this);
    }

    public static CompUpgradableBed CompForPregnancy(Hediff_Pregnant hediff)
    {
        return AllBeds.FirstOrDefault(c => c.RegisteredPregnancies.ContainsKey(hediff.pawn));
    }

    public static CompUpgradableBed CompForSleepingPawn(Pawn pawn)
    {
        return AllBeds.FirstOrDefault(comp => comp.Bed.CurOccupants.Contains(pawn));
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref Experience, "points", 0);
        Scribe_Values.Look(ref HediffMultiplier, "HediffMultiplier", 1);
        Scribe_Values.Look(ref Levels, "Levels", 0);
        Scribe_Collections.Look(ref PawnsLovedInThisBed, "PawnsLovedInThisBed", LookMode.Reference);
        Scribe_Collections.Look(ref PawnsConcievedInThisBed, "PawnsConcievedInThisBed", LookMode.Reference);
        Scribe_Collections.Look(ref RegisteredPregnancies, "RegisteredPregnancies", LookMode.Reference, LookMode.Reference);
        Scribe_Collections.Look(ref StatMultipliers, "StatMultipliers", LookMode.Def, LookMode.Value);
        Scribe_Collections.Look(ref StatOffsets, "StatOffsets", LookMode.Def, LookMode.Value);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            PawnsLovedInThisBed.RemoveAll(p => p == null);
            PawnsConcievedInThisBed.RemoveAll(p => p == null);

            StatDefs =
            [
                DefDatabase<StatDef>.GetNamed("Fertility"),
                DefDatabase<StatDef>.GetNamed("MoveSpeed"),
                DefDatabase<StatDef>.GetNamed("GlobalLearningFactor"),
                DefDatabase<StatDef>.GetNamed("InjuryHealingFactor"),
                DefDatabase<StatDef>.GetNamed("PainShockThreshold"),
                DefDatabase<StatDef>.GetNamed("JoyFallRateFactor"),
                DefDatabase<StatDef>.GetNamed("NegotiationAbility"),
                DefDatabase<StatDef>.GetNamed("PawnBeauty"),
                DefDatabase<StatDef>.GetNamed("SocialImpact"),
                DefDatabase<StatDef>.GetNamed("TameAnimalChance"),
                DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"),
                DefDatabase<StatDef>.GetNamed("MiningSpeed"),
                DefDatabase<StatDef>.GetNamed("DeepDrillingSpeed"),
                DefDatabase<StatDef>.GetNamed("ResearchSpeed"),
                DefDatabase<StatDef>.GetNamed("PlantWorkSpeed"),
                DefDatabase<StatDef>.GetNamed("ConstructionSpeed"),
            ];
        }
    }

    public virtual void Notify_GotSomeLovin(List<Pawn> pawns)
    {
        PawnsLovedInThisBed.AddRange(pawns.Except(PawnsLovedInThisBed));
        StringBuilder pawnNames = new();
        foreach (Pawn pawn in pawns)
        {
            pawnNames.Append(pawn.LabelShort);
            pawnNames.Append(", ");
        }

        string names = pawnNames.ToString().TrimEnd(", ".ToCharArray());
        AddExperience(pawns.Count, "MSSFP_BedLevelUpBecauseLovin".Translate(names));
    }

    public virtual void Notify_PawnBorn(Pawn pawn)
    {
        if (!PawnsConcievedInThisBed.Contains(pawn))
            PawnsConcievedInThisBed.Add(pawn);
        AddExperience(10, "MSSFP_BedLevelUpBecauseConcieved".Translate(pawn.LabelShort));
    }

    public override void CompTickLong()
    {
        foreach (Pawn occupant in Bed.CurOccupants)
        {
            AddExperience(0.025f, "MSSFP_BedLevelUpBecauseSlept".Translate(occupant.LabelShort));
            Hediff hediff = occupant.health.GetOrAddHediff(MSSFPDefOf.MSS_FP_WellSlept);
            hediff.Severity += (0.025f * HediffMultiplier);
            HediffCompBedUpgrade hcomp = hediff.TryGetComp<HediffCompBedUpgrade>();
            if (hcomp == null)
                continue;
            hcomp.hediffGiver = Bed;
        }
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder stringBuilder = new(base.CompInspectStringExtra());
        stringBuilder.AppendLine("MSSFP_BedExp".Translate(Experience, PointsPerLevel));
        stringBuilder.AppendLine("MSSFP_BedLevels".Translate(Levels));

        if (!PawnsSleptInThisBed.NullOrEmpty())
        {
            stringBuilder.AppendLine("MSSFP_PawnsSleptInBed".Translate());
            foreach (Pawn pawn in PawnsSleptInThisBed)
            {
                stringBuilder.AppendLine(" - " + pawn.LabelShort);
            }
        }

        if (!PawnsLovedInThisBed.NullOrEmpty())
        {
            stringBuilder.AppendLine("MSSFP_PawnsLovedInBed".Translate());
            foreach (Pawn pawn in PawnsLovedInThisBed)
            {
                stringBuilder.AppendLine(" - " + pawn.LabelShort);
            }
        }

        if (!PawnsConcievedInThisBed.NullOrEmpty())
        {
            stringBuilder.AppendLine("MSSFP_PawnsConcievedInBed".Translate());
            foreach (Pawn pawn in PawnsConcievedInThisBed)
            {
                stringBuilder.AppendLine(" - " + pawn.LabelShort);
            }
        }

        return stringBuilder.ToString();
    }
}

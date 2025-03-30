using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class PregnancyInfo : IExposable
{
    public Pawn Mother;
    public List<Pawn> Others;

    public PregnancyInfo() { }

    public PregnancyInfo(Pawn mother, List<Pawn> others)
    {
        Mother = mother;
        Others = others;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Mother, "Mother");
        Scribe_Collections.Look(ref Others, "Others", LookMode.Reference);
    }
}

public class CompUpgradableBed : ThingComp
{
    public enum Direction : byte
    {
        Up,
        Down,
    }

    public List<BedUpgradeDef> AppliedOneshotUpgrades = new();

    public static IEnumerable<BedUpgradeDef> BedUpgradesAvailable => DefDatabase<BedUpgradeDef>.AllDefs;
    public Building_Bed Bed => parent as Building_Bed;
    public float Experience = 0;
    public int Levels = 0;

    public float HediffMultiplier = 1;

    public List<Pawn> PawnsLovedInThisBed = new();
    public List<Pawn> PawnsConcievedInThisBed = new();
    public List<Pawn> PawnsSleptInThisBed = new();
    public List<PregnancyInfo> RegisteredPregnancies = new();

    public static HashSet<CompUpgradableBed> AllBeds = new();

    public static int PointsPerLevel = 25;

    public Dictionary<StatDef, float> StatMultipliers = new();
    public Dictionary<StatDef, float> StatOffsets = new();

    public string NewName = null;

    public virtual void Reset(bool all = false)
    {
        StatMultipliers.Clear();
        StatOffsets.Clear();
        AppliedOneshotUpgrades.Clear();

        Experience = 0;
        Levels = 0;
        HediffMultiplier = 1;

        if (all)
        {
            PawnsLovedInThisBed.Clear();
            PawnsConcievedInThisBed.Clear();
            PawnsSleptInThisBed.Clear();
            RegisteredPregnancies.Clear();
        }
    }

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
        return AllBeds.FirstOrDefault(c => c.RegisteredPregnancies.Any(p => p.Mother == hediff.pawn));
    }

    public List<Pawn> ParentsForPregnancy(Hediff_Pregnant hediff)
    {
        return RegisteredPregnancies.FirstOrDefault(p => p.Mother == hediff.pawn).Others;
    }

    public List<Pawn> ParentsForPregnancy(Pawn mother)
    {
        return RegisteredPregnancies.FirstOrDefault(p => p.Mother == mother)?.Others ?? [];
    }

    public void RemovePregnancy(Hediff_Pregnant hediff)
    {
        RegisteredPregnancies.RemoveAll(p => p.Mother == hediff.pawn);
    }

    public void AddPregnancy(Pawn mother, List<Pawn> others)
    {
        RegisteredPregnancies.Add(new PregnancyInfo(mother, others));
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
        Scribe_Values.Look(ref NewName, "NewName", null);
        Scribe_Collections.Look(ref PawnsLovedInThisBed, "PawnsLovedInThisBed", LookMode.Reference);
        Scribe_Collections.Look(ref PawnsConcievedInThisBed, "PawnsConcievedInThisBed", LookMode.Reference);
        Scribe_Collections.Look(ref RegisteredPregnancies, "RegisteredPregnancies", LookMode.Deep);
        Scribe_Collections.Look(ref StatMultipliers, "StatMultipliers", LookMode.Def, LookMode.Value);
        Scribe_Collections.Look(ref StatOffsets, "StatOffsets", LookMode.Def, LookMode.Value);
        Scribe_Collections.Look(ref PawnsSleptInThisBed, "PawnsSleptInThisBed", LookMode.Reference);
        Scribe_Collections.Look(ref AppliedOneshotUpgrades, "AppliedOneshotUpgrades", LookMode.Def);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            PawnsLovedInThisBed.RemoveAll(p => p == null);
            PawnsConcievedInThisBed.RemoveAll(p => p == null);
        }

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (StatMultipliers.NullOrEmpty())
                StatMultipliers = new Dictionary<StatDef, float>();
            if (StatOffsets.NullOrEmpty())
                StatOffsets = new Dictionary<StatDef, float>();
            if (RegisteredPregnancies.NullOrEmpty())
                RegisteredPregnancies = new List<PregnancyInfo>();
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

    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(GenDate.TicksPerHour))
            return;

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

        // force recalculation
        AddExperience(0);
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder stringBuilder = new(base.CompInspectStringExtra());
        stringBuilder.AppendLine("MSSFP_BedExp".Translate(Experience.ToStringDecimalIfSmall(), PointsPerLevel));
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

        return stringBuilder.ToString().TrimEnd();
    }

    public string GetStatString(BedUpgradeDef def)
    {
        if (def.stat == null)
            return def.LabelCap;
        StringBuilder sb = new();

        if (!StatMultipliers.TryGetValue(def.stat, out float mult))
        {
            mult = 1;
        }

        if (!StatOffsets.TryGetValue(def.stat, out float offset))
        {
            offset = 0;
        }

        sb.AppendLine(def.stat.LabelCap);
        sb.Append("x");
        sb.Append(mult.ToStringPercent());
        sb.Append(" | ");
        sb.Append(" +");
        sb.Append(offset.ToStringPercent());

        return sb.ToString().TrimEnd();
    }

    public virtual string NewLabel()
    {
        if (!NewName.NullOrEmpty())
            return NewName;
        return GenLabel.ThingLabel(parent, 1);
    }

    public override string TransformLabel(string label)
    {
        if (!NewName.NullOrEmpty())
            label = NewName;

        return Levels < 1 ? label : $"{label} [Lvl {Levels}]";
    }

    public override string GetDescriptionPart() => CompInspectStringExtra();
}

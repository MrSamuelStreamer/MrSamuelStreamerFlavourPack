using System;
using System.Reflection;
using AchievementsExpanded;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.Achievements;

public class SweetBabyBoyTracker: TrackerBase
{
    public override MethodInfo MethodHook => AccessTools.Method(typeof(Pawn_RelationsTracker), "AddDirectRelation");
    public override MethodInfo PatchMethod => AccessTools.Method(typeof(SweetBabyBoyTracker), "AddDirectRelation_Patch");
    public override PatchType PatchType => PatchType.Postfix;

    public static Lazy<FieldInfo> GetPawn = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn"));

    public Pawn SweetBabyBoy;
    public Pawn Victim;

    public SweetBabyBoyTracker()
    {
    }


    public SweetBabyBoyTracker(SweetBabyBoyTracker reference) : base(reference)
    {
    }

    public static void AddDirectRelation_Patch(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
    {
        if(def != PawnRelationDefOf.Lover || def != PawnRelationDefOf.Fiance || def != PawnRelationDefOf.Spouse) return;
        Pawn parent = GetPawn.Value.GetValue(__instance) as Pawn;

        if(parent == null) return;
        if(otherPawn == null) return;

        Pawn eldest;
        Pawn youngest;

        if (parent.ageTracker.AgeBiologicalYears > otherPawn.ageTracker.AgeBiologicalYears)
        {
            eldest = parent;
            youngest = otherPawn;
        }
        else
        {
            eldest = otherPawn;
            youngest = parent;
        }

        if ((eldest.ageTracker.AgeBiologicalYears / 2) + 7 > youngest.ageTracker.AgeBiologicalYears)
        {
            // Sweet baby boy time
            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (AchievementCard card in AchievementPointManager.GetCards<SweetBabyBoyTracker>())
                {
                    try
                    {
                        if(card.tracker is SweetBabyBoyTracker sbb && sbb.Trigger()){
                            card.UnlockCard();

                            sbb.SweetBabyBoy = eldest;
                            sbb.Victim = youngest;

                            eldest.story.traits.GainTrait(new Trait(MSSFPDefOf.MSS_SweetBabyBoy, 1, true));
                        }
                    }

                    catch (Exception ex)
                    {
                        Log.Error($"Unable to trigger event for card validation. To avoid further errors {card.def.LabelCap} has been automatically unlocked.\n\nException={ex.Message}");
                        card.UnlockCard();
                    }
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref SweetBabyBoy, "SweetBabyBoy");
        Scribe_References.Look(ref Victim, "Victim");
    }

    public override string Key
    {
        get { return "MSS_SweetBabyBoyTracker"; }
        set { }
    }

    protected override string[] DebugText => [$"Sweet Baby Boy: {SweetBabyBoy?.Name}, Victim: {Victim?.Name}]"];

    public override bool Trigger()
    {
        base.Trigger();

        return true;
    }
}

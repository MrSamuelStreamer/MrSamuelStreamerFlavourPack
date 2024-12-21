using System;
using System.Reflection;
using AchievementsExpanded;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.VAE.Achievements;

public class SweetBabyBoyTracker: TrackerBase
{
    public override MethodInfo MethodHook => AccessTools.Method(typeof(Pawn_RelationsTracker), "AddDirectRelation");
    public override MethodInfo PatchMethod => AccessTools.Method(typeof(SweetBabyBoyTracker), "AddDirectRelation_Patch");
    public override PatchType PatchType => PatchType.Postfix;

    public static Lazy<FieldInfo> GetPawn = new(()=>AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn"));

    public Pawn SweetBabyBoy;
    public Pawn Victim;

    public bool HasTriggered = false;

    public SweetBabyBoyTracker()
    {
    }


    public SweetBabyBoyTracker(SweetBabyBoyTracker reference) : base(reference)
    {
    }

    public static void CheckPawnRelations(Pawn pawnA)
    {
        foreach (DirectPawnRelation directPawnRelation in pawnA.GetLoveRelations(false))
        {
            Pawn eldest;
            Pawn youngest;

            if (pawnA.ageTracker.AgeBiologicalYears > directPawnRelation.otherPawn.ageTracker.AgeBiologicalYears)
            {
                eldest = pawnA;
                youngest = directPawnRelation.otherPawn;
            }
            else
            {
                eldest = directPawnRelation.otherPawn;
                youngest = pawnA;
            }

            if ((eldest.ageTracker.AgeBiologicalYears / 2) + 7 <= youngest.ageTracker.AgeBiologicalYears)
            {
                continue;
            }

            // Sweet baby boy time
            if (Current.ProgramState != ProgramState.Playing)
            {
                continue;
            }

            foreach (AchievementCard card in AchievementPointManager.GetCards<SweetBabyBoyTracker>())
            {
                try
                {
                    if (card.tracker is not SweetBabyBoyTracker sbb || !sbb.Trigger() || sbb.HasTriggered)
                    {
                        continue;
                    }

                    sbb.HasTriggered = true;
                    card.UnlockCard();

                    sbb.SweetBabyBoy = eldest;
                    sbb.Victim = youngest;

                    // eldest.story.traits.GainTrait(new Trait(MSSFPDefOf.MSS_SweetBabyBoy, 0, true));
                }

                catch (Exception ex)
                {
                    Log.Error($"Unable to trigger event for card validation. To avoid further errors {card.def.LabelCap} has been automatically unlocked.\n\nException={ex.Message}");
                    card.UnlockCard();
                }
            }
        }
    }

    public static void AddDirectRelation_Patch(Pawn_RelationsTracker __instance)
    {
        if(GetPawn.Value.GetValue(__instance) is not Pawn parent) return;
        CheckPawnRelations(parent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref SweetBabyBoy, "SweetBabyBoy");
        Scribe_References.Look(ref Victim, "Victim");
        Scribe_Values.Look(ref HasTriggered, "HasTriggered", false);
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

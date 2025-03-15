using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Rituals;

public class RitualOutcomeEffectWorker_WitchTrial : RitualOutcomeEffectWorker_FromQuality
  {
    public const int ConvictGuiltyForDays = 15;

    public override bool SupportsAttachableOutcomeEffect => false;

    public RitualOutcomeEffectWorker_WitchTrial()
    {
    }

    public RitualOutcomeEffectWorker_WitchTrial(RitualOutcomeEffectDef def)
      : base(def)
    {
    }

    public override void Apply(
      float progress,
      Dictionary<Pawn, int> totalPresence,
      LordJob_Ritual jobRitual)
    {
      float quality = GetQuality(jobRitual, progress);
      RitualOutcomePossibility outcome = GetOutcome(quality, jobRitual);

      Pawn judge = jobRitual.PawnWithRole("leader");
      Pawn accused = jobRitual.PawnWithRole("convict");

      if (outcome.positivityIndex == 2)
      {
          // swap for the flipped accusations
          judge = jobRitual.PawnWithRole("convict");
          accused = jobRitual.PawnWithRole("leader");
      }

      LookTargets letterLookTargets = accused;

      string extraLetterText = null;
      if (jobRitual.Ritual != null)
        ApplyAttachableOutcome(totalPresence, jobRitual, outcome, out extraLetterText, ref letterLookTargets);

      string label = accused.LabelShort + " " + outcome.label;
      TaggedString taggedString = outcome.description.Formatted(accused.Named("PAWN"), judge.Named("PROSECUTOR"));
      string str = def.OutcomeMoodBreakdown(outcome);
      if (!str.NullOrEmpty())
        taggedString += "\n\n" + str;
      TaggedString text = taggedString + ("\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual));
      if (extraLetterText != null)
        text += "\n\n" + extraLetterText;

      if (outcome.Positive)
      {
        Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter((TaggedString) label, text, LetterDefOf.RitualOutcomePositive, letterLookTargets));
        accused.guilt.Notify_Guilty(900000);
        accused.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.TrialConvicted);
      }
      else
      {
        Find.LetterStack.ReceiveLetter((TaggedString) label, text, LetterDefOf.RitualOutcomeNegative, letterLookTargets);
        judge.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.TrialFailed);
        accused.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.TrialExonerated);
      }
    }

    public override RitualOutcomePossibility GetOutcome(float quality, LordJob_Ritual ritual)
    {
      return !Rand.Chance(quality) ? def.outcomeChances[0] : def.outcomeChances[1];
    }

    public override string ExpectedQualityLabel()
    {
      return "ExpectedConvictionChance".Translate();
    }

}

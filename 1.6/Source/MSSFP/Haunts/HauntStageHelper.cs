using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Haunts;

/// <summary>
/// Shared severity-to-stage mapping for the haunt system.
/// Stages: 0 = Whisper (0–0.33), 1 = Presence (0.34–0.66), 2 = Awakened (0.67+).
/// </summary>
public static class HauntStageHelper
{
    public const float WhisperMax = 0.33f;
    public const float PresenceMax = 0.66f;

    public static int GetStage(float severity) =>
        severity <= WhisperMax ? 0
        : severity <= PresenceMax ? 1
        : 2;

    /// <summary>
    /// Sends a player-visible message when a haunt crosses a stage threshold.
    /// Called by both HediffComp_DynamicHaunt and HediffComp_HauntProgression.
    /// </summary>
    public static void NotifyStageAdvanced(HediffWithComps hediff, int newStage)
    {
        Pawn pawn = hediff.pawn;
        HediffComp_Haunt hauntComp = hediff.TryGetComp<HediffComp_Haunt>();
        string spiritName = hauntComp?.PawnName ?? hauntComp?.pawnToDraw?.LabelShort ?? "a spirit";

        string key = newStage switch
        {
            1 => "MSS_FP_Haunt_StagePresence_Msg",
            2 => "MSS_FP_Haunt_StageAwakened_Msg",
            _ => null,
        };

        if (key != null)
        {
            Messages.Message(
                key.Translate(pawn.LabelShort, spiritName),
                pawn,
                newStage == 2 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.SilentInput,
                historical: false
            );
        }
    }
}

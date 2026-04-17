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
}

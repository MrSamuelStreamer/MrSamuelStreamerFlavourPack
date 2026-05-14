using MSSFP.Comps;
using MSSFP.Holo;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.AICore;

/// <summary>
/// Output channel helpers for <see cref="CompTrueAICore"/> chatter / events.
///
/// Three rungs of escalation:
///   <see cref="EmitChatter"/>  — bubble only, ambient flavour
///   <see cref="EmitMessage"/>  — top-of-screen toast + bubble; non-historical, non-blocking
///   <see cref="EmitLetter"/>   — Letter (caller must throttle: per-core daily cap, suppress on StoryDanger)
///
/// BUBBLE ANCHOR: when a holo projection is live, the bubble anchors to the projected pawn so
/// it visually reads as the holo speaking. Falls back to the projector building when no
/// projection is active (unpowered, recalled, persona without holo). Toast LookTargets follow
/// the same anchor so camera-focus clicks land on the speaker.
///
/// FORBIDDEN ROUTING: do NOT pipe these through <see cref="Find.PlayLog"/>.<c>Add</c> or any
/// <c>LogEntry</c> subclass. The vanilla social log is Pawn-typed; routing a building host
/// corrupts the social log iteration paths AND subverts our own renderer — bubbles MUST flow
/// through <see cref="AICoreBubbler.Add"/> only.
/// </summary>
public static class AICoreSpeech
{
    /// <summary>Bubble color when personality has none. Cool grey, distinct from vanilla.</summary>
    private static readonly Color FallbackColor = new Color(0.78f, 0.84f, 0.92f);

    private static Color ColorFor(CompTrueAICore comp)
    {
        if (comp?.activePersonality == null) return FallbackColor;
        Color c = comp.activePersonality.textColor;
        // textColor scribed as 0-255 ints in XML; if any channel >1 treat whole color as 0-255 and normalise.
        if (c.r > 1f || c.g > 1f || c.b > 1f)
            return new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f);
        return c;
    }

    /// <summary>
    /// Resolve the bubble/toast anchor for this AI core. Prefers the projected holo pawn when
    /// the projector has a live projection on a map; falls back to the projector building.
    /// Null-safe — returns null only when both the comp and its parent are null/destroyed,
    /// which callers already gate on.
    /// </summary>
    private static Thing AnchorFor(CompTrueAICore comp)
    {
        if (comp?.parent == null) return null;
        Pawn projected = comp.parent.TryGetComp<CompHoloProjector>()?.projected;
        if (projected != null && projected.Spawned && !projected.Destroyed)
            return projected;
        return comp.parent;
    }

    /// <summary>
    /// Lowest rung. Bubble only. No toast, no letter, no log. Use for ambient chatter that should
    /// flavour a colony without nagging the player.
    /// </summary>
    public static void EmitChatter(CompTrueAICore comp, string line)
    {
        if (comp?.parent == null || string.IsNullOrEmpty(line)) return;
        Thing anchor = AnchorFor(comp);
        if (anchor == null) return;
        AICoreBubbler.Add(anchor, line, ColorFor(comp));
    }

    /// <summary>
    /// Mid rung. Bubble + non-historical neutral toast. Use when the chatter should briefly grab
    /// attention but not interrupt (e.g. art-completion blurb).
    /// </summary>
    public static void EmitMessage(CompTrueAICore comp, string line)
    {
        if (comp?.parent == null || string.IsNullOrEmpty(line)) return;
        Thing anchor = AnchorFor(comp);
        if (anchor == null) return;
        AICoreBubbler.Add(anchor, line, ColorFor(comp));
        Messages.Message(line, new LookTargets(anchor), MessageTypeDefOf.NeutralEvent, historical: false);
    }

    /// <summary>
    /// High rung. Bubble + Letter via <see cref="LetterStack.ReceiveLetter(Letter, string)"/>.
    /// Caller MUST throttle (per-core daily cap, suppress during <see cref="StoryDanger.High"/>+).
    /// <paramref name="letterDef"/> picks the Letter category (NeutralEvent / ThreatSmall / etc).
    /// </summary>
    public static void EmitLetter(CompTrueAICore comp, string label, string body, LetterDef letterDef)
    {
        if (comp?.parent == null || string.IsNullOrEmpty(body)) return;
        if (letterDef == null) letterDef = LetterDefOf.NeutralEvent;
        Thing anchor = AnchorFor(comp);
        if (anchor == null) return;

        // Bubble carries a teaser line — re-use label so the player sees something above the anchor too.
        AICoreBubbler.Add(anchor, label ?? body, ColorFor(comp));

        Letter letter = LetterMaker.MakeLetter(
            label ?? string.Empty,
            body,
            letterDef,
            new LookTargets(anchor)
        );
        Find.LetterStack.ReceiveLetter(letter);
    }
}

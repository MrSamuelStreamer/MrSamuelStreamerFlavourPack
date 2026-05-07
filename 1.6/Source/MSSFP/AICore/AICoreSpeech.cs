using MSSFP.Comps;
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
/// FORBIDDEN ROUTING: do NOT pipe these through <see cref="Find.PlayLog"/>.<c>Add</c> or any
/// <c>LogEntry</c> subclass. The vanilla social log is Pawn-typed; an AI core is a <see cref="Building"/>.
/// Routing a non-Pawn host through PlayLog corrupts the social log iteration paths AND subverts our
/// own renderer — bubbles MUST flow through <see cref="AICoreBubbler.Add"/> only.
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
    /// Lowest rung. Bubble only. No toast, no letter, no log. Use for ambient chatter that should
    /// flavour a colony without nagging the player.
    /// </summary>
    public static void EmitChatter(CompTrueAICore comp, string line)
    {
        if (comp?.parent == null || string.IsNullOrEmpty(line)) return;
        AICoreBubbler.Add(comp.parent, line, ColorFor(comp));
    }

    /// <summary>
    /// Mid rung. Bubble + non-historical neutral toast. Use when the chatter should briefly grab
    /// attention but not interrupt (e.g. art-completion blurb).
    /// </summary>
    public static void EmitMessage(CompTrueAICore comp, string line)
    {
        if (comp?.parent == null || string.IsNullOrEmpty(line)) return;
        AICoreBubbler.Add(comp.parent, line, ColorFor(comp));
        Messages.Message(line, new LookTargets(comp.parent), MessageTypeDefOf.NeutralEvent, historical: false);
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

        // Bubble carries a teaser line — re-use label so the player sees something above the building too.
        AICoreBubbler.Add(comp.parent, label ?? body, ColorFor(comp));

        Letter letter = LetterMaker.MakeLetter(
            label ?? string.Empty,
            body,
            letterDef,
            new LookTargets(comp.parent)
        );
        Find.LetterStack.ReceiveLetter(letter);
    }
}

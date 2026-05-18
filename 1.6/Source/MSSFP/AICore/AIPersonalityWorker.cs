using MSSFP.Comps;
using MSSFP.Defs;
using Verse;
using Verse.Grammar;

namespace MSSFP.AICore;

/// <summary>
/// Base worker for an <see cref="AIPersonalityDef"/>.
///
/// INVARIANT: Workers are STATELESS singletons. One instance per def is shared across
/// every <see cref="CompTrueAICore"/> on the map. Per-core state MUST live on the
/// comp itself (see <c>CompTrueAICore.personalityScratch</c>) — never in fields here.
///
/// Subclasses override hooks to add personality-specific behaviour beyond the
/// XML-driven RulePack output. Default implementation just consults the def's
/// rule packs and returns the resolved string.
/// </summary>
public class AIPersonalityWorker
{
    /// <summary>Back-reference set by <see cref="AIPersonalityDef.Worker"/>.</summary>
    public AIPersonalityDef def;

    /// <summary>
    /// Called when the comp's chatter MTB rolls a hit. Return a line (already resolved)
    /// or null/empty to skip this tick. Default: resolves <see cref="AIPersonalityDef.ambientChatter"/>.
    /// </summary>
    public virtual string RollChatter(CompTrueAICore core)
    {
        if (def?.ambientChatter == null) return null;
        GrammarRequest req = new GrammarRequest();
        req.Includes.Add(def.ambientChatter);
        return GrammarResolver.Resolve("r_chatter", req);
    }

    /// <summary>
    /// Called when the comp addresses a specific colonist (e.g. ambient chatter aimed at
    /// a nearby pawn). Default: resolves <see cref="AIPersonalityDef.pawnAddress"/>.
    /// </summary>
    public virtual string RollPawnAddress(CompTrueAICore core, Pawn target)
    {
        if (def?.pawnAddress == null) return null;
        GrammarRequest req = new GrammarRequest();
        req.Includes.Add(def.pawnAddress);
        if (target != null)
            req.Rules.AddRange(GrammarUtility.RulesForPawn("PAWN", target, req.Constants));
        return GrammarResolver.Resolve("r_address", req);
    }

    /// <summary>
    /// Called from <see cref="CompTrueAICore.TryCompleteArt"/> after the sculpture is spawned.
    /// Hook for personality-specific side effects (extra letters, relations, etc).
    /// Default: no-op.
    /// </summary>
    public virtual void OnArtCompleted(CompTrueAICore core, Thing sculpture) { }

    /// <summary>
    /// Personality-driven scheduled emit hook. Called from <see cref="CompTrueAICore.CompTickRare"/>
    /// after the spawn-announce retry has succeeded, and BEFORE the MTB-driven ambient chatter
    /// path. Returns <c>true</c> if the worker consumed this tick (e.g. emitted a scheduled
    /// red-letter alarm); the host comp then short-circuits and skips ambient chatter for the
    /// same tick to avoid double-talk.
    ///
    /// Default: no-op, returns false. Subclasses override to implement personality-specific
    /// scheduling (e.g. Mr Beans' 3am coffee bulletins). Per the stateless-singleton invariant,
    /// throttle bookkeeping MUST live in <see cref="CompTrueAICore.personalityScratch"/>, not
    /// in fields on the worker.
    ///
    /// Hosting comp guarantees on call: <c>core.parent.Spawned</c>, faction == player,
    /// power on (or powerless variant), <c>activePersonality</c> non-null, and
    /// <c>spawnAnnounced</c> true. Map may still be null on edge cases (pocket maps,
    /// caravan-only state); subclasses must re-validate when they touch
    /// <see cref="GenLocalDate"/> or <c>parent.Map</c>.
    /// </summary>
    public virtual bool TickScheduled(CompTrueAICore core) => false;

    /// <summary>
    /// Resolves <see cref="AIPersonalityDef.scheduledChatter"/> under the <c>r_alert</c>
    /// keyword. Helper for <see cref="TickScheduled"/> implementations that emit lines
    /// from the def's dedicated scheduled-alert RulePack. Returns null when the def has
    /// no <c>scheduledChatter</c> pack.
    /// </summary>
    public virtual string RollScheduled(CompTrueAICore core)
    {
        if (def?.scheduledChatter == null) return null;
        GrammarRequest req = new GrammarRequest();
        req.Includes.Add(def.scheduledChatter);
        return GrammarResolver.Resolve("r_alert", req);
    }

    /// <summary>
    /// Returns the sculpture title to override <c>CompArt.Title</c> with.
    /// Default: resolves <see cref="AIPersonalityDef.artTitles"/>.
    /// </summary>
    public virtual string GenerateTitle(CompTrueAICore core)
    {
        if (def?.artTitles == null) return null;
        GrammarRequest req = new GrammarRequest();
        req.Includes.Add(def.artTitles);
        return GrammarResolver.Resolve("r_title", req);
    }

    /// <summary>
    /// Returns the sculpture description to override the art description with.
    /// Default: resolves <see cref="AIPersonalityDef.artDescriptions"/>.
    /// </summary>
    public virtual string GenerateDescription(CompTrueAICore core)
    {
        if (def?.artDescriptions == null) return null;
        GrammarRequest req = new GrammarRequest();
        req.Includes.Add(def.artDescriptions);
        return GrammarResolver.Resolve("r_desc", req);
    }

    /// <summary>
    /// Called by <c>PersonaSocialSwap_Patch</c> when this persona initiates a
    /// whitelisted social interaction (Chitchat, DeepTalk, Slight, Insult,
    /// KindWords) and the per-entry roll selects persona text.
    ///
    /// Resolves <see cref="AIPersonalityDef.socialInitiator"/> with both
    /// INITIATOR_* and RECIPIENT_* pawn rules in scope. Returns null when
    /// the def has no <c>socialInitiator</c> pack, signalling the patch to
    /// fall through to vanilla resolution.
    ///
    /// The caller is responsible for <see cref="Verse.Rand.PushState"/> /
    /// <see cref="Verse.Rand.PopState"/> around this call — the postfix
    /// re-seeds with the log entry's id so the same interaction always
    /// resolves to the same line.
    /// </summary>
    public virtual string RollSocialLine(CompTrueAICore core, Pawn initiator, Pawn recipient)
    {
        if (def?.socialInitiator == null) return null;
        GrammarRequest req = new GrammarRequest();
        req.Includes.Add(def.socialInitiator);
        if (initiator != null)
            req.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, req.Constants));
        if (recipient != null)
            req.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, req.Constants));
        return GrammarResolver.Resolve("r_social", req);
    }
}

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
}

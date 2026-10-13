using System.Linq;
using MSSFP.Comps;
using MSSFP.HarmonyPatches;
using RimWorld;
using Verse;

namespace MSSFP.StatParts;

/// <summary>
/// StatPart attached to <see cref="StatDefOf.GravshipRange"/> via
/// <c>Patches/MSSFP_GravshipRangeStatPart.xml</c>. Adds a flat tile bonus to the gravship's
/// max launch range when a powered, persona-loaded Pondering Orb sits on the engine's
/// substructure.
///
/// All work is delegated to <see cref="OrbGravshipAssist.TryGetActiveAssistOrb"/>, which is
/// the single source of truth for "orb is on this ship and active" and which provides a
/// tick-stamped cache so this getter stays cheap inside the world tile-picker's per-frame
/// stat read.
/// </summary>
public class StatPart_PonderingOrbAstroNav : StatPart
{
    public float bonusTiles = 5f;

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (!TryResolveEngine(req, out Building_GravEngine engine))
            return;
        if (OrbGravshipAssist.TryGetActiveAssistOrb(engine, out _))
            val += bonusTiles;
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (!TryResolveEngine(req, out Building_GravEngine engine))
            return null;
        if (!OrbGravshipAssist.TryGetActiveAssistOrb(engine, out CompTrueAICore orb))
            return null;
        return "MSSFP_PonderingOrbAstroNav_StatExplain".Translate(
            orb.activePersonality.LabelCap,
            bonusTiles.ToString("F0"));
    }

    private static bool TryResolveEngine(StatRequest req, out Building_GravEngine engine)
    {
        engine = null;
        if (!req.HasThing)
            return false;
        if (req.Thing is not Building_GravEngine candidate)
            return false;
        if (!candidate.Spawned || candidate.Map == null)
            return false;
        engine = candidate;
        return true;
    }
}

using Verse;

namespace MSSFP.Comps;

/// <summary>
/// Marker comp props for sculptures spawned by an AI core. The comp itself stores a
/// personality-flavoured description (resolved at art-completion time) which a Harmony
/// postfix on <see cref="RimWorld.CompArt.GenerateImageDescription"/> reads to override
/// the vanilla taleRef-driven text.
///
/// Vanilla CompArt has no per-instance description field — descriptions are generated
/// on demand from <c>taleRef.GenerateText(Props.descriptionMaker)</c>. This sidecar comp
/// is the cheapest way to inject one without subclassing CompArt or mutating shared
/// CompProperties_Art.descriptionMaker.
/// </summary>
public class CompProperties_TrueAICoreArt : CompProperties
{
    public CompProperties_TrueAICoreArt()
    {
        compClass = typeof(CompTrueAICoreArt);
    }
}

using Verse;

namespace MSSFP.Comps;

/// <summary>
/// Sidecar art-description holder attached to sculptures spawned by an AI core.
///
/// CONTRACT:
/// - <see cref="flavouredDescription"/> is set ONCE by <c>CompTrueAICore.TryCompleteArt</c> right after
///   <c>CompArt.InitializeArt</c>. Never mutated again.
/// - Read by the Harmony postfix on <see cref="RimWorld.CompArt.GenerateImageDescription"/>.
/// - Scribed verbatim — the resolved sentence is preserved across save/load. (Re-resolving from a
///   RulePackDef on load would risk drift if the def's grammar files change.)
/// - Empty/null = no override; vanilla taleRef path runs.
/// </summary>
public class CompTrueAICoreArt : ThingComp
{
    /// <summary>Personality-resolved description string. Null/empty = no override.</summary>
    public string flavouredDescription;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref flavouredDescription, "flavouredDescription", null);
    }
}

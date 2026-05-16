using Verse;

namespace MSSFP.PlaceWorkers;

/// <summary>
/// PlaceWorker that hides its host ThingDef's auto-generated <see cref="RimWorld.Designator_Build"/>
/// from the Architect menu while leaving every other build pipeline behaviour intact.
///
/// WHY THIS EXISTS (not just <c>canGenerateDefaultDesignator=false</c>):
///   vanilla <see cref="RimWorld.Ideo.MembersCanBuild"/> in 1.6 treats
///   <c>canGenerateDefaultDesignator == false</c> as "this is an ideology-restricted
///   buildable", so a colonist with any non-classic Ideo will refuse to construct the
///   def's frame with <c>OnlyMembersCanBuild</c>. That breaks redeploy of The Pondering
///   Orb via its loaded-core variant.
///
///   <see cref="RimWorld.Designator_Build.Visible"/> iterates PlaceWorkers and short-circuits
///   to <c>false</c> as soon as any returns <c>false</c> from IsBuildDesignatorVisible.
///   So we can keep <c>canGenerateDefaultDesignator=true</c> (ideo gate passes) while
///   still preventing the auto-generated designator from rendering in the menu — the
///   designator instance exists but is hidden.
///
/// Custom designators (like <see cref="MSSFP.Dialogs.Designator_DeployPonderingOrb"/>)
/// activated via Find.DesignatorManager.Select(...) bypass the Visible gate and still
/// work normally.
/// </summary>
public class PlaceWorker_HideFromArchitect : PlaceWorker
{
    public override bool IsBuildDesignatorVisible(BuildableDef def) => false;
}

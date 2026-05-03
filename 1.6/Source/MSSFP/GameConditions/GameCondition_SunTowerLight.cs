using RimWorld;

namespace MSSFP.GameConditions;

/// <summary>
/// UI-marker condition for the Sun Tower. The actual sky brightening is driven by
/// <see cref="MSSFP.Comps.CompAffectsSky_Sustained"/> on each tower (vanilla CompAffectsSky
/// runs AFTER GameCondition aggregation in <c>SkyManager.CurrentSkyTarget</c>, so it
/// can brighten via <c>SkyTarget.Lerp</c> — GameConditions can only darken via
/// <c>SkyTarget.LerpDarken</c>).
///
/// This condition exists purely so the player sees a marker in the conditions panel
/// while a tower is active. It overrides nothing.
/// </summary>
public class GameCondition_SunTowerLight : GameCondition
{
}

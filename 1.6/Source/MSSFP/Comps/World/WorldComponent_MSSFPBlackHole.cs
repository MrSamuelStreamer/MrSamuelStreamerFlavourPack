using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

/// <summary>
/// Keeps the <see cref="GameCondition_BlackHole"/> world condition in sync with
/// <see cref="Settings.BlackHoleEnabled"/>. The condition is permanent (no expiry)
/// and toggleable at any time — including mid-game — via the settings tab calling
/// <see cref="SyncActiveStateWithSetting"/>.
/// </summary>
public class WorldComponent_MSSFPBlackHole : WorldComponent
{
    public WorldComponent_MSSFPBlackHole(RimWorld.Planet.World world) : base(world)
    {
    }

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        SyncActiveStateWithSetting();
    }

    /// <summary>
    /// Registers or ends the permanent black hole condition on the current world so it
    /// matches <see cref="Settings.BlackHoleEnabled"/>. Safe to call with no world
    /// loaded — it's a no-op then.
    /// </summary>
    internal static void SyncActiveStateWithSetting()
    {
        RimWorld.Planet.World world = Find.World;
        if (world?.GameConditionManager == null) return;

        bool shouldBeActive = MSSFPMod.settings != null && MSSFPMod.settings.BlackHoleEnabled;
        GameCondition active = world.GameConditionManager.GetActiveCondition(MSSFPDefOf.MSSFP_BlackHole);

        if (shouldBeActive && active == null)
        {
            GameCondition condition = GameConditionMaker.MakeConditionPermanent(MSSFPDefOf.MSSFP_BlackHole);
            world.GameConditionManager.RegisterCondition(condition);
            ModLog.Log("[WorldComponent_MSSFPBlackHole] Black hole enabled (permanent, via setting).");
        }
        else if (shouldBeActive && active != null && !GameCondition_BlackHole.IsActive)
        {
            GameCondition_BlackHole.ActivateBlackHole(active.startTick);
            ModLog.Log("[WorldComponent_MSSFPBlackHole] Re-attached render helper for save-loaded black hole condition.");
        }
        else if (!shouldBeActive && active != null)
        {
            active.End();
            ModLog.Log("[WorldComponent_MSSFPBlackHole] Black hole disabled via setting.");
        }
    }
}

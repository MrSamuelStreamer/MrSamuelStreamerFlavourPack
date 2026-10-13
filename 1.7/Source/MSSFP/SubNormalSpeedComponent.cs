using Verse;

namespace MSSFP;

public enum SlowLevel
{
    None = 0,
    Half = 1,
    Quarter = 2,
    Eighth = 3,
}

public static class SlowLevelExtensions
{
    public static float Multiplier(this SlowLevel level) =>
        level switch
        {
            SlowLevel.Half => 0.5f,
            SlowLevel.Quarter => 0.25f,
            SlowLevel.Eighth => 0.125f,
            _ => 1f,
        };
}

public class SubNormalSpeedComponent : GameComponent
{
    public SlowLevel CurrentLevel = SlowLevel.None;

    // Cached instance so TickRateMultiplier's getter (a per-frame hot path) doesn't pay a
    // GetComponent scan over the game-component list on every call. Set once when the
    // component is constructed for the active game; cleared on load/new-game (see
    // MSSFP.HarmonyPatches.GameLoad_Patch) so a stale instance from a previous game can't
    // be returned before the new one is constructed.
    private static SubNormalSpeedComponent current;

    public SubNormalSpeedComponent(Game game)
    {
        current = this;
    }

    public static SubNormalSpeedComponent Current => current;

    public static void ClearCache() => current = null;
}

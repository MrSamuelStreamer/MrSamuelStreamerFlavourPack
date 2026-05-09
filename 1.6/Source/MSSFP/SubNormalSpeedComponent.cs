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

    public SubNormalSpeedComponent(Game game) { }

    public static SubNormalSpeedComponent Current =>
        Verse.Current.Game?.GetComponent<SubNormalSpeedComponent>();
}

using RimWorld;
using Verse;

namespace MSSFP.Tunnels;

public class GenStep_TunnelEntrance : GenStep_BaseRuins
{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private LayoutDef layoutDef;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

    public override int SeedPart => 9642121;

    protected override LayoutDef LayoutDef => layoutDef;

    protected override int RegionSize => 75;

    protected override FloatRange DefaultMapFillPercentRange => new(0.01f, 0.05f);

    protected override FloatRange MergeRange => new(0.1f, 0.35f);

    protected override int MoveRangeLimit => 3;

    protected override int ContractLimit => 3;

    protected override int MinRegionSize => 4;

    protected override Faction Faction => Faction.OfAncientsHostile;
}

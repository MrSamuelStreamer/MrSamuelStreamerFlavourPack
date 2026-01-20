using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_BreakdownableConfigurable : CompProperties
{
    public int mtbBreakdownTicks = 13680000;
    public string explanationTranslationKey = "BrokenDown";

    public CompProperties_BreakdownableConfigurable() => compClass = typeof(CompBreakdownableConfigurable);
}

public class CompBreakdownableConfigurable : CompBreakdownable
{
    public CompProperties_BreakdownableConfigurable BreakdownProperties => (CompProperties_BreakdownableConfigurable) this.props;

    public override string CompInspectStringExtra()
    {
        return BrokenDown ? BreakdownProperties.explanationTranslationKey.Translate() : null;
    }
}

using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompBreakdownableConfigurable : CompBreakdownable
{
    public CompProperties_BreakdownableConfigurable BreakdownProperties => (CompProperties_BreakdownableConfigurable) this.props;

    public override string CompInspectStringExtra()
    {
        return BrokenDown ? BreakdownProperties.explanationTranslationKey.Translate() : null;
    }
}

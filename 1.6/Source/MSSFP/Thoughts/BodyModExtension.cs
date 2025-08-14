using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MSSFP.Thoughts;

public class BodyModExtension : DefModExtension
{
    public BodyModCategoryDef mods;
    public List<HediffDef> BodyMods => mods?.bodyMods ?? [];

    public int NaturalImplantCount(Pawn pawn)
    {
        return pawn
            .health.hediffSet.hediffs.Where(h => BodyMods.Contains(h.def))
            .Count(t => t.def.countsAsAddedPartOrImplant);
    }

    public int UnnaturalImplantCount(Pawn pawn)
    {
        return pawn
            .health.hediffSet.hediffs.Where(h => !BodyMods.Contains(h.def))
            .Count(t => t.def.countsAsAddedPartOrImplant);
    }
}

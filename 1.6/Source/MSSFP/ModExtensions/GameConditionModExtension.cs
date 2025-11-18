using System.Collections.Generic;
using Verse;

namespace MSSFP.ModExtensions;

public class GameConditionModExtension: DefModExtension
{
    public bool canTargetAnimals = false;
    public bool canTargetMechs = true;
    public bool canTargetHumans = false;
    public bool canTargetSubhumans = false;
    public bool canTargetEntities = false;
    public bool canTargetBloodfeeders = false;

    public bool mustTargetPlayerFaction = true;

    public List<HediffDef> hediffsToApply;
}

using System.Collections.Generic;
using Verse;

namespace MSSFP.ModExtensions;

public class AgeUpGeneModDefExtension: DefModExtension
{
    public float WeightingForRandomSelection = 0.5f;
    public List<GeneDef> ConflictsWith;
}

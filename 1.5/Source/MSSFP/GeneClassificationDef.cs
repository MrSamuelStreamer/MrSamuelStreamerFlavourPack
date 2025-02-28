using System.Collections.Generic;
using Verse;

namespace MSSFP;

public enum GeneType
{
    good,
    bad,
    neutral,
    random
}

public enum ClassificationType
{
    ability,
    health,
    mental,
    psychic,
    cosmetic,
    skill
}

public class GeneClassification : IExposable
{
    public GeneDef gene;
    public float weight;
    public ClassificationType geneClassification;

    public List<GeneDef> requires;

    public GeneClassification(){}

    public GeneClassification(GeneDef gene, float weight)
    {
        this.gene = gene;
        this.weight = weight;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref gene, "gene");
        Scribe_Values.Look(ref weight, "weight");
        Scribe_Values.Look(ref geneClassification, "geneClassification");
        Scribe_Collections.Look(ref requires, "requires", LookMode.Def);
    }
}

public class GeneClassificationDef: Def
{
    public GeneType type;
    public List<GeneClassification> genes;
}

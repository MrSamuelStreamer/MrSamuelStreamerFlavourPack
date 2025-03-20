using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MSSFP.Genes;

public class GeneMutatorDef : Def
{
    public enum MutatorType
    {
        Birth,
        Worker,
    }

    public GameConditionDef conditionActive;
    public string reasonString;
    public string ReasonString => reasonString ?? conditionActive.LabelCap;
    public List<GeneClassification> genes;
    public GeneClassificationDef geneClassificationDef;
    public float? chanceToApply;
    public MutatorType type = MutatorType.Birth;
    public Type mutatorWorker;

    public bool pinataMode = false;
    public IntRange pinataModeChance = new IntRange(1, 10);

    public GeneMutatorWorker workerInt;

    public GeneMutatorWorker Worker
    {
        get
        {
            if (workerInt != null)
            {
                return workerInt;
            }

            workerInt = (GeneMutatorWorker)Activator.CreateInstance(mutatorWorker ?? typeof(GeneMutatorWorker));
            workerInt.Initialize(this);

            return workerInt;
        }
    }

    // null or empty genes allows for choice from any gene
    public GeneClassification RandomGene => GenePool.RandomElementByWeight(g => g.weight);

    public List<GeneClassification> GenePool
    {
        get
        {
            if (!genes.NullOrEmpty())
            {
                return genes;
            }
            if (geneClassificationDef != null)
            {
                return geneClassificationDef.genes;
            }

            return DefDatabase<GeneDef>.AllDefs.Select(g => new GeneClassification(g, 1f)).ToList();
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string configError in base.ConfigErrors())
            yield return configError;
        if (conditionActive is null)
            yield return "conditionActive must not be null";
        if (pinataMode && type != MutatorType.Worker)
            yield return "can't enable pinataMode when not a Worker type";
        if (type == MutatorType.Worker && mutatorWorker == null)
            yield return "mutatorWorker must be set if type is Exposure";
        if (type == MutatorType.Worker && mutatorWorker != null && !typeof(GeneMutatorWorker).IsAssignableFrom(mutatorWorker))
            yield return "mutatorWorker must be GeneMutatorWorker or a subclass of it";
        if (!genes.NullOrEmpty() && geneClassificationDef != null)
            yield return "can't define both genes and geneClassificationDef";
    }
}

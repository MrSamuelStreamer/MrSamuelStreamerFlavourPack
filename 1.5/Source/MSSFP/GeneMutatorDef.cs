using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace MSSFP;

public class GeneMutatorDef : Def
{
    public enum MutatorType
    {
        Birth,
        Worker
    }

    public class GeneDefWeightClass : IExposable
    {
        public GeneDef geneDef;
        public float weight;

        public GeneDefWeightClass() { }

        public GeneDefWeightClass(GeneDef g, float w)
        {
            geneDef = g;
            weight = w;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref geneDef, "geneDef");
            Scribe_Values.Look(ref weight, "weight");
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "geneDef", xmlRoot.Name);
            weight = ParseHelper.FromString<float>(xmlRoot.FirstChild.InnerText);
        }
    }

    public GameConditionDef conditionActive;
    public string reasonString;
    public string ReasonString => reasonString ?? conditionActive.LabelCap;
    public List<GeneDefWeightClass> genes;
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
    public GeneDef RandomGene => GenePool.RandomElementByWeight(g => g.weight).geneDef;
    public List<GeneDefWeightClass> GenePool => genes.NullOrEmpty() ? DefDatabase<GeneDef>.AllDefs.Select(g=>new GeneDefWeightClass(g, 1f)).ToList() : genes;


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
    }
}

using System.Collections.Generic;
using Verse;

namespace MSSFP.Comps;

public class PrisonerProcessorTracker : MapComponent
{
    private readonly List<CompPrisonerProcessor> processors = new();

    public PrisonerProcessorTracker(Verse.Map map)
        : base(map) { }

    public void Register(CompPrisonerProcessor comp) => processors.Add(comp);

    public void Deregister(CompPrisonerProcessor comp) => processors.Remove(comp);

    public IReadOnlyList<CompPrisonerProcessor> Processors => processors;
}

using System;
using RimWorld;

namespace MSSFP;

public class BookOutcomeProperties_ThoughtGiver : BookOutcomeProperties
{
    public ThoughtDef ThoughtDef;
    public override Type DoerClass => typeof(ReadingOutcomeDoerThoughtGiver);
}

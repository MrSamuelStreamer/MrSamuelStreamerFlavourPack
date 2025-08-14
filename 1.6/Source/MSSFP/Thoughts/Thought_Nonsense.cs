using MSSFP.Incidents;
using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class Thought_Nonsense : Thought_Memory
{
    public Thought_Nonsense() { }

    private string cachedLabelCap;
    private string cachedDescription;

    public override string LabelCap =>
        cachedLabelCap ??= IncidentWorker_Nonsense.ResolveAbsoluteText(
            pawn,
            "incidentLetter",
            true
        );
    public override string Description =>
        cachedDescription ??= IncidentWorker_Nonsense.ResolveAbsoluteText(
            pawn,
            "incidentDescription",
            true
        );

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cachedLabelCap, "cachedLabelCap");
        Scribe_Values.Look(ref cachedDescription, "cachedDescription");
    }
}

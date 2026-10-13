using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

/// <summary>
///     Tracks which IED raid batches have already sent their one decay-warning
///     letter, so a field of 5–20 traps from the same raid warns the player
///     once rather than once per trap. See <see cref="MSSFP.Comps.CompIEDDecay" />.
/// </summary>
public class IEDDecayMapComponent(Verse.Map map) : MapComponent(map)
{
    private HashSet<int> warnedBatches = new();

    /// <returns>True if this call fired the warning (first for this batch); false if already warned.</returns>
    public bool TryWarnBatch(int batchId, IntVec3 cell, Verse.Map onMap)
    {
        if (!warnedBatches.Add(batchId)) return false;

        Find.LetterStack.ReceiveLetter(
            "MSSFP_IEDDecayWarningLetterLabel".Translate(),
            "MSSFP_IEDDecayWarningLetterText".Translate(),
            LetterDefOf.NegativeEvent,
            new TargetInfo(cell, onMap));
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref warnedBatches, "warnedIEDBatches", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            warnedBatches ??= new HashSet<int>();
        }
    }
}

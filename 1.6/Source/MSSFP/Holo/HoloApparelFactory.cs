using RimWorld;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Factory for projected-apparel clones worn by holo pawns. Produces a fresh
/// <see cref="Apparel"/> instance of the same def + stuff, preserving quality + HP for
/// stat-contribution parity with the source. Skips Tainted / WornByCorpse / biocode —
/// the clone is a projection, not the physical item, so contagion-style flags don't transfer.
///
/// Caller MUST register the result via <see cref="HoloApparelRegistry.Mark"/> before any
/// gameplay code can observe it. Without the registry mark every enforcement gate
/// (drop-destroy, recursion guard) silently no-ops on this instance.
/// </summary>
public static class HoloApparelFactory
{
    /// <summary>
    /// Build a projection clone of <paramref name="src"/>. Returns null if <paramref name="src"/>
    /// is null. Does NOT mark the clone — caller's responsibility.
    /// </summary>
    public static Apparel Clone(Apparel src)
    {
        if (src == null) return null;

        Apparel clone = (Apparel)ThingMaker.MakeThing(src.def, src.Stuff);

        // Quality copy for StatOffsets parity (DamageDefence, Insulation etc. all scale by quality).
        CompQuality srcQuality = src.TryGetComp<CompQuality>();
        if (srcQuality != null)
        {
            CompQuality cloneQuality = clone.TryGetComp<CompQuality>();
            cloneQuality?.SetQuality(srcQuality.Quality, ArtGenerationContext.Outsider);
        }

        // HP copy preserves stat factors that key off HitPoints/MaxHitPoints (e.g. damaged armour).
        if (src.def.useHitPoints)
            clone.HitPoints = src.HitPoints;

        // Tainted / WornByCorpse intentionally NOT copied — projected version, no contagion.
        // Biocode intentionally NOT copied — clone is unbiocoded; holo wears the projection
        // regardless of source bond.

        return clone;
    }
}

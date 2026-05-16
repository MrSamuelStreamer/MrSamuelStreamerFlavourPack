using System.Collections.Generic;
using MSSFP.Comps;
using MSSFP.Defs;
using RimWorld;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Gate which hediffs may land on a holo pawn. Vanilla age-tickers, body-mod incidents,
/// disease incidents etc. all funnel through <see cref="Pawn_HealthTracker.AddHediff(Hediff, BodyPartRecord, System.Nullable{DamageInfo}, DamageWorker.DamageResult)"/>;
/// the filter patch consults this policy before letting the add through.
///
/// Allow rules (any one passes):
///   1. <see cref="MSSFPDefOf.MSSFP_Hediff_Hologram"/> itself (system marker).
///   2. Chemical-related: addiction, drug-effect, withdrawal — overdose is intentional comedy.
///   3. Listed in the persona's <see cref="AIPersonalityDef.allowedHediffs"/>.
///   4. Listed in the persona's <see cref="AIPersonalityDef.fixedHediffs"/>
///      (so re-applying a fixed hediff at spawn isn't blocked by the same patch).
/// </summary>
public static class HoloHediffPolicy
{
    public static bool IsAllowed(Pawn pawn, HediffDef def)
    {
        if (def == null) return false;
        if (def == MSSFPDefOf.MSSFP_Hediff_Hologram) return true;
        if (IsChemical(def)) return true;

        AIPersonalityDef persona = ResolvePersona(pawn);
        if (persona == null) return false;
        if (persona.allowedHediffs != null && persona.allowedHediffs.Contains(def)) return true;
        if (persona.fixedHediffs != null && persona.fixedHediffs.Contains(def)) return true;
        return false;
    }

    /// <summary>
    /// Heuristic for "this is a chemical / drug / addiction hediff". Vanilla has no single
    /// flag — combine the three signals that actually exist on HediffDef + hediffClass.
    /// </summary>
    public static bool IsChemical(HediffDef def)
    {
        if (def == null) return false;
        if (def.IsAddiction) return true;
        if (def.chemicalNeed != null) return true;
        if (def.hediffClass != null)
        {
            // Drug-effect hediffs (AlcoholHigh, GoJuiceHigh, etc.) derive from Hediff_High.
            if (typeof(Hediff_High).IsAssignableFrom(def.hediffClass)) return true;
            if (typeof(Hediff_Addiction).IsAssignableFrom(def.hediffClass)) return true;
            // Drug-aftereffect hediffs (Hangover) — set by hardcoded vanilla code, not visible
            // through the def graph. Match by class name (covers any mod-added subclass too).
            if (def.hediffClass.Name == "Hediff_Hangover"
                || typeof(Hediff_Hangover).IsAssignableFrom(def.hediffClass)) return true;
        }
        if (def.HasComp(typeof(HediffComp_DrugEffectFactor))) return true;
        // Tolerances + drug-aftereffect hediffs (Hangover, X-Tolerance) — looked up against
        // the ChemicalDef ↔ HediffDef map cached at first call.
        return DrugHediffs.Contains(def);
    }

    /// <summary>
    /// Cached set of every HediffDef referenced by a ChemicalDef (toleranceHediff /
    /// addictionHediff) or produced by an ingestible drug's outcome-doer. Built lazily
    /// after defs are loaded; never mutated after first build.
    /// </summary>
    private static HashSet<HediffDef> drugHediffsCache;
    private static HashSet<HediffDef> DrugHediffs
    {
        get
        {
            if (drugHediffsCache != null) return drugHediffsCache;
            HashSet<HediffDef> set = new();
            foreach (ChemicalDef chem in DefDatabase<ChemicalDef>.AllDefsListForReading)
            {
                if (chem.toleranceHediff != null) set.Add(chem.toleranceHediff);
                if (chem.addictionHediff != null) set.Add(chem.addictionHediff);
            }
            foreach (ThingDef td in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                IngestibleProperties ing = td.ingestible;
                if (ing?.outcomeDoers == null) continue;
                foreach (IngestionOutcomeDoer doer in ing.outcomeDoers)
                {
                    if (doer is IngestionOutcomeDoer_GiveHediff giver && giver.hediffDef != null)
                        set.Add(giver.hediffDef);
                }
            }
            drugHediffsCache = set;
            return drugHediffsCache;
        }
    }

    /// <summary>
    /// Apply the persona's <see cref="AIPersonalityDef.fixedHediffs"/> to a freshly-projected
    /// holo pawn. Idempotent: already-present defs are skipped so re-projection doesn't stack.
    /// Called from <see cref="CompHoloProjector.SpawnProjection"/> after the hologram marker
    /// is attached.
    /// </summary>
    public static void ApplyFixedHediffs(Pawn pawn, AIPersonalityDef persona)
    {
        if (pawn?.health?.hediffSet == null) return;
        if (persona?.fixedHediffs == null) return;

        foreach (HediffDef def in persona.fixedHediffs)
        {
            if (def == null) continue;
            if (pawn.health.hediffSet.HasHediff(def)) continue;
            Hediff h = HediffMaker.MakeHediff(def, pawn);
            pawn.health.AddHediff(h);
        }
    }

    /// <summary>
    /// Grant the holo's "AI personality" trait. Idempotent — re-projection won't stack.
    /// <c>forced: true</c> so vanilla random-trait passes and dev-tool removal both leave it alone.
    /// </summary>
    public static void ApplyHoloTrait(Pawn pawn)
    {
        if (pawn?.story?.traits == null) return;
        TraitDef def = MSSFPDefOf.MSSF_AIPersonality;
        if (def == null) return;
        if (pawn.story.traits.HasTrait(def)) return;
        pawn.story.traits.GainTrait(new Trait(def, degree: 0, forced: true));
    }

    /// <summary>True iff <paramref name="def"/> describes an injury (Hediff_Injury subclass).</summary>
    public static bool IsInjury(HediffDef def) =>
        def?.hediffClass != null && typeof(Hediff_Injury).IsAssignableFrom(def.hediffClass);

    /// <summary>
    /// Hediffs the Defrag cycle removes: chemical (addictions, drug effects) and injuries.
    /// Permanent stuff (scars, old wounds, persona-fixed hediffs) stays. The hologram marker
    /// itself is never removed here.
    /// </summary>
    public static List<Hediff> CollectDefragRemovable(Pawn pawn)
    {
        List<Hediff> removable = new();
        if (pawn?.health?.hediffSet == null) return removable;

        foreach (Hediff h in pawn.health.hediffSet.hediffs)
        {
            if (h == null) continue;
            if (h.def == MSSFPDefOf.MSSFP_Hediff_Hologram) continue;
            if (h is Hediff_Injury) { removable.Add(h); continue; }
            if (IsChemical(h.def)) { removable.Add(h); continue; }
        }
        return removable;
    }

    private static AIPersonalityDef ResolvePersona(Pawn p) =>
        p?.TryGetComp<CompHoloProjected>()
         ?.sourceProjector?.TryGetComp<CompTrueAICore>()?.activePersonality;
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Settings;
using RimWorld;
using Verse;

namespace MSSFP.PawnPortability.Import
{
    /// <summary>
    /// Creates live Pawns from PawnTemplateDef instances.
    /// Template mode: minimal PawnGenerationRequest + conditional overlay.
    /// </summary>
    internal static class PawnTemplateSpawner
    {
        private static readonly FieldInfo ChildhoodField = typeof(Pawn_StoryTracker).GetField(
            "childhood",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo AdulthoodField = typeof(Pawn_StoryTracker).GetField(
            "adulthood",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo BirthLastNameField = typeof(Pawn_StoryTracker).GetField(
            "birthLastName",
            BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Create a pawn from a PawnTemplateDef without placing on a map.
        /// </summary>
        public static Pawn Create(PawnTemplateDef def, Faction faction = null,
            PawnKindDef kindOverride = null)
        {
            if (def == null)
            {
                ModLog.Error("[PawnPortability] Cannot create pawn: def is null");
                return null;
            }

            try
            {
                bool logging = PawnPortabilitySettings.LoggingEnabled;
                if (logging)
                    ModLog.Log($"[PawnPortability] Creating pawn from def: {def.defName} (mode: {def.mode})");

                // Step 1: Build MINIMAL PawnGenerationRequest
                PawnKindDef kind = kindOverride ?? def.kindDef ?? PawnKindDefOf.Colonist;
                Faction resolvedFaction = faction ?? Faction.OfPlayer;

                float? bioAge = def.age?.biologicalAgeTicks > 0
                    ? (float)(def.age.biologicalAgeTicks / (double)GenDate.TicksPerYear)
                    : null;
                float? chronoAge = def.age?.chronologicalAgeTicks > 0
                    ? (float)(def.age.chronologicalAgeTicks / (double)GenDate.TicksPerYear)
                    : null;

                bool forceNoBackstory = def.story?.childhood != null || def.story?.adulthood != null;

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kind,
                    faction: resolvedFaction,
                    fixedGender: def.gender,
                    fixedBiologicalAge: bioAge,
                    fixedChronologicalAge: chronoAge,
                    canGeneratePawnRelations: false,
                    allowFood: false,
                    allowAddictions: false,
                    forceNoBackstory: forceNoBackstory
                );
                // Set body type via property — not available as constructor param
                if (def.story?.bodyType != null)
                    request.ForceBodyType = def.story.bodyType;
                // CRITICAL: Do NOT set ForcedTraits, ForcedXenotype, ForcedEndogenes, ForcedXenogenes

                // Step 2: Generate base pawn
                Pawn pawn = PawnGenerator.GeneratePawn(request);

                if (logging)
                    ModLog.Log($"[PawnPortability] Base pawn generated, applying overlay for {def.defName}");

                // Step 3: Conditional overlay — only for non-null fields
                OverlayName(pawn, def, logging);
                OverlayBackstories(pawn, def, logging);
                OverlayAppearance(pawn, def, logging);
                OverlayTraits(pawn, def, logging);
                OverlayGenes(pawn, def, logging);
                OverlaySkills(pawn, def, logging);
                OverlayHediffs(pawn, def, logging);
                OverlayEquipment(pawn, def, logging);
                OverlayApparel(pawn, def, logging);
                OverlayAbilities(pawn, def, logging);
                OverlayStyle(pawn, def, logging);

                // Step 4: Post-overlay recache
                pawn.Notify_DisabledWorkTypesChanged();
                pawn.skills?.Notify_SkillDisablesChanged();
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();

                if (logging)
                    ModLog.Log($"[PawnPortability] Pawn {def.defName} created successfully");

                return pawn;
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to create pawn from def {def.defName}", ex);
                return null;
            }
        }

        /// <summary>
        /// Create and place a pawn on the map.
        /// </summary>
        public static Pawn Spawn(PawnTemplateDef def, IntVec3 position, Map map,
            Faction faction = null, PawnKindDef kindOverride = null)
        {
            Pawn pawn = Create(def, faction, kindOverride);
            if (pawn == null) return null;

            try
            {
                GenSpawn.Spawn(pawn, position, map);
                return pawn;
            }
            catch (Exception ex)
            {
                ModLog.Error($"[PawnPortability] Failed to spawn pawn {def.defName} at {position}", ex);
                return null;
            }
        }

        // ── Overlay Steps ──────────────────────────────────────────────────

        // a. Name
        private static void OverlayName(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.name == null) return;

            pawn.Name = new NameTriple(
                def.name.first ?? "",
                def.name.nick ?? def.name.first ?? "",
                def.name.last ?? "");

            if (logging)
                ModLog.Log($"[PawnPortability] Set name: {pawn.Name}");
        }

        // b. Backstories
        private static void OverlayBackstories(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.story == null) return;

            if (def.story.childhood != null && ChildhoodField != null)
            {
                ChildhoodField.SetValue(pawn.story, def.story.childhood);
                if (logging)
                    ModLog.Log($"[PawnPortability] Set childhood: {def.story.childhood.defName}");
            }

            if (def.story.adulthood != null && AdulthoodField != null)
            {
                AdulthoodField.SetValue(pawn.story, def.story.adulthood);
                if (logging)
                    ModLog.Log($"[PawnPortability] Set adulthood: {def.story.adulthood.defName}");
            }

            if (!string.IsNullOrEmpty(def.story.birthLastName) && BirthLastNameField != null)
            {
                BirthLastNameField.SetValue(pawn.story, def.story.birthLastName);
                if (logging)
                    ModLog.Log($"[PawnPortability] Set birthLastName: {def.story.birthLastName}");
            }
        }

        // c. Appearance
        private static void OverlayAppearance(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.story == null) return;

            if (def.story.bodyType != null)
                pawn.story.bodyType = def.story.bodyType;

            if (def.story.headType != null)
                pawn.story.headType = def.story.headType;

            if (def.story.hairDef != null)
                pawn.story.hairDef = def.story.hairDef;

            if (def.story.hairColor != default)
                pawn.story.HairColor = def.story.hairColor;

            if (def.story.skinColorOverride.HasValue)
                pawn.story.skinColorOverride = def.story.skinColorOverride;

            if (def.story.furDef != null)
                pawn.story.furDef = def.story.furDef;

            if (def.story.favoriteColor != null)
                pawn.story.favoriteColor = def.story.favoriteColor;

            if (logging)
                ModLog.Log("[PawnPortability] Applied appearance overlay");
        }

        // d. Traits — clear ALL then set
        private static void OverlayTraits(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.story?.traits == null) return;

            pawn.story.traits.allTraits.Clear();
            foreach (PawnTraitEntry entry in def.story.traits)
            {
                if (entry.def == null) continue;
                pawn.story.traits.GainTrait(new Trait(entry.def, entry.degree));
                if (logging)
                    ModLog.Log($"[PawnPortability] Added trait: {entry.def.defName} (degree {entry.degree})");
            }
        }

        // e. Genes — clear ALL then set
        private static void OverlayGenes(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.genes == null || pawn.genes == null) return;

            pawn.genes.Endogenes.Clear();
            pawn.genes.Xenogenes.Clear();

            if (def.genes.xenotype != null)
            {
                pawn.genes.SetXenotype(def.genes.xenotype);
                if (logging)
                    ModLog.Log($"[PawnPortability] Set xenotype: {def.genes.xenotype.defName}");
            }

            if (def.genes.endogenes != null)
            {
                foreach (GeneDef gene in def.genes.endogenes)
                {
                    if (gene == null) continue;
                    pawn.genes.AddGene(gene, xenogene: false);
                    if (logging)
                        ModLog.Log($"[PawnPortability] Added endogene: {gene.defName}");
                }
            }

            if (def.genes.xenogenes != null)
            {
                foreach (GeneDef gene in def.genes.xenogenes)
                {
                    if (gene == null) continue;
                    pawn.genes.AddGene(gene, xenogene: true);
                    if (logging)
                        ModLog.Log($"[PawnPortability] Added xenogene: {gene.defName}");
                }
            }
        }

        // f. Skills — set only specified skills
        private static void OverlaySkills(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.skills == null || pawn.skills == null) return;

            foreach (PawnSkillEntry entry in def.skills)
            {
                if (entry.def == null) continue;
                SkillRecord rec = pawn.skills.GetSkill(entry.def);
                if (rec == null) continue;

                rec.levelInt = Math.Max(0, entry.level);
                rec.passion = entry.passion;
                if (logging)
                    ModLog.Log($"[PawnPortability] Set skill {entry.def.defName}: level {entry.level}, passion {entry.passion}");
            }
        }

        // g. Hediffs — clear non-gene hediffs, then add from template
        private static void OverlayHediffs(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.hediffs == null) return;

            // Clear ALL hediffs (removes PawnGenerator's random age injuries, scars, etc.)
            // Gene-sourced hediffs will be re-established by the gene tracker as needed.
            List<Hediff> toRemove = pawn.health.hediffSet.hediffs.ToList();
            foreach (Hediff hediff in toRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }

            foreach (PawnHediffEntry entry in def.hediffs)
            {
                if (entry.def == null) continue;
                try
                {
                    BodyPartRecord bodyPart = FindBodyPart(pawn, entry);
                    Hediff hediff = HediffMaker.MakeHediff(entry.def, pawn, bodyPart);
                    if (entry.severity >= 0)
                        hediff.Severity = entry.severity;

                    pawn.health.AddHediff(hediff, bodyPart);
                    if (logging)
                        ModLog.Log(
                            $"[PawnPortability] Added hediff: {entry.def.defName}" +
                            (bodyPart != null ? $" on {bodyPart.Label}" : " (whole body)"));
                }
                catch (Exception ex)
                {
                    ModLog.Warn(
                        $"[PawnPortability] Failed to add hediff {entry.def.defName}: {ex.Message}");
                }
            }
        }

        // h. Equipment — destroy all then set
        private static void OverlayEquipment(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.equipment == null || pawn.equipment == null) return;

            pawn.equipment.DestroyAllEquipment();
            foreach (PawnItemEntry entry in def.equipment)
            {
                Thing thing = CreateThing(entry, logging);
                if (thing is ThingWithComps twc)
                    pawn.equipment.AddEquipment(twc);
            }
        }

        // i. Apparel — destroy all then set
        private static void OverlayApparel(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.apparel == null || pawn.apparel == null) return;

            pawn.apparel.DestroyAll();
            foreach (PawnItemEntry entry in def.apparel)
            {
                Thing thing = CreateThing(entry, logging);
                if (thing is Apparel apparelItem)
                    pawn.apparel.Wear(apparelItem, dropReplacedApparel: false);
            }
        }

        // j. Abilities — add on top (keep gene-sourced)
        private static void OverlayAbilities(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.abilities == null || pawn.abilities == null) return;

            foreach (AbilityDef abilityDef in def.abilities)
            {
                if (abilityDef == null) continue;
                pawn.abilities.GainAbility(abilityDef);
                if (logging)
                    ModLog.Log($"[PawnPortability] Added ability: {abilityDef.defName}");
            }
        }

        // k. Style
        private static void OverlayStyle(Pawn pawn, PawnTemplateDef def, bool logging)
        {
            if (def.style == null || pawn.style == null) return;

            if (def.style.beardDef != null)
                pawn.style.beardDef = def.style.beardDef;

            if (def.style.faceTattoo != null)
                pawn.style.FaceTattoo = def.style.faceTattoo;

            if (def.style.bodyTattoo != null)
                pawn.style.BodyTattoo = def.style.bodyTattoo;

            if (logging)
                ModLog.Log("[PawnPortability] Applied style overlay");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static BodyPartRecord FindBodyPart(Pawn pawn, PawnHediffEntry entry)
        {
            if (entry.bodyPart == null) return null;

            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts
                .Where(p => p.def == entry.bodyPart)
                .ToList();

            if (parts.Count == 0) return null;
            if (parts.Count == 1) return parts[0];

            // Disambiguate by label
            if (!string.IsNullOrEmpty(entry.bodyPartLabel))
            {
                BodyPartRecord labeled = parts.FirstOrDefault(
                    p => p.Label.EqualsIgnoreCase(entry.bodyPartLabel));
                if (labeled != null) return labeled;

                if (PawnPortabilitySettings.LoggingEnabled)
                    ModLog.Warn(
                        $"[PawnPortability] Body part label '{entry.bodyPartLabel}' not found for " +
                        $"{entry.bodyPart.defName}, using first match");
            }

            return parts[0];
        }

        private static Thing CreateThing(PawnItemEntry entry, bool logging)
        {
            if (entry.def == null) return null;

            try
            {
                Thing thing = ThingMaker.MakeThing(entry.def, entry.stuff);

                if (thing.TryGetComp<CompQuality>() is CompQuality qualityComp)
                    qualityComp.SetQuality(entry.quality, ArtGenerationContext.Outsider);

                if (entry.hitPoints > 0)
                    thing.HitPoints = entry.hitPoints;
                else
                    thing.HitPoints = thing.MaxHitPoints;

                if (entry.stackCount > 1)
                    thing.stackCount = entry.stackCount;

                if (logging)
                    ModLog.Log(
                        $"[PawnPortability] Created thing: {entry.def.defName}" +
                        (entry.stuff != null ? $" ({entry.stuff.defName})" : "") +
                        $" quality={entry.quality}");

                return thing;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"[PawnPortability] Failed to create thing {entry.def.defName}: {ex.Message}");
                return null;
            }
        }
    }
}

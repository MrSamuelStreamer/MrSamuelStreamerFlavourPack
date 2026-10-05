using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MSSFP.PawnPortability;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.LabelIcons;

/// <summary>
/// The icon table and the per-pawn evaluation behind the map-label icon row.
/// Order matters: the first icon is drawn nearest the label.
/// </summary>
[StaticConstructorOnStartup]
public static class LabelIcons
{
    // Real time, not ticks: ticks stop while paused, and pausing to draft or
    // tend is exactly when the icons need to be current.
    private const float CacheSeconds = 0.25f;
    private const float ToxicSeverityThreshold = 0.2f;
    private const int DyingBleedOutTicks = GenDate.TicksPerHour * 6;

    private static readonly Color Red = new(0.9f, 0.15f, 0.15f);
    private static readonly Color Green = new(0.35f, 0.9f, 0.2f);
    private static readonly Color Orange = new(1f, 0.55f, 0.1f);
    private static readonly Color Yellow = new(1f, 0.85f, 0.2f);
    private static readonly Color Purple = new(0.7f, 0.4f, 1f);

    public static readonly IReadOnlyList<LabelIcon> All = new List<LabelIcon>
    {
        new("Template", "UI/MSSFP/UserSubmitted", Color.white, p => Hediff_TemplateOrigin.Of(p) != null),
        new("Drafted", "UI/MSSFP/Drafted", Red, p => p.Drafted, live: true),
        new("Bleeding", "UI/MSSFP/Bleeding", Red, p => p.health.hediffSet.BleedRateTotal > 0f),
        new("Toxic", "UI/MSSFP/Toxic", Green, IsToxic),
        // Multicolour source art: drawn untinted.
        new("Dying", "UI/MSSFP/Dying", Color.white, IsDying, pulse: true),
        new("MentalBreak", "UI/MSSFP/MentalBreak", Orange, p => p.InMentalState, live: true),
        new("NeedsTending", "UI/MSSFP/NeedsTending", Yellow, p => p.health.HasHediffsNeedingTend()),
        new("Sick", "UI/MSSFP/Sick", Purple, IsSick),
    };

    private sealed class CacheEntry
    {
        public float Time = float.MinValue;
        public readonly bool[] Results = new bool[All.Count];
    }

    private static readonly ConditionalWeakTable<Pawn, CacheEntry> Cache = new();

    /// <summary>Fills <paramref name="into"/> with the enabled icons that apply to the pawn.</summary>
    public static void Collect(Pawn pawn, List<LabelIcon> into)
    {
        into.Clear();
        if (pawn?.health?.hediffSet == null) return;

        CacheEntry entry = Cache.GetOrCreateValue(pawn);
        bool refresh = Time.realtimeSinceStartup - entry.Time >= CacheSeconds;
        if (refresh) entry.Time = Time.realtimeSinceStartup;

        for (int i = 0; i < All.Count; i++)
        {
            LabelIcon icon = All[i];
            if (!LabelIconsSettings.IsEnabled(icon.Id)) continue;

            bool applies;
            if (icon.Live) applies = icon.Applies(pawn);
            else
            {
                if (refresh) entry.Results[i] = icon.Applies(pawn);
                applies = entry.Results[i];
            }

            if (applies) into.Add(icon);
        }
    }

    private static bool IsToxic(Pawn pawn)
    {
        Hediff toxic = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
        return toxic is { Visible: true } && toxic.Severity >= ToxicSeverityThreshold;
    }

    private static bool IsDying(Pawn pawn)
    {
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff.Visible && hediff.CurStage?.lifeThreatening == true) return true;
        }

        return pawn.health.hediffSet.BleedRateTotal > 0f
               && HealthUtility.TicksUntilDeathDueToBloodLoss(pawn) < DyingBleedOutTicks;
    }

    private static bool IsSick(Pawn pawn)
    {
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff.Visible && hediff.TryGetComp<HediffComp_Immunizable>() != null) return true;
        }

        return false;
    }
}

using Maux36.Rimbody;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.RB;

/// <summary>
/// Settings for pawn lifting — using another colonist as a free weight.
///
/// Discovered by reflection over loaded assemblies (see <c>MSSFP.Settings.LoadTabs</c>),
/// so it registers ONLY when this assembly is loaded, which <c>loadFolders.xml</c> gates
/// on <c>maux36.rimbody</c> being active. Players without Rimbody never see the tab.
/// State lives here, not on core <see cref="MSSFP.Settings"/>, for the same reason.
/// </summary>
public class PawnLiftSettingsTab : SettingsTab
{
    /// <summary>Sentinel meaning "track Rimbody's own hulk threshold instead of a fixed number".</summary>
    public const float FollowRimbodyHulkThreshold = -1f;

    public bool EnablePawnLifting = true;

    /// <summary>
    /// Muscle mass a lifter needs before pawn lifting is offered. Defaults to tracking
    /// Rimbody's player-configurable hulk threshold (35 by default) rather than a hardcoded
    /// number: Rimbody generates normal pawns in the 16-34 band, so any fixed cutoff in the
    /// twenties qualifies roughly a quarter of a starting colony on day one.
    /// </summary>
    public float MuscleThreshold = FollowRimbodyHulkThreshold;

    public bool AllowIdleColonists = true;
    public bool AllowPrisoners = true;
    public bool AllowDowned = true;
    public bool GiveLifteeThought = true;

    public PawnLiftSettingsTab(ModSettings settings, Mod mod)
        : base(settings, mod) { }

    public override string TabName => "Pawn Lifting";

    public override int TabOrder => 90;

    private static PawnLiftSettingsTab Instance => MSSFPMod.settings?.GetSettings<PawnLiftSettingsTab>();

    public static bool Enabled => Instance?.EnablePawnLifting ?? true;

    public static bool LifteeThought => Instance?.GiveLifteeThought ?? true;

    public static bool IdleColonistsAllowed => Instance?.AllowIdleColonists ?? true;

    public static bool PrisonersAllowed => Instance?.AllowPrisoners ?? true;

    public static bool DownedAllowed => Instance?.AllowDowned ?? true;

    /// <summary>
    /// Live threshold. Reads Rimbody's hulk threshold when set to the sentinel so that a
    /// player who retunes Rimbody does not also have to retune this.
    /// </summary>
    public static float Threshold
    {
        get
        {
            float configured = Instance?.MuscleThreshold ?? FollowRimbodyHulkThreshold;
            return configured <= 0f ? RimbodySettings.muscleThresholdHulk : configured;
        }
    }

    public override void DoTabContents(Listing_Standard options, Rect scrollViewRect, ref float scrollViewHeight)
    {
        scrollViewHeight += options.Label("MSSFP_RB_Settings_Blurb".Translate()).height;

        DrawCheckBox(options, "MSSFP_RB_Settings_Enable".Translate(), ref EnablePawnLifting, ref scrollViewHeight);

        if (!EnablePawnLifting)
            return;

        options.GapLine();
        scrollViewHeight += 12f;

        bool followRimbody = MuscleThreshold <= 0f;
        bool wasFollowing = followRimbody;
        DrawCheckBox(
            options,
            "MSSFP_RB_Settings_FollowHulkThreshold".Translate(RimbodySettings.muscleThresholdHulk.ToString("F0")),
            ref followRimbody,
            ref scrollViewHeight
        );

        if (followRimbody)
        {
            MuscleThreshold = FollowRimbodyHulkThreshold;
        }
        else
        {
            if (wasFollowing)
                MuscleThreshold = RimbodySettings.muscleThresholdHulk;

            MuscleThreshold = options.SliderLabeled(
                "MSSFP_RB_Settings_MuscleThreshold".Translate(MuscleThreshold.ToString("F0")),
                MuscleThreshold,
                1f,
                50f
            );
            scrollViewHeight += 30f;
        }

        options.GapLine();
        scrollViewHeight += 12f;

        DrawCheckBox(options, "MSSFP_RB_Settings_AllowIdleColonists".Translate(), ref AllowIdleColonists, ref scrollViewHeight);
        DrawCheckBox(options, "MSSFP_RB_Settings_AllowPrisoners".Translate(), ref AllowPrisoners, ref scrollViewHeight);
        DrawCheckBox(options, "MSSFP_RB_Settings_AllowDowned".Translate(), ref AllowDowned, ref scrollViewHeight);
        DrawCheckBox(options, "MSSFP_RB_Settings_LifteeThought".Translate(), ref GiveLifteeThought, ref scrollViewHeight);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref EnablePawnLifting, "MSSFP_RB_EnablePawnLifting", true);
        Scribe_Values.Look(ref MuscleThreshold, "MSSFP_RB_MuscleThreshold", FollowRimbodyHulkThreshold);
        Scribe_Values.Look(ref AllowIdleColonists, "MSSFP_RB_AllowIdleColonists", true);
        Scribe_Values.Look(ref AllowPrisoners, "MSSFP_RB_AllowPrisoners", true);
        Scribe_Values.Look(ref AllowDowned, "MSSFP_RB_AllowDowned", true);
        Scribe_Values.Look(ref GiveLifteeThought, "MSSFP_RB_GiveLifteeThought", true);
    }
}

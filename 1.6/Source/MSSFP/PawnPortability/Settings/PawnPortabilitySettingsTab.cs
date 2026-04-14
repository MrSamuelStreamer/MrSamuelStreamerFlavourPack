using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.PawnPortability.Settings
{
    public class PawnPortabilitySettingsTab : SettingsTab
    {
        public bool EnablePawnPortabilityLogging;
        public bool ShowExportGizmo;

        public PawnPortabilitySettingsTab(ModSettings settings, Mod mod)
            : base(settings, mod)
        {
        }

        public override string TabName => "Pawn Portability";
        public override int TabOrder => 80;

        public override void DoTabContents(
            Listing_Standard options,
            Rect scrollViewRect,
            ref float scrollViewHeight)
        {
            DrawCheckBox(
                options,
                "MSS_FP_Settings_ShowExportGizmo".Translate(),
                ref ShowExportGizmo,
                ref scrollViewHeight);

            DrawCheckBox(
                options,
                "MSS_FP_Settings_EnablePawnPortabilityLogging".Translate(),
                ref EnablePawnPortabilityLogging,
                ref scrollViewHeight);

            options.GapLine();
            scrollViewHeight += 12f;

            MSSFP.Settings s = settings as MSSFP.Settings;

            DrawCheckBox(
                options,
                "MSS_FP_Settings_EnableTemplateWandererJoin".Translate(),
                ref s.EnableTemplateWandererJoin,
                ref scrollViewHeight);

            if (s.EnableTemplateWandererJoin)
            {
                s.TemplateWandererJoinChanceMultiplier = options.SliderLabeled(
                    "MSS_FP_Settings_TemplateWandererJoinChanceMultiplier".Translate(
                        s.TemplateWandererJoinChanceMultiplier.ToString("F1")),
                    s.TemplateWandererJoinChanceMultiplier, 0.1f, 5.0f);
                scrollViewHeight += 30f;
            }

            options.GapLine();
            scrollViewHeight += 12f;

            DrawCheckBox(
                options,
                "MSS_FP_Settings_EnableUserTemplateLoading".Translate(),
                ref s.EnableUserTemplateLoading,
                ref scrollViewHeight);

            if (s.EnableUserTemplateLoading)
            {
                // Export directory path
                string dirLabel = "MSS_FP_Settings_UserTemplateDir".Translate(
                    UserPawnTemplateRegistry.ExportedPawnsDir);
                float dirHeight = Text.CalcHeight(dirLabel, options.ColumnWidth);
                options.Label(dirLabel, dirHeight);
                scrollViewHeight += dirHeight;

                // Loaded count
                options.Label("MSS_FP_Settings_UserTemplateCount".Translate(
                    UserPawnTemplateRegistry.Count));
                scrollViewHeight += 24f;

                options.Gap(6f);
                scrollViewHeight += 6f;

                if (options.ButtonText("MSS_FP_Settings_ReloadUserTemplates".Translate()))
                {
                    UserPawnTemplateRegistry.Refresh();
                    Messages.Message(
                        "MSS_FP_Settings_UserTemplatesReloaded".Translate(UserPawnTemplateRegistry.Count),
                        MessageTypeDefOf.PositiveEvent);
                }
                scrollViewHeight += 30f;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(
                ref ShowExportGizmo,
                "ShowExportGizmo",
                false);
            Scribe_Values.Look(
                ref EnablePawnPortabilityLogging,
                "EnablePawnPortabilityLogging",
                false);

            MSSFP.Settings s = settings as MSSFP.Settings;
            Scribe_Values.Look(ref s.EnableUserTemplateLoading, "EnableUserTemplateLoading", true);
            Scribe_Values.Look(ref s.EnableTemplateWandererJoin, "EnableTemplateWandererJoin", false);
            Scribe_Values.Look(ref s.TemplateWandererJoinChanceMultiplier, "TemplateWandererJoinChanceMultiplier", 1.0f);
        }
    }

    /// <summary>
    /// Static accessor for the logging setting, used throughout the PawnPortability system.
    /// </summary>
    public static class PawnPortabilitySettings
    {
        public static bool LoggingEnabled
        {
            get
            {
                var tab = MSSFPMod.settings?.GetSettings<PawnPortabilitySettingsTab>();
                return tab?.EnablePawnPortabilityLogging ?? false;
            }
        }

        public static bool ExportGizmoEnabled
        {
            get
            {
                var tab = MSSFPMod.settings?.GetSettings<PawnPortabilitySettingsTab>();
                return tab?.ShowExportGizmo ?? false;
            }
        }
    }
}

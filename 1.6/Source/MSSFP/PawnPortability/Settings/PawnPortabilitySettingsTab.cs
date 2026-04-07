using MSSFP.Utils;
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

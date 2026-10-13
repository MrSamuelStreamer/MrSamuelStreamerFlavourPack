using System.Collections.Generic;
using System.Linq;
using MSSFP.HarmonyPatches;
using MSSFP.PawnPortability.Defs;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.PawnPortability.Settings
{
    public class PawnPortabilitySettingsTab : SettingsTab
    {
        public const string DefaultTemplateNameColour = "green";

        public bool EnablePawnPortabilityLogging;
        public bool ShowExportGizmo;

        public bool EnableTemplateInjection;
        public float TemplateInjectionChance = 0.02f;
        public HashSet<string> ExcludedTemplateDefNames = new();

        public bool ColourTemplateNames;
        public string TemplateNameColour = DefaultTemplateNameColour;
        public bool ColourHostileTemplateNames;

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

            DrawCheckBox(
                options,
                "MSS_FP_Settings_EnableTemplateInjection".Translate(),
                ref EnableTemplateInjection,
                ref scrollViewHeight);

            if (EnableTemplateInjection)
            {
                TemplateInjectionChance = options.SliderLabeled(
                    "MSS_FP_Settings_TemplateInjectionChance".Translate(
                        TemplateInjectionChance.ToStringPercent("0.#")),
                    TemplateInjectionChance, 0f, 1f);
                scrollViewHeight += 30f;
            }

            DrawGeneratedThisGame(options, ref scrollViewHeight);

            DrawSectionHeader(options, "MSS_FP_Settings_TemplateNameDisplay".Translate(), ref scrollViewHeight);
            DrawNameDisplay(options, ref scrollViewHeight);

            DrawSectionHeader(options, "MSS_FP_Settings_ExcludedTemplates".Translate(), ref scrollViewHeight);
            DrawExcludeList(options, ref scrollViewHeight);

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

        private void DrawGeneratedThisGame(Listing_Standard options, ref float scrollViewHeight)
        {
            TemplateGenerationTracker tracker = TemplateGenerationTracker.Instance;
            if (tracker == null) return;

            options.Label("MSS_FP_Settings_TemplatesGeneratedThisGame".Translate(tracker.Count));
            scrollViewHeight += 24f;

            if (options.ButtonText("MSS_FP_Settings_ResetTemplatesGenerated".Translate()))
                tracker.Reset();
            scrollViewHeight += 30f;
        }

        private void DrawNameDisplay(Listing_Standard options, ref float scrollViewHeight)
        {
            bool colourBefore = ColourTemplateNames;
            bool hostileBefore = ColourHostileTemplateNames;
            string colourTextBefore = TemplateNameColour;

            DrawCheckBox(
                options,
                "MSS_FP_Settings_ColourTemplateNames".Translate(),
                ref ColourTemplateNames,
                ref scrollViewHeight);

            if (ColourTemplateNames)
            {
                TemplateNameColour = options.TextEntryLabeled(
                    "MSS_FP_Settings_TemplateNameColour".Translate(), TemplateNameColour);
                scrollViewHeight += 24f + options.verticalSpacing;

                string hex = PawnPortabilitySettings.ParseColourHex(TemplateNameColour);
                string preview = hex == null
                    ? "MSS_FP_Settings_TemplateNameColourInvalid".Translate().ToString()
                    : "MSS_FP_Settings_TemplateNameColourPreview".Translate($"<color=#{hex}>Bob Template</color>").ToString();
                options.Label(preview);
                scrollViewHeight += 24f;

                DrawCheckBox(
                    options,
                    "MSS_FP_Settings_ColourHostileTemplateNames".Translate(),
                    ref ColourHostileTemplateNames,
                    ref scrollViewHeight);
            }

            if (colourBefore != ColourTemplateNames
                || hostileBefore != ColourHostileTemplateNames
                || colourTextBefore != TemplateNameColour)
                QueueRedecorate();
        }

        private void DrawExcludeList(Listing_Standard options, ref float scrollViewHeight)
        {
            List<PawnTemplateDef> templates = PawnPortability.AllDefsIncludingUser
                .OrderBy(d => d.label ?? d.defName)
                .ToList();

            if (templates.Count == 0)
            {
                options.Label("MSS_FP_Settings_NoTemplates".Translate());
                scrollViewHeight += 24f;
                return;
            }

            foreach (PawnTemplateDef def in templates)
            {
                bool excluded = ExcludedTemplateDefNames.Contains(def.defName);
                bool before = excluded;
                DrawCheckBox(options, $"{def.label ?? def.defName} ({def.defName})", ref excluded, ref scrollViewHeight);

                if (excluded == before) continue;
                if (excluded) ExcludedTemplateDefNames.Add(def.defName);
                else ExcludedTemplateDefNames.Remove(def.defName);
            }
        }

        private static void QueueRedecorate()
        {
            System.Action action = Pawn_Patch.RedecorateAll;
            if (!PostSaveActions.Contains(action))
                PostSaveActions.Add(action);
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

            Scribe_Values.Look(ref EnableTemplateInjection, "EnableTemplateInjection", false);
            Scribe_Values.Look(ref TemplateInjectionChance, "TemplateInjectionChance", 0.02f);
            Scribe_Collections.Look(ref ExcludedTemplateDefNames, "ExcludedTemplateDefNames", LookMode.Value);
            ExcludedTemplateDefNames ??= new HashSet<string>();

            Scribe_Values.Look(ref ColourTemplateNames, "ColourTemplateNames", false);
            Scribe_Values.Look(ref TemplateNameColour, "TemplateNameColour", DefaultTemplateNameColour);
            Scribe_Values.Look(ref ColourHostileTemplateNames, "ColourHostileTemplateNames", false);

            MSSFP.Settings s = settings as MSSFP.Settings;
            Scribe_Values.Look(ref s.EnableUserTemplateLoading, "EnableUserTemplateLoading", true);
            Scribe_Values.Look(ref s.EnableTemplateWandererJoin, "EnableTemplateWandererJoin", false);
            Scribe_Values.Look(ref s.TemplateWandererJoinChanceMultiplier, "TemplateWandererJoinChanceMultiplier", 1.0f);
        }
    }

    /// <summary>
    /// Static accessors for PawnPortability settings, used throughout the system.
    /// </summary>
    public static class PawnPortabilitySettings
    {
        private static readonly HashSet<string> EmptySet = new();

        private static PawnPortabilitySettingsTab Tab =>
            MSSFPMod.settings?.GetSettings<PawnPortabilitySettingsTab>();

        public static bool LoggingEnabled => Tab?.EnablePawnPortabilityLogging ?? false;

        public static bool ExportGizmoEnabled => Tab?.ShowExportGizmo ?? false;

        public static bool TemplateInjectionEnabled => Tab?.EnableTemplateInjection ?? false;

        public static float TemplateInjectionChance => Tab?.TemplateInjectionChance ?? 0f;

        public static HashSet<string> ExcludedTemplateDefNames => Tab?.ExcludedTemplateDefNames ?? EmptySet;

        public static bool ColourHostileTemplateNames => Tab?.ColourHostileTemplateNames ?? false;

        /// <summary>
        /// Rich-text hex (RRGGBB) for template names, or null when colouring is off
        /// or the configured value doesn't parse.
        /// </summary>
        public static string TemplateNameColourHex
        {
            get
            {
                PawnPortabilitySettingsTab tab = Tab;
                return tab is { ColourTemplateNames: true } ? ParseColourHex(tab.TemplateNameColour) : null;
            }
        }

        /// <summary>
        /// Accepts Unity colour names ("green") or #hex. Normalised to hex so the
        /// emitted tag always parses — Unity rich text rejects spaces around '='.
        /// </summary>
        public static string ParseColourHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return ColorUtility.TryParseHtmlString(value.Trim(), out Color colour)
                ? ColorUtility.ToHtmlStringRGB(colour)
                : null;
        }
    }
}

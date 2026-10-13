using System.Collections.Generic;
using MSSFP.HarmonyPatches;
using Verse;

namespace MSSFP.PawnPortability
{
    /// <summary>
    /// Per-game record of which templates have already produced a pawn. Random
    /// generation (raid injection, template wanderer join) uses each template at
    /// most once per game. Stores defName strings only, so nothing goes stale.
    /// </summary>
    public class TemplateGenerationTracker : GameComponent
    {
        private HashSet<string> generatedDefNames = new();

        public TemplateGenerationTracker(Game game)
        {
        }

        public static TemplateGenerationTracker Instance =>
            Current.Game?.GetComponent<TemplateGenerationTracker>();

        public int Count => generatedDefNames.Count;

        public bool HasGenerated(string defName) => generatedDefNames.Contains(defName);

        public void MarkGenerated(string defName) => generatedDefNames.Add(defName);

        public void Reset() => generatedDefNames.Clear();

        public override void FinalizeInit()
        {
            // Pawn.nameInt is scribed directly, so the Name setter (and with it the
            // decoration postfix) never runs on load. Rebuild decorations once here.
            Pawn_Patch.RedecorateAll();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref generatedDefNames, "generatedDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                generatedDefNames ??= new HashSet<string>();
        }
    }
}

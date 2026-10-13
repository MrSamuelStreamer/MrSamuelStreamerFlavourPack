using Verse;

namespace MSSFP.Comps;

/// <summary>
/// Properties for <see cref="CompLoadedAIPersonaCore"/>. No fields — all state lives on the
/// comp instance (storedHolo, storedPersonality). This properties type only exists to be
/// referenced from XML so the def-loader resolves the comp class.
/// </summary>
public class CompProperties_LoadedAIPersonaCore : CompProperties
{
    public CompProperties_LoadedAIPersonaCore()
    {
        compClass = typeof(CompLoadedAIPersonaCore);
    }
}

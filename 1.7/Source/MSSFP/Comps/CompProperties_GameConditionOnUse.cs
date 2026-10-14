using UnityEngine;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_GameConditionOnUse: CompProperties
{
    public string label;
    public string description;
    public string iconPath;
    public GameConditionDef condition;

    public Texture2D icon;
    public Texture2D Icon
    {
        get
        {
            if (icon == null)
            {
                icon = ContentFinder<Texture2D>.Get(iconPath);
            }
            return icon;
        }
    }

    public bool canTargetAnimals = false;
    public bool canTargetMechs = false;
    public bool canTargetHumans = false;
    public bool canTargetSubhumans = false;
    public bool canTargetEntities = false;
    public bool canTargetBloodfeeders = false;
    public bool consumeOnUse = true;

    public bool mustTargetPlayerFaction = true;

    public CompProperties_GameConditionOnUse()
    {
        compClass = typeof(Comp_GameConditionOnUse);
    }
}

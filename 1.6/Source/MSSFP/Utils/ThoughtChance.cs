using System.Xml;
using RimWorld;
using Verse;

namespace MSSFP.Utils;

public class ThoughtChance
{

    public ThoughtDef though;
    public float chance;

    public ThoughtChance() { }

    public ThoughtChance(ThoughtDef though, float chance)
    {
        this.though = though;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "though", xmlNode.Name);
        chance = ParseHelper.FromString<float>(xmlNode.FirstChild.Value);
    }
}

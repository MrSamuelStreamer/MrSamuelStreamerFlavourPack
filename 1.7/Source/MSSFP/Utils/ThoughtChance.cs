using System.Xml;
using RimWorld;

namespace MSSFP.Utils;

public class ThoughtChance : ChanceEntryBase
{
    public ThoughtDef though;

    public ThoughtChance() { }

    public ThoughtChance(ThoughtDef though, float chance)
    {
        this.though = though;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlNode) => LoadEntryFromXml(xmlNode, "though");
}

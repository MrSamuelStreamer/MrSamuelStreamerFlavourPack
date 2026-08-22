using System.Xml;
using RimWorld;
using Verse;

namespace MSSFP.Utils;

public class MentalBreakChance : ChanceEntryBase
{
    public MentalBreakDef mentalBreak;

    public MentalBreakChance() { }

    public MentalBreakChance(MentalBreakDef mentalBreak, float chance)
    {
        this.mentalBreak = mentalBreak;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlNode) => LoadEntryFromXml(xmlNode, "mentalBreak");
}

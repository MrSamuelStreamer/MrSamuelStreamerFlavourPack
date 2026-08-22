using System.Xml;
using Verse;

namespace MSSFP.Utils;

public class HediffChance : ChanceEntryBase
{
    public HediffDef hediff;

    public HediffChance() { }

    public HediffChance(HediffDef hediff, float chance)
    {
        this.hediff = hediff;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlNode) => LoadDataFromXmlCustom(xmlNode, "hediff");
}

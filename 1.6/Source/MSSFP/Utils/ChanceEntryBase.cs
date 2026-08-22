using System.Xml;
using Verse;

namespace MSSFP.Utils;

/// <summary>
/// Shared XML-loading logic for the "def + chance" pair classes (ThoughtChance,
/// HediffChance, MentalBreakChance). Each subclass owns its own def-typed field
/// (the field name differs per subclass and is passed through to the cross-ref
/// registration) but the MayRequire/MayRequireAnyOf handling and chance parsing
/// are identical, so they live here once.
/// </summary>
public abstract class ChanceEntryBase
{
    public float chance;

    // NOTE: deliberately NOT named LoadDataFromXmlCustom — RimWorld's
    // XmlToObjectUtils.CustomDataLoadMethodOf uses Type.GetMethod(name) which throws
    // AmbiguousMatchException if two methods share that name (base overload + the
    // subclass's public single-arg entry point).
    protected void LoadEntryFromXml(XmlNode xmlNode, string fieldName)
    {
        XmlAttribute mayRequire = xmlNode?.Attributes?["MayRequire"];
        XmlAttribute mayRequireAnyOf = xmlNode?.Attributes?["MayRequireAnyOf"];
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, fieldName, xmlNode?.Name, mayRequire?.Value.ToLower(), mayRequireAnyOf?.Value.ToLower());
        chance = ParseHelper.FromString<float>(xmlNode?.FirstChild.Value ?? "0");
    }
}

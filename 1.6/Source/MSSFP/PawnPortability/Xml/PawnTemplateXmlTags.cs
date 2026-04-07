namespace MSSFP.PawnPortability.Xml
{
    internal static class PawnTemplateXmlTags
    {
        public const string Root = "PawnTemplateDef";
        public const string SchemaVersion = "schemaVersion";
        public const string Mode = "mode";
        public const string RequiredMods = "requiredMods";
        public const string PackageId = "packageId";

        // Character metadata
        public const string OriginSeries = "originSeries";
        public const string Tags = "tags";
        public const string NarrativeNotes = "narrativeNotes";

        // Pawn identity
        public const string KindDef = "kindDef";
        public const string Gender = "gender";
        public const string Name = "name";
        public const string First = "first";
        public const string Nick = "nick";
        public const string Last = "last";

        // Age
        public const string Age = "age";
        public const string BiologicalAgeTicks = "biologicalAgeTicks";
        public const string ChronologicalAgeTicks = "chronologicalAgeTicks";

        // Story
        public const string Story = "story";
        public const string BodyType = "bodyType";
        public const string HeadType = "headType";
        public const string HairDef = "hairDef";
        public const string HairColor = "hairColor";
        public const string SkinColorOverride = "skinColorOverride";
        public const string FurDef = "furDef";
        public const string FavoriteColor = "favoriteColor";
        public const string Childhood = "childhood";
        public const string Adulthood = "adulthood";
        public const string BirthLastName = "birthLastName";
        public const string Traits = "traits";

        // Trait entry
        public const string Def = "def";
        public const string Degree = "degree";

        // Skills
        public const string Skills = "skills";
        public const string Level = "level";
        public const string Passion = "passion";

        // Genes
        public const string Genes = "genes";
        public const string Xenotype = "xenotype";
        public const string XenotypeName = "xenotypeName";
        public const string Endogenes = "endogenes";
        public const string Xenogenes = "xenogenes";

        // Hediffs
        public const string Hediffs = "hediffs";
        public const string Severity = "severity";
        public const string BodyPart = "bodyPart";
        public const string BodyPartLabel = "bodyPartLabel";
        public const string Permanent = "permanent";

        // Equipment / Apparel / Inventory
        public const string Equipment = "equipment";
        public const string Apparel = "apparel";
        public const string Inventory = "inventory";
        public const string Stuff = "stuff";
        public const string Quality = "quality";
        public const string HitPoints = "hitPoints";
        public const string Color = "color";
        public const string StackCount = "stackCount";

        // Abilities
        public const string Abilities = "abilities";

        // Style
        public const string Style = "style";
        public const string BeardDef = "beardDef";
        public const string FaceTattoo = "faceTattoo";
        public const string BodyTattoo = "bodyTattoo";

        // Snapshot-only (Phase 2)
        public const string PermanentMemories = "permanentMemories";
        public const string Stage = "stage";
        public const string Ideology = "ideology";
        public const string Certainty = "certainty";
        public const string IdeoName = "ideoName";
        public const string Royalty = "royalty";
        public const string Titles = "titles";
        public const string Faction = "faction";
        public const string Favor = "favor";
        public const string WorkPriorities = "workPriorities";
        public const string Priority = "priority";

        // MayRequire attribute
        public const string MayRequire = "MayRequire";

        // List item element
        public const string Li = "li";
    }
}

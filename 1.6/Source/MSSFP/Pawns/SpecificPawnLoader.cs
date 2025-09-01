using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RimWorld;
using Verse;

namespace MSSFP.Pawns
{
    public static class SpecificPawnLoader
    {
        private static readonly string ModPawnsPath = Path.Combine(
            LoadedModManager.GetMod<MSSFPMod>().Content.RootDir,
            "1.6",
            "Data",
            "Pawns"
        );

        public static Pawn GetSpecificPawn(string pawnName)
        {
            try
            {
                string xmlPath = Path.Combine(ModPawnsPath, $"{pawnName}.xml");
                if (!File.Exists(xmlPath))
                {
                    Log.Warning($"MSSFP: Pawn XML file not found: {xmlPath}");
                    return null;
                }

                return LoadPawnFromXml(xmlPath);
            }
            catch (Exception ex)
            {
                Log.Error($"MSSFP: Error loading pawn {pawnName}: {ex.Message}");
                return null;
            }
        }

        public static List<string> GetAvailablePawnNames()
        {
            var names = new List<string>();

            try
            {
                if (!Directory.Exists(ModPawnsPath))
                {
                    Log.Warning($"MSSFP: Mod pawns directory not found: {ModPawnsPath}");
                    return names;
                }

                string[] xmlFiles = Directory.GetFiles(ModPawnsPath, "*.xml");
                foreach (string file in xmlFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    names.Add(fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"MSSFP: Error getting available pawn names: {ex.Message}");
            }

            return names;
        }

        private static Pawn LoadPawnFromXml(string xmlPath)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlPath);

                XmlNode root = doc.DocumentElement;

                // --- Load kind and faction ---
                string kindStr = GetXmlValue(root, "kindDef");
                PawnKindDef kindDef = !string.IsNullOrEmpty(kindStr)
                    ? DefDatabase<PawnKindDef>.GetNamed(kindStr, false)
                    : PawnKindDefOf.Villager;

                string factionStr = GetXmlValue(root, "faction");
                Faction faction = Faction.OfPlayer;
                if (!string.IsNullOrEmpty(factionStr))
                {
                    FactionDef factionDef = DefDatabase<FactionDef>.GetNamed(factionStr, false);
                    if (factionDef != null)
                        faction =
                            Find.FactionManager.FirstFactionOfDef(factionDef) ?? Faction.OfPlayer;
                }

                // Create request with forced backstories if available
                var request = new PawnGenerationRequest(kindDef, faction);

                // Check if we have specific backstories to force
                XmlNode storyNode = doc.SelectSingleNode("//story");
                if (storyNode != null)
                {
                    string childhood = GetXmlValue(storyNode, "childhood");
                    string adulthood = GetXmlValue(storyNode, "adulthood");

                    if (!string.IsNullOrEmpty(childhood) || !string.IsNullOrEmpty(adulthood))
                    {
                        // Force no random backstories since we'll set them manually
                        request.ForceNoBackstory = true;
                    }
                }

                Pawn pawn = PawnGenerator.GeneratePawn(request);

                // --- Apply hybrid data ---
                LoadPawnName(pawn, doc);
                LoadPawnStory(pawn, doc);
                LoadPawnSkills(pawn, doc);
                LoadPawnAppearance(pawn, doc);
                LoadPawnStyle(pawn, doc);
                LoadPawnGenes(pawn, doc);
                LoadPawnAge(pawn, doc);

                return pawn;
            }
            catch (Exception ex)
            {
                Log.Error($"MSSFP: Error loading pawn from XML {xmlPath}: {ex.Message}");
                return null;
            }
        }

        private static void LoadPawnName(Pawn pawn, XmlDocument doc)
        {
            XmlNode node = doc.SelectSingleNode("//name");
            if (node != null)
            {
                string first = GetXmlValue(node, "first");
                string nick = GetXmlValue(node, "nick");
                string last = GetXmlValue(node, "last");

                if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
                    pawn.Name = new NameTriple(first ?? "", nick ?? first ?? "", last ?? "");
            }
        }

        private static void LoadPawnStory(Pawn pawn, XmlDocument doc)
        {
            XmlNode storyNode = doc.SelectSingleNode("//story");
            if (storyNode == null)
                return;

            // Traits
            XmlNodeList traits = storyNode.SelectNodes(".//traits/allTraits/li");
            foreach (XmlNode traitNode in traits)
            {
                string traitDefName = GetXmlValue(traitNode, "def");
                if (!string.IsNullOrEmpty(traitDefName))
                {
                    TraitDef traitDef = DefDatabase<TraitDef>.GetNamed(traitDefName, false);
                    if (traitDef != null)
                    {
                        int degree = 0;
                        string degreeStr = GetXmlValue(traitNode, "degree");
                        if (
                            !string.IsNullOrEmpty(degreeStr)
                            && int.TryParse(degreeStr, out int parsed)
                        )
                            degree = parsed;
                        pawn.story.traits.GainTrait(new Trait(traitDef, degree));
                    }
                }
            }

            // Backstories
            string childhood = GetXmlValue(storyNode, "childhood");
            if (!string.IsNullOrEmpty(childhood))
            {
                BackstoryDef childhoodDef = DefDatabase<BackstoryDef>.GetNamed(childhood, false);
                if (childhoodDef != null)
                {
                    // Use reflection to set the childhood backstory
                    var field = typeof(Pawn_StoryTracker).GetField(
                        "childhood",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    if (field != null)
                        field.SetValue(pawn.story, childhoodDef);
                }
            }

            string adulthood = GetXmlValue(storyNode, "adulthood");
            if (!string.IsNullOrEmpty(adulthood))
            {
                BackstoryDef adulthoodDef = DefDatabase<BackstoryDef>.GetNamed(adulthood, false);
                if (adulthoodDef != null)
                {
                    // Use reflection to set the adulthood backstory
                    var field = typeof(Pawn_StoryTracker).GetField(
                        "adulthood",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    if (field != null)
                        field.SetValue(pawn.story, adulthoodDef);
                }
            }

            // Load birth last name
            string birthLastName = GetXmlValue(storyNode, "birthLastName");
            if (!string.IsNullOrEmpty(birthLastName))
            {
                // Use reflection to set the birth last name
                var field = typeof(Pawn_StoryTracker).GetField(
                    "birthLastName",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );
                if (field != null)
                    field.SetValue(pawn.story, birthLastName);
            }

            // Load favorite color
            string favoriteColorDef = GetXmlValue(storyNode, "favoriteColorDef");
            if (!string.IsNullOrEmpty(favoriteColorDef))
            {
                ThingDef colorDef = DefDatabase<ThingDef>.GetNamed(favoriteColorDef, false);
                if (colorDef != null)
                {
                    // Use reflection to set the favorite color
                    var field = typeof(Pawn_StoryTracker).GetField(
                        "favoriteColorDef",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    if (field != null)
                        field.SetValue(pawn.story, colorDef);
                }
            }
        }

        private static void LoadPawnSkills(Pawn pawn, XmlDocument doc)
        {
            XmlNodeList skills = doc.SelectNodes("//skills/skills/li");
            foreach (XmlNode skillNode in skills)
            {
                string skillDefName = GetXmlValue(skillNode, "def");
                string levelStr = GetXmlValue(skillNode, "level");

                if (string.IsNullOrEmpty(skillDefName) || string.IsNullOrEmpty(levelStr))
                    continue;

                SkillDef def = DefDatabase<SkillDef>.GetNamed(skillDefName, false);
                if (def == null)
                    continue;

                if (int.TryParse(levelStr, out int level))
                {
                    SkillRecord rec = pawn.skills.GetSkill(def);
                    rec.levelInt = Math.Max(0, level);

                    string passionStr = GetXmlValue(skillNode, "passion");
                    rec.passion = passionStr?.ToLower() switch
                    {
                        "major" => Passion.Major,
                        "minor" => Passion.Minor,
                        _ => Passion.None,
                    };
                }
            }
        }

        private static void LoadPawnAppearance(Pawn pawn, XmlDocument doc)
        {
            XmlNode storyNode = doc.SelectSingleNode("//story");
            if (storyNode == null)
                return;

            string bodyTypeStr = GetXmlValue(storyNode, "bodyType");
            if (!string.IsNullOrEmpty(bodyTypeStr))
                pawn.story.bodyType = DefDatabase<BodyTypeDef>.GetNamed(bodyTypeStr, false);

            string headTypeStr = GetXmlValue(storyNode, "headType");
            if (!string.IsNullOrEmpty(headTypeStr))
                pawn.story.headType = DefDatabase<HeadTypeDef>.GetNamed(headTypeStr, false);

            string hairDefStr = GetXmlValue(storyNode, "hairDef");
            if (!string.IsNullOrEmpty(hairDefStr))
                pawn.story.hairDef = DefDatabase<HairDef>.GetNamed(hairDefStr, false);
        }

        private static void LoadPawnStyle(Pawn pawn, XmlDocument doc)
        {
            XmlNode styleNode = doc.SelectSingleNode("//style");
            if (styleNode == null)
                return;

            string beardDefStr = GetXmlValue(styleNode, "beardDef");
            if (!string.IsNullOrEmpty(beardDefStr))
                pawn.style.beardDef = DefDatabase<BeardDef>.GetNamed(beardDefStr, false);

            string faceTattooStr = GetXmlValue(styleNode, "faceTattoo");
            if (!string.IsNullOrEmpty(faceTattooStr))
                pawn.style.FaceTattoo = DefDatabase<TattooDef>.GetNamed(faceTattooStr, false);

            string bodyTattooStr = GetXmlValue(styleNode, "bodyTattoo");
            if (!string.IsNullOrEmpty(bodyTattooStr))
                pawn.style.BodyTattoo = DefDatabase<TattooDef>.GetNamed(bodyTattooStr, false);
        }

        private static void LoadPawnGenes(Pawn pawn, XmlDocument doc)
        {
            XmlNode genesNode = doc.SelectSingleNode("//genes");
            if (genesNode == null)
                return;

            string xenotypeStr = GetXmlValue(genesNode, "xenotype");
            if (!string.IsNullOrEmpty(xenotypeStr))
            {
                XenotypeDef xenotype = DefDatabase<XenotypeDef>.GetNamed(xenotypeStr, false);
                if (xenotype != null)
                    pawn.genes.SetXenotype(xenotype);
            }

            // Clear existing endogenes first to avoid conflicts
            pawn.genes.Endogenes.Clear();

            XmlNodeList endogenes = genesNode.SelectNodes(".//endogenes/li");
            foreach (XmlNode geneNode in endogenes)
            {
                string geneDefName = GetXmlValue(geneNode, "def");
                if (!string.IsNullOrEmpty(geneDefName))
                {
                    GeneDef geneDef = DefDatabase<GeneDef>.GetNamed(geneDefName, false);
                    if (geneDef != null)
                        pawn.genes.AddGene(geneDef, false);
                }
            }
        }

        private static void LoadPawnAge(Pawn pawn, XmlDocument doc)
        {
            XmlNode ageNode = doc.SelectSingleNode("//age");
            if (ageNode == null)
                return;

            string bioStr = GetXmlValue(ageNode, "biologicalTicks");
            if (!string.IsNullOrEmpty(bioStr) && long.TryParse(bioStr, out long bio))
                pawn.ageTracker.AgeBiologicalTicks = bio;

            // Don't set negative chronological ticks - let RimWorld calculate this
            // string chronoStr = GetXmlValue(ageNode, "chronologicalTicks");
            // if (!string.IsNullOrEmpty(chronoStr) && long.TryParse(chronoStr, out long chrono))
            //     pawn.ageTracker.AgeChronologicalTicks = chrono;
        }

        private static string GetXmlValue(XmlNode parent, string child)
        {
            XmlNode node = parent?.SelectSingleNode(child);
            return node?.InnerText?.Trim();
        }
    }
}

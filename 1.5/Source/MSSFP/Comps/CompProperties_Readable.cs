using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_Readable : CompProperties
{
    public LetterDef letterDef;
    public string letterLabel;
    public string letterText;
    public string buttonLabel = "Read";
    public string buttonDesc = "Read the letter";
    public string buttonIcon;

    public bool destroyOnRead = false;
    public Texture2D buttonIconTex => buttonIcon is not null ? ContentFinder<Texture2D>.Get(buttonIcon) : BaseContent.BadTex;

    public CompProperties_Readable()
    {
        compClass = typeof(CompReadable);
    }
}

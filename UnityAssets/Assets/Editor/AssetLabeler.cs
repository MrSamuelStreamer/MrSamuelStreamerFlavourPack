using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Labels every supported asset under Assets/Data with a single bundle name and
/// applies sensible importer defaults. Called by ModAssetBundleBuilder before build.
///
/// Texture importers are forced to TextureType=Default — prevents Unity from
/// emitting Sprite sub-assets that confuse RimWorld's ContentFinder.
/// </summary>
public class AssetLabeler
{
    private static readonly string[] assetFolders = { "Assets/Data" };

    private static readonly HashSet<string> ExtensionsToProcess = new HashSet<string>(new[]
    {
        ".shader",
        ".png",
        ".jpeg",
        ".jpg",
        ".psd",
        ".wav",
        ".mp3",
        ".ogg"
    });

    /// <summary>
    /// Switch any Sprite-typed texture back to Default. Sprite import generates
    /// sub-assets that ContentFinder can't address by name.
    /// </summary>
    private static void ConvertSpriteToDefault(string assetPath)
    {
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter
            {
                textureType: TextureImporterType.Sprite
            } importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.SaveAndReimport();
        Debug.Log($"Converted {assetPath} from Sprite to Default.");
    }

    /// <summary>
    /// Label every asset under Assets/Data with the given bundle name. Returns
    /// the list of asset paths so the builder can pass them to BuildPipeline.
    /// </summary>
    public static List<string> LabelAllAssetsWithCommonName(string assetFileName)
    {
        var allFilePaths = new List<string>();
        foreach (string folder in assetFolders)
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"Folder not found (skipping): {folder}");
                continue;
            }
            allFilePaths.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories));
        }

        string[] filePaths = allFilePaths.ToArray();
        List<string> assetsLabeled = new();

        foreach (string filePath in filePaths)
        {
            string assetPath = filePath.Replace("\\", "/");
            string extension = Path.GetExtension(assetPath).ToLower();

            if (!ExtensionsToProcess.Contains(extension))
                continue;
            if (!assetPath.StartsWith("Assets"))
                continue;

            bool isTexture = extension is ".png" or ".jpeg" or ".jpg" or ".psd";
            bool isShader = extension is ".shader";

            if (isTexture)
            {
                ConvertSpriteToDefault(assetPath);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer is null)
                {
                    Debug.LogWarning($"Could not get importer for: {assetPath}");
                    continue;
                }

                importer.assetBundleName = assetFileName;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                importer.SaveAndReimport();
            }
            else if (isShader)
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                importer.assetBundleName = assetFileName;
                importer.SaveAndReimport();
            }
            else
            {
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(assetPath);
                if (importer is null)
                {
                    Debug.LogWarning($"Could not get importer for: {assetPath}");
                    continue;
                }

                importer.assetBundleName = assetFileName;
                AudioImporterSampleSettings sampleSettings = new AudioImporterSampleSettings
                {
                    compressionFormat = AudioCompressionFormat.Vorbis,
                    sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
                    loadType = AudioClipLoadType.CompressedInMemory,
                    quality = 0.25f,
                    preloadAudioData = true
                };
                importer.defaultSampleSettings = sampleSettings;
                importer.SaveAndReimport();
            }

            assetsLabeled.Add(assetPath);
            Debug.Log($"Labeled asset: {assetPath} as {assetFileName}");
        }

        Debug.Log($"Labeling complete: {assetsLabeled.Count} assets labeled with \"{assetFileName}\".");
        return assetsLabeled;
    }

    [MenuItem("Assets/Label All MSSFP Assets")]
    public static void Menu_LabelAllAssetsWithCommonName()
    {
        LabelAllAssetsWithCommonName("mssfp");
    }
}
